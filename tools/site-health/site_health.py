# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Check www.cardgamesimulator.com and keep its GitHub Pages certificate renewed.

Cloudflare proxies the site, so visitors get Cloudflare's certificate, and Cloudflare's SSL mode "Full" accepts
GitHub's origin certificate even after it expires. GitHub can only issue or renew that certificate while the
domain's DNS records point straight at GitHub Pages, which they do not while proxied. The `renew` command
switches the records to DNS only, restarts provisioning by removing and re-adding the custom domain, waits for
the new certificate, and always restores the proxy.

  python3 tools/site-health/site_health.py check-site
  python3 tools/site-health/site_health.py check-certificate --repository OWNER/REPO   # needs GH_TOKEN
  python3 tools/site-health/site_health.py renew --repository OWNER/REPO
      # needs GH_TOKEN (a repository admin's token), CLOUDFLARE_API_TOKEN (Zone DNS edit) and CLOUDFLARE_ZONE_ID
"""

from __future__ import annotations

import argparse
import datetime
import json
import os
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass
from typing import Callable

SITE = "https://www.cardgamesimulator.com"
APEX = "https://cardgamesimulator.com"
GITHUB_API = "https://api.github.com"
CLOUDFLARE_API = "https://api.cloudflare.com/client/v4"
USER_AGENT = "card-game-simulator-site-health"

# GitHub issues 90-day Let's Encrypt certificates. Renew well before expiry so that, during the few minutes the
# records are DNS only, visitors still connect to GitHub with a valid certificate.
RENEW_DAYS_BEFORE_EXPIRY = 21
HEALTHY_CERTIFICATE_STATE = "approved"
DNS_ONLY_TTL = 60
# Cloudflare answers for proxied records with a 300 second TTL, so resolvers can keep its addresses that long.
PROXIED_TTL_SECONDS = 300
DNS_CHECK_TIMEOUT_SECONDS = 20 * 60
PROVISIONING_TIMEOUT_SECONDS = 40 * 60
# The workflow allows 60 minutes; reserve 10 for API calls, restoration, and the final website check.
RENEWAL_TIMEOUT_SECONDS = 50 * 60
POLL_SECONDS = 30
FAILED_CERTIFICATE_STATES = ("errored", "bad_authz", "authorization_revoked")


@dataclass(frozen=True)
class Expectation:
    url: str
    status: int
    location: str | None = None
    contains: str | None = None


SITE_EXPECTATIONS = [
    Expectation(f"{SITE}/", 200, contains="Card Game Simulator"),
    Expectation(f"{APEX}/", 301, location=f"{SITE}/"),
    Expectation(f"{SITE}/llms.txt", 200, contains="Card Game Simulator"),
    # The app downloads games from the site, and the PlayMode tests download this one.
    Expectation(f"{SITE}/games/Arcmage/cgs.json", 200, contains='"name"'),
]


class ApiError(RuntimeError):
    pass


class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


def fetch(url: str, timeout: float = 30) -> tuple[int, dict[str, str], str]:
    """GET a URL over verified TLS without following redirects. Returns (status, headers, body)."""
    opener = urllib.request.build_opener(_NoRedirect)
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    try:
        with opener.open(request, timeout=timeout) as response:
            return response.status, dict(response.headers), response.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        return error.code, dict(error.headers), error.read().decode("utf-8", "replace")


def check_site(get: Callable[[str], tuple[int, dict[str, str], str]] = fetch) -> list[str]:
    """Return a problem description for each expectation the live site does not meet."""
    problems = []
    for expectation in SITE_EXPECTATIONS:
        try:
            status, headers, body = get(expectation.url)
        except Exception as error:  # TLS failures, DNS failures and timeouts all mean the site is down.
            problems.append(f"{expectation.url}: {error}")
            continue
        location = {key.lower(): value for key, value in headers.items()}.get("location")
        if status != expectation.status:
            problems.append(f"{expectation.url}: expected HTTP {expectation.status}, got {status}")
        elif expectation.location and location != expectation.location:
            problems.append(f"{expectation.url}: expected a redirect to {expectation.location}, got {location}")
        elif expectation.contains and expectation.contains not in body:
            problems.append(f"{expectation.url}: the response does not contain {expectation.contains!r}")
    return problems


def certificate_problem(pages: dict, today: datetime.date) -> str | None:
    """Return why the GitHub Pages certificate needs renewing, or None if it is healthy."""
    certificate = pages.get("https_certificate")
    if not certificate:
        return "GitHub Pages has no certificate for the custom domain"
    state = certificate.get("state")
    if state != HEALTHY_CERTIFICATE_STATE:
        return f"the certificate state is {state!r}: {certificate.get('description', '')}".rstrip(": ")
    expires = certificate.get("expires_at")
    if not expires:
        return "the certificate has no expiry date"
    days_left = (datetime.date.fromisoformat(expires) - today).days
    if days_left <= RENEW_DAYS_BEFORE_EXPIRY:
        return f"the certificate expires on {expires}, in {days_left} days"
    return None


class JsonApi:
    def __init__(self, base_url: str, headers: dict[str, str]):
        self.base_url = base_url
        self.headers = {"User-Agent": USER_AGENT, "Accept": "application/json", **headers}

    def request(self, method: str, path: str, body: dict | None = None):
        data = None if body is None else json.dumps(body).encode()
        headers = dict(self.headers)
        if data is not None:
            headers["Content-Type"] = "application/json"
        request = urllib.request.Request(self.base_url + path, data=data, headers=headers, method=method)
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                text = response.read().decode()
        except urllib.error.HTTPError as error:
            raise ApiError(f"{method} {path} failed with HTTP {error.code}: {error.read().decode()[:500]}") from error
        return json.loads(text) if text else None


class GitHubPages:
    def __init__(self, repository: str, token: str):
        self.repository_path = f"/repos/{repository}"
        self.path = f"{self.repository_path}/pages"
        self.api = JsonApi(GITHUB_API, {"Authorization": f"Bearer {token}",
                                        "Accept": "application/vnd.github+json",
                                        "X-GitHub-Api-Version": "2022-11-28"})

    def get(self) -> dict:
        return self.api.request("GET", self.path)

    def update(self, **fields) -> None:
        self.api.request("PUT", self.path, fields)

    def health(self) -> dict | None:
        """GitHub's DNS check for the custom domain, or None while GitHub is still running it."""
        result = self.api.request("GET", f"{self.path}/health")
        return result if result and result.get("domain") else None

    def can_administer(self) -> bool:
        """Whether the token's account is a repository administrator, which changing the custom domain needs."""
        return bool((self.api.request("GET", self.repository_path).get("permissions") or {}).get("admin"))


