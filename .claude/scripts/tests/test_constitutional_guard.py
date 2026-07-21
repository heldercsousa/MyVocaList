"""Guard 3 (ITF bounds) tests — Level A per testing.md (enforcement logic).

Spec: Docs/Management/DevCycleCraft/inline-trivial-fix/{requirements,design}.md
Each test traces to an AC per testing.md § Acceptance Criteria Traceability.
"""
import contextlib
import importlib.util
import io
import json
import os
import subprocess
import sys
import tempfile
import unittest
from datetime import datetime, timedelta, timezone

_GUARD_PATH = os.path.normpath(
    os.path.join(os.path.dirname(__file__), "..", "constitutional-guard.py"))
_spec = importlib.util.spec_from_file_location("constitutional_guard", _GUARD_PATH)
guard = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(guard)

EDIT_3_LINES = {"old_string": "a\nb\nc", "new_string": "a\nb\nz"}


def _git(cwd, *args):
    subprocess.run(["git", "-C", cwd, *args], check=True,
                   capture_output=True, text=True)


def _make_repo(path):
    """A real git repo on a task branch (so Guard 1 is satisfied)."""
    os.makedirs(path, exist_ok=True)
    _git(path, "init", "-q", "-b", "task/itf-test")
    _git(path, "config", "user.email", "t@t")
    _git(path, "config", "user.name", "t")
    return path


def _declare(root, rel_file, minutes_ago=0, raw=None):
    marker = os.path.join(root, ".itf-active")
    if raw is not None:
        with open(marker, "w", encoding="utf-8") as fh:
            fh.write(raw)
        return marker
    declared_at = datetime.now(timezone.utc) - timedelta(minutes=minutes_ago)
    with open(marker, "w", encoding="utf-8") as fh:
        json.dump({
            "id": "BUG-050",
            "file": rel_file,
            "expected_lines": 1,
            "declared_at": declared_at.isoformat().replace("+00:00", "Z"),
        }, fh)
    return marker


def _touch(root, rel_file, text="// x\n"):
    full = os.path.join(root, rel_file.replace("/", os.sep))
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8") as fh:
        fh.write(text)
    return full.replace("\\", "/")


def _many_lines(n):
    return {"new_string": "\n".join(f"line{i}" for i in range(n))}


