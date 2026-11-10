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
VIEW = '<ContentView xmlns="x"></ContentView>'
PAGE_FORM_CONTAINER = '<ContentPage xmlns:dxe="y" SafeAreaEdges="Container"><dxe:TextEdit/></ContentPage>'
PAGE_FORM_ALL = '<ContentPage xmlns:dxe="y" SafeAreaEdges="All"><dxe:TextEdit/></ContentPage>'
PAGE_NO_INPUT_ALL = '<ContentPage SafeAreaEdges="All"><Label/></ContentPage>'
PAGE_NONE_VALUE = '<ContentPage SafeAreaEdges="None"><Label/></ContentPage>'


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

    def test_container_without_input_passes(self):
        self.assertIsNone(self.m.check_safearea("P.xaml", PAGE_OK))

    def test_missing_fails(self):
        self.assertIn("must declare", self.m.check_safearea("P.xaml", PAGE_NONE))

    def test_none_value_fails(self):
        self.assertIn("must declare", self.m.check_safearea("P.xaml", PAGE_NONE_VALUE))

    def test_all_on_text_input_page_passes(self):
        self.assertIsNone(self.m.check_safearea("P.xaml", PAGE_FORM_ALL))

    def test_container_on_text_input_page_fails(self):
        out = self.m.check_safearea("P.xaml", PAGE_FORM_CONTAINER)
        self.assertIn("text input", out)
        self.assertIn("TextEdit", out)

    def test_all_without_detected_input_passes(self):
        self.assertIsNone(self.m.check_safearea("P.xaml", PAGE_NO_INPUT_ALL))

    def test_non_page_root_ignored(self):
        self.assertIsNone(self.m.check_safearea("V.xaml", VIEW))

    def test_comment_before_root_does_not_confuse(self):
        xaml = '<!-- <ContentPage SafeAreaEdges="Container"> -->\n' + PAGE_NONE
        self.assertIsNotNone(self.m.check_safearea("P.xaml", xaml))

    def test_commented_out_input_not_counted(self):
        xaml = '<ContentPage SafeAreaEdges="Container"><!-- <dxe:TextEdit/> --><Label/></ContentPage>'
        self.assertIsNone(self.m.check_safearea("P.xaml", xaml))


class ItemTrailerCheck(unittest.TestCase):
    IDS = {"trim-context", "ledger-cleanup"}

    @classmethod
    def setUpClass(cls):
        cls.m = _load()

    def _c(self, msg):
        return self.m.check_item_trailer(msg, self.IDS)

    def test_required_types_without_trailer_fail(self):
        for t in ("feat", "refactor", "perf", "amend"):
            self.assertEqual(1, len(self._c(t + ": something\n\nbody")), t)

    def test_scoped_and_breaking_subjects_are_recognised(self):
        self.assertEqual(1, len(self._c("feat(ui)!: x")))

    def test_exempt_types_pass_without_trailer(self):
        for t in ("docs", "chore", "merge", "fix", "test"):
            self.assertEqual([], self._c(t + ": something"), t)

    def test_non_conventional_subject_passes(self):
        self.assertEqual([], self._c("Merge branch 'x' into develop"))

    def test_known_item_passes(self):
        self.assertEqual([], self._c("feat: x\n\nItem: trim-context"))

    def test_unknown_item_fails(self):
        out = self._c("feat: x\n\nItem: nope")
        self.assertEqual(1, len(out))
        self.assertIn("nope", out[0])

    def test_multiple_items_all_checked(self):
        self.assertEqual([], self._c("amend: x\n\nItems: trim-context, ledger-cleanup"))
        self.assertEqual(1, len(self._c("amend: x\n\nItems: trim-context, bogus")))

    def test_comment_lines_ignored_when_finding_subject(self):
        self.assertEqual(1, len(self._c("# comment\nfeat: x")))


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
        self.assertEqual([], self.m.check_tasklog("task-log.md", two, {12}))


if __name__ == "__main__":
    unittest.main()
