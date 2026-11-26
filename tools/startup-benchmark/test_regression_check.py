# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Tests for the pass/fail rules of regression_check.py."""

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import startup_stats as stats  # noqa: E402
from regression_check import check  # noqa: E402

THRESHOLDS = {"launchToMenuMs": 3000, "startToActionMs": 1500}


def run(index, menu, action, ok=True):
    return stats.RunRecord(str(index), ok, None if ok else "timeout",
                           {"launchToMenuMs": menu, "startToActionMs": action})


class CheckTests(unittest.TestCase):
    def test_passes_when_every_median_is_within_its_threshold(self):
        passed, _ = check([run(1, 1200, 300), run(2, 1300, 280), run(3, 3500, 290)], THRESHOLDS)
        self.assertTrue(passed, "one slow run does not move the median over the threshold")

    def test_fails_when_a_median_is_over_its_threshold(self):
        passed, lines = check([run(1, 3600, 300), run(2, 3700, 280), run(3, 1200, 290)], THRESHOLDS)
        self.assertFalse(passed)
        self.assertTrue(any(line.startswith("launchToMenuMs") and line.endswith("FAIL") for line in lines))

    def test_fails_when_a_run_fails(self):
        passed, lines = check([run(1, 1200, 300), run(2, None, None, ok=False)], THRESHOLDS)
        self.assertFalse(passed)
        self.assertTrue(any("failed" in line for line in lines))

    def test_fails_when_an_interval_has_no_values(self):
        passed, _ = check([run(1, 1200, None)], THRESHOLDS)
        self.assertFalse(passed)


if __name__ == "__main__":
    unittest.main()
