"""ledger_rotate tests — AC-04.1 to AC-04.4 of the token-usage reduction item (enforcement logic)."""
import datetime
import os
import sys
import unittest

sys.path.insert(0, os.path.normpath(os.path.join(os.path.dirname(__file__), "..")))
import ledger_rotate as lr  # noqa: E402

TODAY = datetime.date(2026, 11, 15)
ALIVE = {"task/alive", "task/old-alive"}


def exists(name):
    return name in ALIVE


def row(id_, branch, merged):
    return "| {0} | something | {1} | {2} | abc1234 |".format(id_, branch, merged)


def ledger(rows):
    return "\n".join([
        "# Work Ledger", "", "## Active", "",
        "| ID | Feature / Bug | Phase / Step | Branch | Worktree | Status | Last commit | Next action |",
        "|----|----|----|----|----|----|----|----|",
        "| ACT | active thing | step | `task/gone-active` | — | merged | a1 | 2026-01-01 old date here |",
        "", "## Completed (this month)", "", "Some prose line.", "",
        "| ID | Feature / Bug | Branch | Merged into develop | Commit |",
        "|----|---------------|--------|---------------------|--------|",
    ] + rows + ["", "---", "", "**Status vocabulary:** x", ""])


class ShouldMoveTests(unittest.TestCase):
    def _m(self, branch, merged):
        return lr.should_move(row("X", branch, merged), TODAY, exists)

    def test_recent_merge_on_a_live_branch_stays(self):
        self.assertIsNone(self._m("`task/alive`", "2026-11-12 merge; Verifier PASS"))

    def test_merge_seven_or_more_days_old_moves_even_if_the_branch_lives(self):
        self.assertIsNotNone(self._m("`task/old-alive`", "2026-11-08 merge"))
        self.assertIsNone(self._m("`task/old-alive`", "2026-11-09 merge"))

    def test_branch_marked_deleted_moves(self):
        self.assertIn("deleted", self._m("`task/x` (deleted)", "2026-11-14 merge"))

    def test_branch_absent_from_git_moves(self):
        self.assertIn("no longer exists", self._m("`task/vanished`", "2026-11-14 merge"))

    def test_cells_that_never_confirm_gone_stay_when_recent(self):
        for branch in ("", "—", "develop", "develop (T2–T12 merged)", "task/a, task/b", "task/sft-5.5…5.8"):
            self.assertIsNone(self._m(branch, "2026-11-14 closed"), branch)

    def test_unparseable_date_never_satisfies_the_date_rule(self):
        self.assertIsNone(self._m("`task/alive`", "merge `78b2b735`; Verifier PASS"))
        self.assertIsNone(self._m("develop", "long ago"))
        self.assertIsNone(self._m("develop", "2026-13-45 nonsense"))


class PlanTests(unittest.TestCase):
    def setUp(self):
        self.rows = [
            row("OLD", "`task/old-alive`", "2026-11-01 merge"),
            row("NEW", "`task/alive`", "2026-11-14 merge"),
            row("DEL", "`task/x` (deleted)", "2026-11-14 merge"),
            row("DEV", "develop", "2026-11-13 closed"),
        ]
        self.text = ledger(self.rows)

    def test_moves_only_the_rows_that_qualify_and_conserves_the_count(self):
        new_text, moved = lr.plan(self.text, TODAY, exists)
        self.assertEqual([lr._cells(r)[0] for r, _ in moved], ["OLD", "DEL"])
        self.assertEqual(lr.completed_count(self.text), 4)
        self.assertEqual(lr.completed_count(new_text) + len(moved), 4)

    def test_active_table_header_and_footer_are_untouched(self):
        new_text, _ = lr.plan(self.text, TODAY, exists)
        for keep in ("| ACT | active thing", "Some prose line.", "**Status vocabulary:** x",
                     "| ID | Feature / Bug | Branch | Merged into develop | Commit |"):
            self.assertIn(keep, new_text)
        head = self.text.split("## Completed")[0]
        self.assertTrue(new_text.startswith(head))

    def test_moved_rows_reach_the_archive_with_identical_text(self):
        _, moved = lr.plan(self.text, TODAY, exists)
        out = lr.append_to_archive("# Archive\n\nold\n", [r for r, _ in moved], TODAY)
        for r, _ in moved:
            self.assertEqual(out.count(r), 1)
        self.assertIn("## Rotated Completed rows — 2026-11", out)
        self.assertTrue(out.startswith("# Archive\n\nold"))
        again = lr.append_to_archive(out, [row("Z", "develop", "2026-01-01")], TODAY)
        self.assertEqual(again.count("## Rotated Completed rows — 2026-11"), 1)

    def test_crlf_files_keep_crlf(self):
        text = self.text.replace("\n", "\r\n")
        new_text, moved = lr.plan(text, TODAY, exists)
        self.assertEqual(len(moved), 2)
        self.assertNotIn("\n", new_text.replace("\r\n", ""))

    def test_no_completed_section_means_no_change(self):
        self.assertEqual(lr.plan("# x\n\n## Active\n", TODAY, exists), ("# x\n\n## Active\n", []))


if __name__ == "__main__":
    unittest.main()