class CloudflareDns:
    def __init__(self, zone_id: str, token: str):
        self.path = f"/zones/{zone_id}/dns_records"
        self.api = JsonApi(CLOUDFLARE_API, {"Authorization": f"Bearer {token}"})

    def records(self, name: str) -> list[dict]:
        return self.api.request("GET", f"{self.path}?name={name}&per_page=100")["result"]

    def update(self, record_id: str, proxied: bool, ttl: int) -> None:
        self.api.request("PATCH", f"{self.path}/{record_id}", {"proxied": proxied, "ttl": ttl})


HEALTH_FIELDS = ("is_proxied", "is_cloudflare_ip", "is_pointed_to_github_pages_ip", "is_cname_to_github_user_domain",
                 "is_https_eligible", "caa_error", "reason")


def wait_until_https_eligible(pages: GitHubPages, log: Callable[[str], None], sleep: Callable[[float], None],
                              clock: Callable[[], float]) -> None:
    """Wait until GitHub's own DNS check says it can issue a certificate for the domain and its alternate."""
    deadline = clock() + DNS_CHECK_TIMEOUT_SECONDS
    while True:
        health = pages.health()
        if health:
            domains = [health[key] for key in ("domain", "alt_domain") if health.get(key)]
            for domain in domains:
                log(f"GitHub DNS check for {domain.get('host')}: "
                    + ", ".join(f"{field}={domain.get(field)}" for field in HEALTH_FIELDS))
            if all(domain.get("is_https_eligible") for domain in domains):
                return
        else:
            log("GitHub is still running its DNS check")
        if clock() >= deadline:
            raise ApiError(f"GitHub's DNS check did not find the domain eligible for HTTPS after "
                           f"{DNS_CHECK_TIMEOUT_SECONDS // 60} minutes")
        sleep(POLL_SECONDS)


