# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Tests for site_health.py. Nothing here calls GitHub, Cloudflare, or the live site."""

import datetime
import io
import os
import unittest
from unittest import mock
from unittest.mock import patch

import site_health
from site_health import ApiError, certificate_problem, check_site, renew, renews_without_outage

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


class RenewsWithoutOutageTests(unittest.TestCase):
    def test_valid_certificate_near_expiry_renews_on_schedule(self):
        self.assertTrue(renews_without_outage(pages_site(expires="2026-12-13"), TODAY))

    def test_failed_expired_or_missing_certificate_waits_for_a_person(self):
        self.assertFalse(renews_without_outage(pages_site("bad_authz", "2026-11-21"), TODAY))
        self.assertFalse(renews_without_outage(pages_site(expires="2026-11-22"), TODAY))
        self.assertFalse(renews_without_outage(pages_site("errored", "2027-01-01"), TODAY))
        self.assertFalse(renews_without_outage({"cname": "www.example.com"}, TODAY))


class CheckCertificateCommandTests(unittest.TestCase):
    def run_check(self, site, force=""):
        class Pages:
            def __init__(self, *args):
                pass

            def get(self):
                return site
        outputs = {}
        with mock.patch.object(site_health, "GitHubPages", Pages), \
                mock.patch.object(site_health, "_set_output", outputs.__setitem__), \
                mock.patch.dict(os.environ, {"GH_TOKEN": "token", "FORCE_RENEW": force}), \
                mock.patch("sys.stdout", io.StringIO()) as stdout:
            code = site_health.main(["check-certificate", "--repository", "owner/repo", "--github"])
        return code, outputs["renew"], stdout.getvalue()

    def test_stuck_certificate_fails_the_check_without_renewing(self):
        code, renew_output, output = self.run_check(pages_site("bad_authz", "2026-11-21"))
        self.assertEqual((code, renew_output), (1, "false"))
        self.assertIn("::error title=GitHub Pages certificate::Not renewing automatically", output)

    def test_valid_certificate_near_expiry_is_renewed(self):
        expiring = pages_site(expires=str(datetime.date.today() + datetime.timedelta(days=10)))
        self.assertEqual(self.run_check(expiring)[:2], (0, "true"))

    def test_manual_request_renews_a_stuck_certificate(self):
        self.assertEqual(self.run_check(pages_site("bad_authz", "2026-11-21"), force="true")[:2], (0, "true"))


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


def eligible_health(eligible=True):
    return {"domain": {"host": "www.cardgamesimulator.com", "is_https_eligible": eligible},
            "alt_domain": {"host": "cardgamesimulator.com", "is_https_eligible": eligible}}


