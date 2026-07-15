import os
import sys
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import backlog_lib  # noqa: E402


class TestClassifyMemoryChange(unittest.TestCase):
    """Pure classifier: classify_memory_change(filename, line_or_diff) -> 'exempt'|'candidate'."""

    # --- The 4 exempt categories (AC-4) ---

    # [AC] AC-4: feedback_* learnings (category 1) -> exempt
    def test_feedback_file_is_exempt(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "feedback_review_loop_discipline.md",
                "Always dispatch a reviewer subagent after every task.",
            ),
            "exempt",
        )

    # [AC] AC-4: project_* pure continuation pointer for a tracked item (category 2) -> exempt
    def test_project_continuation_pointer_is_exempt(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "project_artists_songs_roadmap.md",
                "NEXT: continue Phase 16C smoke tests in one emulator session.",
            ),
            "exempt",
        )

    # [AC] AC-4: project_* resume note "run smoke test for <tracked>" (category 2) -> exempt
    def test_project_run_smoke_for_tracked_is_exempt(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "project_song_import_resolution.md",
                "NEXT: run smoke test for Wave 5.2 (already tracked).",
            ),
            "exempt",
        )

    # [AC] AC-4: reference-fact cache (category 3) -> exempt
    def test_reference_cache_is_exempt(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "MEMORY.md",
                "The user's email address is heldercsousa@gmail.com.",
            ),
            "exempt",
        )

    # [AC] AC-4: harness-automatic capture the agent did not author (category 4) -> exempt
    def test_harness_automatic_capture_is_exempt(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "MEMORY.md",
                "Today's date is 2026-06-24.",
            ),
            "exempt",
        )

    # --- Genuine new work (AC-4) ---

    # [AC] AC-4: a genuine new-work line with a new work-item noun -> candidate
    def test_new_work_line_is_candidate(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "project_misc.md",
                "implement Foo export pipeline for the queue screen.",
            ),
            "candidate",
        )

    # [AC] INV-3: new-work line inside MEMORY.md -> candidate (line-level wins over file auto-capture)
    def test_new_work_line_inside_memory_md_is_candidate(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "MEMORY.md",
                "TODO: build a CSV import feature for singers.",
            ),
            "candidate",
        )

    # --- AC-13 adversarial precedence ---

    # [AC] AC-13: "NEXT: implement X" where X is a NEW noun -> candidate (new-work noun wins)
    def test_next_implement_new_noun_is_candidate(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "project_artists_songs_roadmap.md",
                "NEXT: implement lyrics caching service.",
            ),
            "candidate",
        )

    # [AC] AC-13: "NEXT: run smoke test for <already-tracked>" -> exempt (pure continuation)
    def test_next_run_smoke_for_tracked_is_exempt(self):
        self.assertEqual(
            backlog_lib.classify_memory_change(
                "project_artists_songs_roadmap.md",
                "NEXT: run smoke test for Phase 16C.1 (already tracked).",
            ),
            "exempt",
        )

    # --- AC-6 garbage / empty -> exempt, never raises ---

    # [AC] AC-6: empty string -> exempt, no exception
    def test_empty_string_is_exempt(self):
        self.assertEqual(backlog_lib.classify_memory_change("MEMORY.md", ""), "exempt")

    # [AC] AC-6: whitespace-only -> exempt, no exception
    def test_whitespace_only_is_exempt(self):
        self.assertEqual(backlog_lib.classify_memory_change("MEMORY.md", "   \t  "), "exempt")

    # [AC] AC-6: garbage / non-meaningful input -> exempt, no exception
    def test_garbage_input_is_exempt(self):
        self.assertEqual(
            backlog_lib.classify_memory_change("", "@@@ %%% !!!"),
            "exempt",
        )

    # [AC] AC-6: None inputs -> exempt, never raises
    def test_none_inputs_are_exempt(self):
        self.assertEqual(backlog_lib.classify_memory_change(None, None), "exempt")


class TestShouldRemind(unittest.TestCase):
    """should_remind(classified_changes, backlog_changed_this_session) -> (bool, str)."""

    # [AC] AC-5: >=1 candidate AND backlog NOT changed this session -> (True, non-empty message)
    def test_candidate_and_backlog_unchanged_reminds(self):
        remind, msg = backlog_lib.should_remind(["exempt", "candidate"], False)
        self.assertTrue(remind)
        self.assertTrue(isinstance(msg, str) and msg.strip() != "")

    # [AC] AC-5: backlog changed this session suppresses the reminder regardless of candidates
    def test_candidate_but_backlog_changed_does_not_remind(self):
        remind, _ = backlog_lib.should_remind(["candidate", "candidate"], True)
        self.assertFalse(remind)

    # [AC] AC-7: all exempt, backlog unchanged -> no reminder (no false positive)
    def test_all_exempt_backlog_unchanged_does_not_remind(self):
        remind, _ = backlog_lib.should_remind(["exempt", "exempt"], False)
        self.assertFalse(remind)

    # [AC] AC-7: empty list -> no reminder
    def test_empty_list_does_not_remind(self):
        remind, _ = backlog_lib.should_remind([], False)
        self.assertFalse(remind)


if __name__ == "__main__":
    unittest.main()