def renew(pages: GitHubPages, dns: CloudflareDns, log: Callable[[str], None] = print,
          sleep: Callable[[float], None] = time.sleep, clock: Callable[[], float] = time.monotonic) -> None:
    """Restart GitHub Pages certificate provisioning while the domain's records are DNS only."""
    renewal_deadline = clock() + RENEWAL_TIMEOUT_SECONDS

    def check_deadline() -> None:
        if clock() >= renewal_deadline:
            raise ApiError(f"certificate renewal exceeded {RENEWAL_TIMEOUT_SECONDS // 60} minutes; "
                           "restoring the custom domain and Cloudflare proxy")

    def wait(seconds: float) -> None:
        check_deadline()
        sleep(min(seconds, renewal_deadline - clock()))
        check_deadline()

    site = pages.get()
    if site.get("build_type") != "workflow":
        # Removing the custom domain from a branch-built site commits a CNAME change to the protected branch.
        raise ApiError("GitHub Pages must deploy from GitHub Actions (build_type 'workflow') to renew automatically")
    cname = site.get("cname")
    if not cname:
        raise ApiError("GitHub Pages has no custom domain")
    # Check before touching Cloudflare, so a token without access leaves the proxy alone.
    if not pages.can_administer():
        raise ApiError("the GitHub token cannot change GitHub Pages settings; it needs admin access to the repository")
    old_expiry = (site.get("https_certificate") or {}).get("expires_at") or ""
    apex = cname[4:] if cname.startswith("www.") else cname
    proxied = [record for name in dict.fromkeys([apex, cname]) for record in dns.records(name)
               if record.get("type") in ("A", "AAAA", "CNAME") and record.get("proxied")]

    try:
        for record in proxied:
            check_deadline()
            log(f"Setting {record['type']} {record['name']} to DNS only")
            dns.update(record["id"], proxied=False, ttl=DNS_ONLY_TTL)
        # GitHub only issues a certificate once its DNS check sees the records pointing straight at GitHub Pages.
        log(f"Waiting {PROXIED_TTL_SECONDS} seconds for resolvers to drop Cloudflare's addresses")
        wait(PROXIED_TTL_SECONDS)
        wait_until_https_eligible(pages, log, wait, clock)
        check_deadline()

        log(f"Removing and re-adding {cname} to restart certificate provisioning")
        pages.update(cname=None)
        wait(POLL_SECONDS)
        pages.update(cname=cname)

        deadline = clock() + PROVISIONING_TIMEOUT_SECONDS
        last_seen, restarted = None, False
        while True:
            check_deadline()
            certificate = pages.get().get("https_certificate") or {}
            check_deadline()
            state, expiry = certificate.get("state"), certificate.get("expires_at") or ""
            if (state, expiry) != last_seen:
                log(f"Certificate state: {state}, expires {expiry or 'unknown'}")
                last_seen = (state, expiry)
            if state == HEALTHY_CERTIFICATE_STATE and expiry > old_expiry:
                log("GitHub Pages has a new certificate")
                break
            # GitHub can keep reporting the old failed state for a long time before provisioning starts (it says
            # HTTPS can take up to an hour), so a failed state only ends the wait once provisioning has restarted.
            if state not in FAILED_CERTIFICATE_STATES:
                restarted = True
            elif restarted:
                raise ApiError(f"certificate provisioning failed with state {state!r}: "
                               f"{certificate.get('description')}")
            if clock() >= deadline:
                raise ApiError(f"no new certificate after {PROVISIONING_TIMEOUT_SECONDS // 60} minutes "
                               f"(state {state!r})")
            wait(POLL_SECONDS)
    finally:
        try:
            if pages.get().get("cname") != cname:
                log(f"Restoring the custom domain {cname}")
                pages.update(cname=cname)
        finally:
            for record in proxied:
                log(f"Restoring the Cloudflare proxy for {record['type']} {record['name']}")
                dns.update(record["id"], proxied=True, ttl=record.get("ttl", 1))
            if proxied:
                # Let resolvers drop GitHub's addresses before anything checks the site through Cloudflare again.
                log(f"Waiting {2 * DNS_ONLY_TTL} seconds for the proxied records to be visible again")
                sleep(2 * DNS_ONLY_TTL)


def _annotate(kind: str, title: str, message: str, github: bool) -> None:
    print(f"::{kind} title={title}::{message}" if github else f"{kind}: {message}")


def _require(name: str) -> str:
    value = os.environ.get(name, "")
    if not value:
        raise SystemExit(f"{name} is not set. See developer-docs/website-hosting.md.")
    return value


def _set_output(name: str, value: str) -> None:
    path = os.environ.get("GITHUB_OUTPUT")
    if path:
        with open(path, "a", encoding="utf-8") as output:
            output.write(f"{name}={value}\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("command", choices=["check-site", "check-certificate", "renew"])
    parser.add_argument("--repository", default=os.environ.get("GITHUB_REPOSITORY", ""))
    parser.add_argument("--github", action="store_true", help="Print GitHub Actions annotations")
    args = parser.parse_args(argv)

    if args.command == "check-site":
        problems = check_site()
        for problem in problems:
            _annotate("error", "Website check", problem, args.github)
        if not problems:
            print(f"All {len(SITE_EXPECTATIONS)} website checks passed.")
        return 1 if problems else 0

    pages = GitHubPages(args.repository, _require("GH_TOKEN"))
    if args.command == "check-certificate":
        site = pages.get()
        print(json.dumps(site.get("https_certificate"), indent=2))
        problem = certificate_problem(site, datetime.date.today())
        force = os.environ.get("FORCE_RENEW", "").lower() == "true"
        if problem:
            _annotate("warning", "GitHub Pages certificate", f"Renewing: {problem}", args.github)
        elif force:
            print("The certificate is healthy, but renewal was requested.")
        else:
            print("The GitHub Pages certificate is healthy.")
        _set_output("renew", "true" if problem or force else "false")
        return 0

    dns = CloudflareDns(_require("CLOUDFLARE_ZONE_ID"), _require("CLOUDFLARE_API_TOKEN"))
    try:
        renew(pages, dns)
    except ApiError as error:
        _annotate("error", "GitHub Pages certificate", str(error), args.github)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
