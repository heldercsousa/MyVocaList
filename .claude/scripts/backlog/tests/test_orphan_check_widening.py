import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import orphan_check  # noqa: E402


class WatchedPathTests(unittest.TestCase):
    def test_backlog_is_watched(self):
        self.assertIn("Docs/Management/BACKLOG.md", orphan_check.WATCHED_PATHS)

    def test_management_readmes_are_watched(self):
        self.assertTrue(any("README" in p or "Docs/Management" == p
                            for p in orphan_check.WATCHED_PATHS))

    def test_registering_a_folder_counts_as_backlog_activity(self):
        self.assertTrue(orphan_check.is_watched(
            "Docs/Management/BusinessFeatures/artists-songs/bugs/2026-07-22-BUG-053-x/README.md"))

    def test_unrelated_doc_does_not_count(self):
        self.assertFalse(orphan_check.is_watched("Docs/Changelog/changelog.md"))


if __name__ == "__main__":
    unittest.main()
