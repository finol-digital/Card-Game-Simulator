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
      # needs GH_TOKEN (repository admin), CLOUDFLARE_API_TOKEN (Zone DNS edit) and CLOUDFLARE_ZONE_ID
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
DNS_SETTLE_SECONDS = 120
PROVISIONING_TIMEOUT_SECONDS = 40 * 60
FAILURE_GRACE_SECONDS = 5 * 60
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
        self.path = f"/repos/{repository}/pages"
        self.api = JsonApi(GITHUB_API, {"Authorization": f"Bearer {token}",
                                        "Accept": "application/vnd.github+json",
                                        "X-GitHub-Api-Version": "2022-11-28"})

    def get(self) -> dict:
        return self.api.request("GET", self.path)

    def update(self, **fields) -> None:
        self.api.request("PUT", self.path, fields)


class CloudflareDns:
    def __init__(self, zone_id: str, token: str):
        self.path = f"/zones/{zone_id}/dns_records"
        self.api = JsonApi(CLOUDFLARE_API, {"Authorization": f"Bearer {token}"})

    def records(self, name: str) -> list[dict]:
        return self.api.request("GET", f"{self.path}?name={name}&per_page=100")["result"]

    def update(self, record_id: str, proxied: bool, ttl: int) -> None:
        self.api.request("PATCH", f"{self.path}/{record_id}", {"proxied": proxied, "ttl": ttl})


def renew(pages: GitHubPages, dns: CloudflareDns, log: Callable[[str], None] = print,
          sleep: Callable[[float], None] = time.sleep, clock: Callable[[], float] = time.monotonic) -> None:
    """Restart GitHub Pages certificate provisioning while the domain's records are DNS only."""
    site = pages.get()
    if site.get("build_type") != "workflow":
        # Removing the custom domain from a branch-built site commits a CNAME change to the protected branch.
        raise ApiError("GitHub Pages must deploy from GitHub Actions (build_type 'workflow') to renew automatically")
    cname = site.get("cname")
    if not cname:
        raise ApiError("GitHub Pages has no custom domain")
    old_expiry = (site.get("https_certificate") or {}).get("expires_at") or ""
    apex = cname[4:] if cname.startswith("www.") else cname
    proxied = [record for name in dict.fromkeys([apex, cname]) for record in dns.records(name)
               if record.get("type") in ("A", "AAAA", "CNAME") and record.get("proxied")]

    try:
        for record in proxied:
            log(f"Setting {record['type']} {record['name']} to DNS only")
            dns.update(record["id"], proxied=False, ttl=DNS_ONLY_TTL)
        log(f"Waiting {DNS_SETTLE_SECONDS} seconds for the DNS change to be visible")
        sleep(DNS_SETTLE_SECONDS)

        log(f"Removing and re-adding {cname} to restart certificate provisioning")
        pages.update(cname=None)
        sleep(10)
        pages.update(cname=cname)

        started = clock()
        deadline = started + PROVISIONING_TIMEOUT_SECONDS
        while True:
            certificate = pages.get().get("https_certificate") or {}
            state, expiry = certificate.get("state"), certificate.get("expires_at") or ""
            log(f"Certificate state: {state}, expires {expiry or 'unknown'}")
            if state == HEALTHY_CERTIFICATE_STATE and expiry > old_expiry:
                log("GitHub Pages has a new certificate")
                break
            # Right after the domain is re-added, GitHub can still report the old failed state for a moment.
            if state in FAILED_CERTIFICATE_STATES and clock() - started >= FAILURE_GRACE_SECONDS:
                raise ApiError(f"certificate provisioning failed with state {state!r}: "
                               f"{certificate.get('description')}")
            if clock() >= deadline:
                raise ApiError(f"no new certificate after {PROVISIONING_TIMEOUT_SECONDS // 60} minutes "
                               f"(state {state!r})")
            sleep(POLL_SECONDS)
    finally:
        try:
            if pages.get().get("cname") != cname:
                log(f"Restoring the custom domain {cname}")
                pages.update(cname=cname)
        finally:
            for record in proxied:
                log(f"Restoring the Cloudflare proxy for {record['type']} {record['name']}")
                dns.update(record["id"], proxied=True, ttl=record.get("ttl", 1))


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
