"""bash-guard tests — worktree location (deny) and large-read notice (warn-only)."""
import importlib.util
import os
import sys
import tempfile
import unittest

_DIR = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, _DIR)
_spec = importlib.util.spec_from_file_location("bash_guard", os.path.join(_DIR, "bash-guard.py"))
bg = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(bg)
import read_budget as rb  # noqa: E402


class WorktreeLocationTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.proj = os.path.join(self._tmp.name, "repo")
        os.makedirs(os.path.join(self.proj, ".claude", "worktrees"))

    def tearDown(self):
        self._tmp.cleanup()

    def _v(self, command, cwd=None):
        return bg.worktree_violation(command, cwd or self.proj, self.proj)

    def test_canonical_location_passes(self):
        self.assertIsNone(self._v("git worktree add .claude/worktrees/x -b task/x develop"))
        self.assertIsNone(self._v("git worktree add -b task/x .claude/worktrees/x develop"))
        self.assertIsNone(self._v("rtk git worktree add '.claude/worktrees/x' task/x"))

    def test_old_folder_is_denied(self):
        self.assertIn(".claude/worktrees", self._v("git worktree add .worktrees/x -b task/x develop"))

    def test_sibling_folder_is_denied(self):
        self.assertIsNotNone(self._v("git worktree add ../MyVocaList-x -b task/x develop"))
        self.assertIsNotNone(self._v("cd . && git worktree add -b t ../x develop"))

    def test_relative_path_resolves_against_cwd(self):
        inside = os.path.join(self.proj, ".claude", "worktrees")
        self.assertIsNone(self._v("git worktree add x -b t develop", cwd=inside))
        self.assertIsNotNone(self._v("git worktree add x -b t develop", cwd=self.proj))

    def test_dash_c_sets_the_base(self):
        self.assertIsNone(self._v(f'git -C "{self.proj}" worktree add .claude/worktrees/x'))

    def test_other_worktree_verbs_and_dynamic_paths_pass(self):
        self.assertIsNone(self._v("git worktree list"))
        self.assertIsNone(self._v("git worktree remove .worktrees/x"))
        self.assertIsNone(self._v("git worktree add $DIR/x -b t develop"))


    def test_text_that_only_mentions_the_command_passes(self):
        heredoc = ("cat > m.txt <<'EOF'\n- denies git worktree add outside x\n"
                   "git worktree add ../y\nEOF\ngit commit -F m.txt")
        self.assertIsNone(self._v(heredoc))
        self.assertIsNone(self._v('git commit -m "no git worktree add ../x here"'))
        self.assertIsNone(self._v("echo git worktree add ../x"))


class ReadNoticeTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.cwd = self._tmp.name
        self._write("Docs/big.md", 600)
        self._write("Docs/small.md", 50)
        self._write(".claude/worktrees/x/src/Big.cs", 600)
        self._write(".claude/worktrees/x/.claude/library/big.md", 600)
        self._write(".claude/library/big.md", 600)

    def tearDown(self):
        self._tmp.cleanup()

    def _write(self, rel, lines):
        path = os.path.join(self.cwd, *rel.split("/"))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "wb") as f:
            f.write(b"line of text here\n" * lines)

    def _n(self, command):
        return bg.read_notices(command, self.cwd)

    def test_whole_file_readers_get_a_notice(self):
        for cmd in ("cat Docs/big.md", "rtk proxy cat Docs/big.md", "type Docs\\big.md",
                    "Get-Content Docs/big.md", "sed -n 1,400p Docs/big.md",
                    "head -n 400 Docs/big.md", "tail -400 Docs/big.md",
                    "cd . && cat Docs/big.md"):
            self.assertEqual(len(self._n(cmd)), 1, cmd)

    def test_bounded_or_small_reads_are_silent(self):
        for cmd in ("cat Docs/small.md", "head Docs/big.md", "head -n 100 Docs/big.md",
                    "sed -n 60,110p Docs/big.md", "Get-Content Docs/big.md -TotalCount 80",
                    "tail -n 50 Docs/big.md", "cat .claude/library/big.md"):
            self.assertEqual(self._n(cmd), [], cmd)

    def test_pipes_redirects_and_other_commands_are_skipped(self):
        for cmd in ("cat Docs/big.md | head -5", "cat > Docs/big.md <<EOF", "git log Docs/big.md",
                    "cat $F", "cat Docs/missing.md"):
            self.assertEqual(self._n(cmd), [], cmd)


class WorktreeExemptionTests(unittest.TestCase):
    def test_source_inside_a_worktree_is_guarded(self):
        self.assertFalse(rb.is_exempt("C:/r/.claude/worktrees/x/MyVocaList.Services/Foo.cs"))
        self.assertFalse(rb.is_exempt("C:\\r\\.claude\\worktrees\\x\\Docs\\a.md"))

    def test_claude_folder_stays_exempt_in_both_checkouts(self):
        self.assertTrue(rb.is_exempt("C:/r/.claude/library/a.md"))
        self.assertTrue(rb.is_exempt("C:/r/.claude/worktrees/x/.claude/library/a.md"))
        self.assertTrue(rb.is_exempt("C:/r/.claude/worktrees/x/App/GlobalUsings.cs"))


if __name__ == "__main__":
    unittest.main()
