"""worktree-base-check tests — Level A per testing.md (enforcement logic).

The check must distinguish a genuine Rule 2 violation (branch forked from
main's history) from a worktree that is correctly forked from develop but
merely behind it. Fixtures are real temporary git repos.
"""
import contextlib
import importlib.util
import io
import os
import subprocess
import tempfile
import unittest

_SCRIPT = os.path.normpath(
    os.path.join(os.path.dirname(__file__), "..", "worktree-base-check.py"))
_spec = importlib.util.spec_from_file_location("worktree_base_check", _SCRIPT)
wbc = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(wbc)


def _git(cwd, *args):
    subprocess.run(["git", "-C", cwd, *args], check=True,
                   capture_output=True, text=True)


def _commit(cwd, name):
    with open(os.path.join(cwd, name), "w", encoding="utf-8") as fh:
        fh.write(name)
    _git(cwd, "add", "-A")
    _git(cwd, "commit", "-q", "-m", name)


def _make_main_repo(path):
    """main -> develop (develop ahead of main by one commit)."""
    os.makedirs(path, exist_ok=True)
    _git(path, "init", "-q", "-b", "main")
    _git(path, "config", "user.email", "t@t")
    _git(path, "config", "user.name", "t")
    _commit(path, "base.txt")
    _git(path, "checkout", "-q", "-b", "develop")
    _commit(path, "dev1.txt")
    return path


def _add_worktree(repo, name, base):
    wt = os.path.join(os.path.dirname(repo), name)
    _git(repo, "worktree", "add", "-q", wt, "-b", f"task/{name}", base)
    return wt


def _run_main(proj):
    buf = io.StringIO()
    old = os.environ.get("CLAUDE_PROJECT_DIR")
    os.environ["CLAUDE_PROJECT_DIR"] = proj
    try:
        with contextlib.redirect_stdout(buf):
            rc = wbc.main()
    finally:
        if old is None:
            os.environ.pop("CLAUDE_PROJECT_DIR", None)
        else:
            os.environ["CLAUDE_PROJECT_DIR"] = old
    return rc, buf.getvalue()


class WorktreeBaseCheckTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.repo = _make_main_repo(os.path.join(self._tmp.name, "repo"))

    def tearDown(self):
        self._tmp.cleanup()

    def test_based_on_develop_and_current_is_clean(self):
        wt = _add_worktree(self.repo, "wt-ok", "develop")
        self.assertEqual(wbc.classify(self.repo, wt), ("clean", 0))
        rc, out = _run_main(self.repo)
        self.assertEqual(rc, 0)
        self.assertEqual(out, "")

    def test_based_on_develop_but_behind_is_advisory_only(self):
        wt = _add_worktree(self.repo, "wt-behind", "develop")
        _commit(wt, "work.txt")            # unmerged work on the branch
        _git(self.repo, "checkout", "-q", "develop")
        _commit(self.repo, "dev2.txt")     # develop advances
        _commit(self.repo, "dev3.txt")

        state, n = wbc.classify(self.repo, wt)
        self.assertEqual(state, "behind")
        self.assertEqual(n, 2)

        rc, out = _run_main(self.repo)
        self.assertEqual(rc, 0)
        self.assertIn("correctly based on develop", out)
        self.assertIn("2 commit(s) behind", out)
        self.assertNotIn("VIOLATION", out)
        self.assertNotIn("worktree remove", out)
        self.assertNotIn("recreate", out)

    def test_branch_cut_from_main_is_misbased(self):
        wt = _add_worktree(self.repo, "wt-bad", "main")
        _commit(wt, "work.txt")
        state, _n = wbc.classify(self.repo, wt)
        self.assertEqual(state, "misbased")

        rc, out = _run_main(self.repo)
        self.assertEqual(rc, 0)
        self.assertIn("WORKTREE BASE VIOLATION", out)
        self.assertIn("forked from main's history", out)

    def test_git_failure_is_silent_exit_zero(self):
        empty = os.path.join(self._tmp.name, "not-a-repo")
        os.makedirs(empty)
        rc, out = _run_main(empty)
        self.assertEqual(rc, 0)
        self.assertEqual(out, "")

    def test_no_extra_worktrees_is_exit_zero(self):
        rc, out = _run_main(self.repo)
        self.assertEqual(rc, 0)
        self.assertEqual(out, "")


if __name__ == "__main__":
    unittest.main()
