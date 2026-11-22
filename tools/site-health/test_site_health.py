# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Tests for site_health.py. Nothing here calls GitHub, Cloudflare, or the live site."""

import datetime
import unittest

import site_health
from site_health import ApiError, certificate_problem, check_site, renew

TODAY = datetime.date(2026, 11, 22)


def pages_site(state="approved", expires="2027-02-20", build_type="workflow", cname="www.cardgamesimulator.com"):
    return {"build_type": build_type, "cname": cname,
            "https_certificate": {"state": state, "description": f"{state} description", "expires_at": expires}}


class CertificateProblemTests(unittest.TestCase):
    def test_healthy_certificate_needs_nothing(self):
        self.assertIsNone(certificate_problem(pages_site(expires="2027-01-01"), TODAY))

    def test_certificate_near_expiry_is_renewed(self):
        self.assertIn("in 21 days", certificate_problem(pages_site(expires="2026-12-13"), TODAY))

    def test_failed_or_expired_certificate_is_renewed(self):
        self.assertIn("'bad_authz'", certificate_problem(pages_site("bad_authz", "2026-11-21"), TODAY))
        self.assertIn("in -1 days", certificate_problem(pages_site(expires="2026-11-21"), TODAY))

    def test_missing_certificate_is_renewed(self):
        self.assertIn("no certificate", certificate_problem({"cname": "www.example.com"}, TODAY))


class CheckSiteTests(unittest.TestCase):
    def responses(self, overrides=None):
        good = {e.url: (e.status, {"Location": e.location} if e.location else {}, e.contains or "")
                for e in site_health.SITE_EXPECTATIONS}
        good.update(overrides or {})

        def get(url):
            response = good[url]
            if isinstance(response, Exception):
                raise response
            return response
        return get

    def test_healthy_site_has_no_problems(self):
        self.assertEqual(check_site(self.responses()), [])

    def test_reports_wrong_status_redirect_content_and_tls_errors(self):
        problems = check_site(self.responses({
            f"{site_health.SITE}/": (526, {}, "Invalid SSL certificate"),
            f"{site_health.APEX}/": (301, {"location": "https://example.com/"}, ""),
            f"{site_health.SITE}/llms.txt": (200, {}, "nothing useful"),
            f"{site_health.SITE}/games/Arcmage/cgs.json": OSError("certificate has expired"),
        }))
        self.assertEqual(len(problems), 4)
        self.assertIn("expected HTTP 200, got 526", problems[0])
        self.assertIn("got https://example.com/", problems[1])
        self.assertIn("does not contain", problems[2])
        self.assertIn("certificate has expired", problems[3])


class FakePages:
    def __init__(self, site, states):
        self.site = site
        self.states = list(states)  # Certificates reported after the domain is re-added, one per poll.
        self.calls = []
        self.readded = False

    def get(self):
        if self.readded and self.states:
            self.site["https_certificate"] = self.states.pop(0)
        return dict(self.site)

    def update(self, **fields):
        self.calls.append(("update", fields))
        if fields.get("cname") and ("update", {"cname": None}) in self.calls:
            self.readded = True
        self.site.update(fields)


class FakeDns:
    def __init__(self):
        self.records_by_name = {
            "cardgamesimulator.com": [
                {"id": "a1", "type": "A", "name": "cardgamesimulator.com", "proxied": True, "ttl": 1},
                {"id": "txt", "type": "TXT", "name": "cardgamesimulator.com", "proxied": False, "ttl": 1}],
            "www.cardgamesimulator.com": [
                {"id": "www", "type": "CNAME", "name": "www.cardgamesimulator.com", "proxied": True, "ttl": 1}],
        }
        self.updates = []

    def records(self, name):
        return self.records_by_name.get(name, [])

    def update(self, record_id, proxied, ttl):
        self.updates.append((record_id, proxied, ttl))


class Clock:
    def __init__(self):
        self.now = 0.0

    def sleep(self, seconds):
        self.now += seconds

    def __call__(self):
        return self.now


class RenewTests(unittest.TestCase):
    def run_renew(self, pages, dns=None):
        dns = dns or FakeDns()
        clock = Clock()
        log = []
        try:
            renew(pages, dns, log=log.append, sleep=clock.sleep, clock=clock)
        finally:
            self.log = log
        return dns

    def test_renews_with_dns_only_records_and_restores_the_proxy(self):
        issued = {"state": "approved", "expires_at": "2027-02-20", "description": ""}
        pages = FakePages(pages_site("bad_authz", "2026-11-21"),
                          [{"state": "new", "expires_at": None}, {"state": "authorization_pending"}, issued])
        dns = self.run_renew(pages)

        self.assertEqual(dns.updates, [("a1", False, 60), ("www", False, 60), ("a1", True, 1), ("www", True, 1)])
        self.assertEqual(pages.calls, [("update", {"cname": None}), ("update", {"cname": "www.cardgamesimulator.com"})])
        self.assertIn("GitHub Pages has a new certificate", self.log)

    def test_failure_still_restores_the_proxy_and_the_domain(self):
        failed = {"state": "errored", "expires_at": None, "description": "DNS check failed"}
        pages = FakePages(pages_site("bad_authz", "2026-11-21"), [failed] * 100)
        dns = FakeDns()
        with self.assertRaisesRegex(ApiError, "state 'errored'"):
            self.run_renew(pages, dns)
        self.assertEqual(dns.updates[-2:], [("a1", True, 1), ("www", True, 1)])
        self.assertEqual(pages.site["cname"], "www.cardgamesimulator.com")

    def test_times_out_when_no_new_certificate_arrives(self):
        stale = {"state": "approved", "expires_at": "2026-12-10", "description": ""}
        pages = FakePages(pages_site(expires="2026-12-10"), [stale] * 1000)
        with self.assertRaisesRegex(ApiError, "no new certificate after 40 minutes"):
            self.run_renew(pages)

    def test_branch_built_site_is_not_changed(self):
        pages = FakePages(pages_site(build_type="legacy"), [])
        dns = FakeDns()
        with self.assertRaisesRegex(ApiError, "GitHub Actions"):
            self.run_renew(pages, dns)
        self.assertEqual((pages.calls, dns.updates), ([], []))


if __name__ == "__main__":
    unittest.main()