class ItfGuardTestBase(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = _make_repo(os.path.join(self._tmp.name, "wt"))

    def tearDown(self):
        self._tmp.cleanup()

    def run_guard(self, file_path, tool_input):
        return guard._itf_guard(file_path, tool_input)


class TestDeclarationScope(ItfGuardTestBase):
    # [AC] AC-ITF-10: no declaration -> guard inert, no ITF bound on any agent
    def test_marker_absent_is_inert(self):
        target = _touch(self.root, "Services/SongService.cs")
        self.assertEqual(self.run_guard(target, _many_lines(50)), 0)

    # [AC] AC-ITF-11: declaration older than 30 min is treated as absent
    def test_expired_declaration_is_inert(self):
        target = _touch(self.root, "Services/SongService.cs")
        _declare(self.root, "Services/SongService.cs", minutes_ago=31)
        self.assertEqual(self.run_guard(target, _many_lines(50)), 0)

    # [AC] AC-ITF-10: declaration in worktree A does not constrain worktree B
    def test_cross_worktree_declaration_is_inert(self):
        other = _make_repo(os.path.join(self._tmp.name, "wt-b"))
        _declare(self.root, "Services/SongService.cs")
        target = _touch(other, "Services/OtherService.cs")
        self.assertEqual(self.run_guard(target, _many_lines(50)), 0)

    # [AC] AC-ITF-09: malformed declaration JSON -> fail-open, never authorization
    def test_malformed_marker_is_inert(self):
        target = _touch(self.root, "Services/SongService.cs")
        _declare(self.root, None, raw="{not json at all")
        self.assertEqual(self.run_guard(target, _many_lines(50)), 0)


class TestItfBounds(ItfGuardTestBase):
    # [AC] AC-ITF-01: declared file, 3 changed lines, plain .cs -> permitted
    def test_valid_small_cs_edit_is_permitted(self):
        target = _touch(self.root, "Services/SongService.cs")
        _declare(self.root, "Services/SongService.cs")
        self.assertEqual(self.run_guard(target, EDIT_3_LINES), 0)

    # [AC] AC-ITF-02: edit to a file other than the declared one -> block, C1
    def test_different_file_blocks(self):
        _declare(self.root, "Services/SongService.cs")
        target = _touch(self.root, "Services/ArtistService.cs")
        self.assertEqual(self.run_guard(target, EDIT_3_LINES), 2)

    # [AC] AC-ITF-03: changed-line count over 5 -> block, C1
    def test_nine_line_edit_blocks(self):
        target = _touch(self.root, "Services/SongService.cs")
        _declare(self.root, "Services/SongService.cs")
        self.assertEqual(self.run_guard(target, _many_lines(9)), 2)

    # [AC] AC-ITF-04: .xaml target -> block, C3
    def test_xaml_target_blocks(self):
        target = _touch(self.root, "UI/Pages/SongFormPage.xaml")
        _declare(self.root, "UI/Pages/SongFormPage.xaml")
        self.assertEqual(self.run_guard(target, EDIT_3_LINES), 2)

    # [AC] AC-ITF-05: governed component -> block, C4
    def test_governed_component_blocks(self):
        target = _touch(self.root, "UI/Components/CrudListView.cs")
        _declare(self.root, "UI/Components/CrudListView.cs")
        self.assertEqual(self.run_guard(target, EDIT_3_LINES), 2)

    # [AC] AC-ITF-05: a differently-named neighbour is NOT governed
    def test_non_governed_lookalike_is_permitted(self):
        target = _touch(self.root, "UI/Components/ListItemLeadingImage.cs")
        _declare(self.root, "UI/Components/ListItemLeadingImage.cs")
        self.assertEqual(self.run_guard(target, EDIT_3_LINES), 0)

    # [AC] AC-ITF-06: sequential-only registry file -> block, C5
    def test_sequential_only_file_blocks(self):
        target = _touch(self.root, "MyVocaList/MauiProgram.cs")
        _declare(self.root, "MyVocaList/MauiProgram.cs")
        self.assertEqual(self.run_guard(target, EDIT_3_LINES), 2)

    # [AC] AC-ITF-06: migration files are sequential-only too
    def test_migration_file_blocks(self):
        target = _touch(self.root, "Infra/Migrations/20260407_Foo.cs")
        _declare(self.root, "Infra/Migrations/20260407_Foo.cs")
        self.assertEqual(self.run_guard(target, EDIT_3_LINES), 2)


class TestBlockMessages(ItfGuardTestBase):
    """Every block must name its condition ID and point at a rules file."""

    def _stderr_for(self, rel_declared, rel_target, tool_input):
        target = _touch(self.root, rel_target)
        _declare(self.root, rel_declared)
        buf = io.StringIO()
        with contextlib.redirect_stderr(buf):
            rc = self.run_guard(target, tool_input)
        return rc, buf.getvalue()

    # [AC] AC-ITF-02
    def test_c1_file_message(self):
        rc, err = self._stderr_for("Services/A.cs", "Services/B.cs", EDIT_3_LINES)
        self.assertEqual(rc, 2)
        self.assertIn("C1", err)
        self.assertIn("workflow.md", err)

    # [AC] AC-ITF-03
    def test_c1_lines_message(self):
        rc, err = self._stderr_for("Services/A.cs", "Services/A.cs", _many_lines(9))
        self.assertEqual(rc, 2)
        self.assertIn("C1", err)

    # [AC] AC-ITF-04
    def test_c3_message(self):
        rc, err = self._stderr_for("UI/P.xaml", "UI/P.xaml", EDIT_3_LINES)
        self.assertEqual(rc, 2)
        self.assertIn("C3", err)

    # [AC] AC-ITF-05
    def test_c4_message_points_at_governance(self):
        rc, err = self._stderr_for("UI/Components/ListItem.cs",
                                   "UI/Components/ListItem.cs", EDIT_3_LINES)
        self.assertEqual(rc, 2)
        self.assertIn("C4", err)
        self.assertIn("component-change-governance.md", err)

    # [AC] AC-ITF-06
    def test_c5_message(self):
        rc, err = self._stderr_for("Infra/AppDbContext.cs",
                                   "Infra/AppDbContext.cs", EDIT_3_LINES)
        self.assertEqual(rc, 2)
        self.assertIn("C5", err)


class TestChangedLineCounting(unittest.TestCase):
    # [AC] AC-ITF-03: Write -> lines in content
    def test_write_counts_content_lines(self):
        self.assertEqual(guard._changed_lines({"content": "a\nb\nc"}), 3)

    # [AC] AC-ITF-03: Edit -> max(old, new)
    def test_edit_takes_max_of_old_and_new(self):
        self.assertEqual(
            guard._changed_lines({"old_string": "a\nb\nc\nd", "new_string": "z"}), 4)

    # [AC] AC-ITF-03: MultiEdit -> sum over edits
    def test_multiedit_sums_edits(self):
        payload = {"edits": [
            {"old_string": "a\nb", "new_string": "c"},
            {"old_string": "d", "new_string": "e\nf\ng"},
        ]}
        self.assertEqual(guard._changed_lines(payload), 5)

    # [AC] AC-ITF-01: expected_lines in the marker is audit-only, never read
    def test_expected_lines_is_not_read_by_the_guard(self):
        with open(_GUARD_PATH, encoding="utf-8") as fh:
            src = fh.read()
        code = "\n".join(
            ln for ln in src.splitlines() if not ln.strip().startswith("#"))
        self.assertNotIn('"expected_lines"', code)
        self.assertNotIn("'expected_lines'", code)


class TestGuardOrdering(ItfGuardTestBase):
    # [AC] AC-ITF-07: Guard 1 fires first — an ITF declaration grants no
    # worktree/branch exemption. Verified on a repo checked out on develop.
    def test_guard1_blocks_on_develop_even_with_declaration(self):
        _git(self.root, "checkout", "-q", "-b", "develop")
        target = _touch(self.root, "Services/SongService.cs")
        _declare(self.root, "Services/SongService.cs")
        payload = {"tool_input": dict(EDIT_3_LINES, file_path=target)}
        proc = subprocess.run(
            [sys.executable, _GUARD_PATH], input=json.dumps(payload),
            capture_output=True, text=True)
        self.assertEqual(proc.returncode, 2)
        self.assertIn("Rule 2", proc.stderr)
        self.assertIn("develop", proc.stderr)

    # [AC] AC-ITF-10: end-to-end through main() — no marker, big edit, permitted
    def test_main_is_inert_without_a_declaration(self):
        target = _touch(self.root, "Services/SongService.cs")
        payload = {"tool_input": dict(_many_lines(200), file_path=target)}
        proc = subprocess.run(
            [sys.executable, _GUARD_PATH], input=json.dumps(payload),
            capture_output=True, text=True)
        self.assertEqual(proc.returncode, 0, proc.stderr)
        self.assertEqual(proc.stderr, "")


class TestCommitTrailerAudit(ItfGuardTestBase):
    # [AC] AC-ITF-08: an ITF commit is discoverable via git log --grep
    def test_lane_itf_trailer_is_greppable(self):
        _touch(self.root, "Services/SongService.cs")
        _git(self.root, "add", "-A")
        _git(self.root, "commit", "-q", "-m",
             "fix: SongService — wrong constant\n\n"
             "Root cause: typo.\nFix: corrected.\nRegression risk: None\n"
             "Lane: ITF (1 files, 1 lines)")
        out = subprocess.run(
            ["git", "-C", self.root, "log", "--grep", "Lane: ITF", "--oneline"],
            capture_output=True, text=True)
        self.assertEqual(out.returncode, 0)
        self.assertIn("SongService", out.stdout)


if __name__ == "__main__":
    unittest.main(verbosity=2)