class FakePages:
    def __init__(self, site, states, admin=True, health=None):
        self.site = site
        self.admin = admin
        # DNS check results, one per call; the last one repeats. Eligible by default.
        self.health_results = list(health or [eligible_health()])
        self.states = list(states)  # Certificates reported after the domain is re-added, one per poll.
        self.calls = []
        self.readded = False

    def get(self):
        if self.readded and self.states:
            self.site["https_certificate"] = self.states.pop(0)
        return dict(self.site)

    def can_administer(self):
        return self.admin

    def health(self):
        self.calls.append(("health",))
        return self.health_results.pop(0) if len(self.health_results) > 1 else self.health_results[0]

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
        self.assertEqual([call for call in pages.calls if call[0] == "update"],
                         [("update", {"cname": None}), ("update", {"cname": "www.cardgamesimulator.com"})])
        self.assertIn("GitHub Pages has a new certificate", self.log)

    def test_failure_still_restores_the_proxy_and_the_domain(self):
        failed = {"state": "errored", "expires_at": None, "description": "DNS check failed"}
        pages = FakePages(pages_site("bad_authz", "2026-11-21"), [{"state": "new"}, failed])
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

    def test_slow_dns_and_provisioning_share_a_deadline_and_restore_the_proxy(self):
        stale = {"state": "approved", "expires_at": "2026-12-10"}
        pages = FakePages(pages_site(expires="2026-12-10"), [stale] * 1000,
                          health=[eligible_health(False)] * 38 + [eligible_health()])
        dns = FakeDns()
        clock = Clock()
        original_get = pages.get

        def slow_get():
            clock.sleep(60)  # Account for API latency as well as polling sleeps.
            return original_get()

        with patch.object(pages, "get", side_effect=slow_get):
            with self.assertRaisesRegex(ApiError, "renewal exceeded 50 minutes"):
                renew(pages, dns, log=lambda _: None, sleep=clock.sleep, clock=clock)
        self.assertIn(("update", {"cname": None}), pages.calls)
        self.assertEqual(pages.site["cname"], "www.cardgamesimulator.com")
        self.assertEqual(dns.updates[-2:], [("a1", True, 1), ("www", True, 1)])
        self.assertLess(clock(), 60 * 60)

    def test_deadline_during_domain_removal_restores_domain_and_proxy(self):
        pages = FakePages(pages_site(), [])
        dns = FakeDns()
        clock = Clock()
        # Expire partway through the removal wait, before the normal re-add operation.
        with patch.object(site_health, "RENEWAL_TIMEOUT_SECONDS", site_health.PROXIED_TTL_SECONDS + 10):
            with self.assertRaisesRegex(ApiError, "renewal exceeded"):
                renew(pages, dns, log=lambda _: None, sleep=clock.sleep, clock=clock)
        self.assertEqual(pages.site["cname"], "www.cardgamesimulator.com")
        self.assertEqual(dns.updates[-2:], [("a1", True, 1), ("www", True, 1)])
        self.assertEqual(clock(), site_health.PROXIED_TTL_SECONDS + 10 + 2 * site_health.DNS_ONLY_TTL)

    def test_old_failed_state_is_not_a_failure_until_provisioning_restarts(self):
        old = {"state": "bad_authz", "expires_at": "2026-11-21", "description": "bad"}
        issued = {"state": "approved", "expires_at": "2027-02-20", "description": ""}
        pages = FakePages(pages_site("bad_authz", "2026-11-21"), [old] * 60 + [{"state": "authorized"}, issued])
        self.run_renew(pages)
        self.assertIn("GitHub Pages has a new certificate", self.log)

    def test_waits_for_github_dns_check_before_restarting_provisioning(self):
        issued = {"state": "approved", "expires_at": "2027-02-20", "description": ""}
        pages = FakePages(pages_site("bad_authz", "2026-11-21"), [issued],
                          health=[None, eligible_health(False), eligible_health()])
        self.run_renew(pages)
        first_update = pages.calls.index(("update", {"cname": None}))
        self.assertEqual(pages.calls[:first_update], [("health",)] * 3)
        self.assertIn("GitHub is still running its DNS check", self.log)

    def test_dns_check_that_never_passes_restores_the_proxy_without_touching_the_domain(self):
        pages = FakePages(pages_site("bad_authz", "2026-11-21"), [], health=[eligible_health(False)])
        dns = FakeDns()
        with self.assertRaisesRegex(ApiError, "DNS check did not find the domain eligible"):
            self.run_renew(pages, dns)
        self.assertNotIn(("update", {"cname": None}), pages.calls)
        self.assertEqual(dns.updates[-2:], [("a1", True, 1), ("www", True, 1)])

    def test_token_without_admin_access_changes_nothing(self):
        pages = FakePages(pages_site("bad_authz", "2026-11-21"), [], admin=False)
        dns = FakeDns()
        with self.assertRaisesRegex(ApiError, "admin access"):
            self.run_renew(pages, dns)
        self.assertEqual((pages.calls, dns.updates), ([], []))


if __name__ == "__main__":
    unittest.main()
