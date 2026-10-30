import importlib.util
import os
import unittest

SPEC = importlib.util.spec_from_file_location("next_version", os.path.join(os.path.dirname(__file__), "next-version.py"))
next_version = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(next_version)

BASE = "2026.10"


class HighestStablePatchTests(unittest.TestCase):
    def test_the_highest_stable_number_of_the_base(self):
        versions = ["2026.9.80", "2026.10.2", "2026.10.40", "2026.10.9", "2026.11.1"]
        self.assertEqual(40, next_version.highest_stable_patch(versions, BASE))

    def test_pre_releases_are_not_counted(self):
        versions = ["2026.10.40", "2026.10.41-rc.261031.5", "2026.10.42-rc.261101.9"]
        self.assertEqual(40, next_version.highest_stable_patch(versions, BASE))

    def test_a_base_that_has_no_release_yet_has_none(self):
        self.assertEqual(-1, next_version.highest_stable_patch(["2026.9.80", "2025.10.4"], BASE))

    def test_the_base_is_not_a_prefix_of_another_base(self):
        # 2026.1 must not count 2026.10.x, and the dots are dots
        self.assertEqual(-1, next_version.highest_stable_patch(["2026.10.40", "2026x10.7"], "2026.1"))
        self.assertEqual(-1, next_version.highest_stable_patch(["2026x10.7"], BASE))


class NextVersionTests(unittest.TestCase):
    def test_the_regular_version_is_the_next_stable_number(self):
        self.assertEqual("2026.10.41", next_version.next_version("regular", BASE, 40))

    def test_a_base_without_a_release_starts_at_zero(self):
        self.assertEqual("2026.11.0", next_version.next_version("regular", "2026.11", -1))

    def test_the_candidate_is_the_pre_release_of_the_next_stable_number_with_the_date_and_the_run(self):
        self.assertEqual("2026.10.41-rc.261031.57", next_version.next_version("candidate", BASE, 40, run="57", date="261031"))

    def test_a_candidate_needs_the_number_of_the_run(self):
        with self.assertRaises(ValueError):
            next_version.next_version("candidate", BASE, 40, run=None, date="261031")
        with self.assertRaises(ValueError):
            next_version.next_version("candidate", BASE, 40, run="abc", date="261031")

    def test_the_candidate_sorts_after_the_last_stable_version_and_before_the_next_one(self):
        # SemVer: 2026.10.40 < 2026.10.41-rc... < 2026.10.41, the pre-release of the same number is below its stable version
        def key(version):
            core, _, pre = version.partition("-")
            numbers = tuple(int(part) for part in core.split("."))
            return numbers, (1,) if not pre else (0, tuple(int(p) if p.isdigit() else p for p in pre.split(".")))

        candidate = next_version.next_version("candidate", BASE, 40, run="57", date="261031")
        self.assertLess(key("2026.10.40"), key(candidate))
        self.assertLess(key(candidate), key("2026.10.41"))
        # the later run of the same day sorts after the earlier one (numbers, not text)
        self.assertLess(key(next_version.next_version("candidate", BASE, 40, run="9", date="261031")), key(next_version.next_version("candidate", BASE, 40, run="10", date="261031")))


if __name__ == "__main__":
    unittest.main()
