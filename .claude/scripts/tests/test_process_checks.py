import os
import unittest


def _load():
    import importlib.util
    path = os.path.join(os.path.dirname(__file__), "..", "process_checks.py")
    spec = importlib.util.spec_from_file_location("process_checks", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


BS = chr(92)
PAGE_OK = '<?xml version="1.0"?>\n<ContentPage xmlns="x" SafeAreaEdges="Container">\n</ContentPage>'
PAGE_NONE = '<?xml version="1.0"?>\n<ContentPage xmlns="x">\n</ContentPage>'
PAGE_ALL = '<ContentPage xmlns="x" SafeAreaEdges="All"/>'
VIEW = '<ContentView xmlns="x"></ContentView>'


class SlnCheck(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.m = _load()

    def _sln(self, *paths):
        return "\n".join("\t\t" + p.replace("/", BS) + " = " + p.replace("/", BS) for p in paths)

    def test_registered_matches_tracked_passes(self):
        self.assertEqual([], self.m.check_sln(["Docs/a.md"], self._sln("Docs/a.md")))

    def test_unregistered_file_fails(self):
        out = self.m.check_sln(["Docs/a.md", "Docs/b.md"], self._sln("Docs/a.md"))
        self.assertEqual(1, len(out))
        self.assertIn("Docs/b.md", out[0])

    def test_deleted_or_moved_file_still_listed_fails(self):
        out = self.m.check_sln(["Docs/new.md"], self._sln("Docs/new.md", "Docs/old.md"))
        self.assertEqual(1, len(out))
        self.assertIn("Docs/old.md", out[0])


class SafeAreaCheck(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.m = _load()

    def test_container_passes(self):
        self.assertIsNone(self.m.check_safearea("P.xaml", PAGE_OK))

    def test_missing_fails(self):
        self.assertIn("lacks", self.m.check_safearea("P.xaml", PAGE_NONE))

    def test_all_fails_for_new_page(self):
        self.assertIsNotNone(self.m.check_safearea("New.xaml", PAGE_ALL))

    def test_known_violation_is_waived(self):
        known = next(iter(self.m.SAFEAREA_KNOWN_VIOLATIONS))
        self.assertIsNone(self.m.check_safearea(known, PAGE_ALL))

    def test_non_page_root_ignored(self):
        self.assertIsNone(self.m.check_safearea("V.xaml", VIEW))

    def test_comment_before_root_does_not_confuse(self):
        self.assertIsNotNone(self.m.check_safearea("P.xaml", "<!-- <ContentPage SafeAreaEdges=\"Container\"> -->\n" + PAGE_NONE))


LOG_BAD = "# log\n\n---\n## Task: T1\n**Status:** To Review\n\n### Build notes\nx\n"
LOG_GOOD = "# log\n\n---\n## Task: T1\n**Status:** To Review\n\n### Changed files:\n- `a.cs` - x\n"
LOG_PROGRESS = "# log\n\n## Task: T1\n**Status:** in progress\n"


class TaskLogCheck(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.m = _load()

    def test_to_review_without_changed_files_fails(self):
        out = self.m.check_tasklog("task-log.md", LOG_BAD, {5})
        self.assertEqual(1, len(out))
        self.assertIn("Changed files", out[0])

    def test_to_review_with_changed_files_passes(self):
        self.assertEqual([], self.m.check_tasklog("task-log.md", LOG_GOOD, {5}))

    def test_in_progress_entry_not_judged(self):
        self.assertEqual([], self.m.check_tasklog("task-log.md", LOG_PROGRESS, {3}))

    def test_untouched_legacy_entry_not_judged(self):
        self.assertEqual([], self.m.check_tasklog("task-log.md", LOG_BAD, {1}))

    def test_only_the_touched_entry_is_judged(self):
        two = LOG_BAD + "\n## Task: T2\n**Status:** To Review\n\n### Changed files:\n- `b.cs`\n"
        out = self.m.check_tasklog("task-log.md", two, {12})
        self.assertEqual([], out)


if __name__ == "__main__":
    unittest.main()
