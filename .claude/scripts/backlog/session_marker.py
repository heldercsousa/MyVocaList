"""SessionStart helper: record the session-scoping boundary for orphan_check.py.

The advisory in `orphan_check.py` must only consider memory writes and BACKLOG
changes made THIS SESSION. The cumulative `.claude/changed-files.txt` log and the
git history both span many sessions, so this helper stamps a boundary at session
start into `.session-marker` (beside this script):

  line 1: the current line count of `.claude/changed-files.txt` (read offset)
  line 2: the current git HEAD sha (in-session-commit boundary)

`orphan_check.py` reads only the log lines AFTER line 1, and treats BACKLOG commits
since the sha on line 2 as in-session.

Fail-open + silent, mirroring the repo's hook style: any error is swallowed and the
process always exits 0. A missing/garbled marker simply makes `orphan_check.py`
fall back to its own fail-open paths (it never blocks).
"""
import os
import subprocess
import sys

_THIS_DIR = os.path.dirname(os.path.abspath(__file__))
_MARKER_PATH = os.path.join(_THIS_DIR, ".session-marker")


def _project_dir():
    """Worktree root holding `.claude/` — prefer env, fall back to git, then cwd."""
    proj = os.environ.get("CLAUDE_PROJECT_DIR")
    if proj:
        return proj
    try:
        top = subprocess.run(
            ["git", "rev-parse", "--show-toplevel"],
            capture_output=True,
            text=True,
        )
        if top.returncode == 0 and top.stdout.strip():
            return top.stdout.strip()
    except Exception:
        pass
    return "."


def _changed_files_line_count():
    """Current line count of the cumulative changed-files log, or 0 if absent."""
    log_path = os.path.join(_project_dir(), ".claude", "changed-files.txt")
    try:
        with open(log_path, encoding="utf-8") as fh:
            return len(fh.read().splitlines())
    except OSError:
        return 0


def _git_head():
    """Current git HEAD sha, or '' on any failure."""
    try:
        head = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            capture_output=True,
            text=True,
        )
        if head.returncode == 0 and head.stdout.strip():
            return head.stdout.strip()
    except Exception:
        pass
    return ""


def main():
    """Write the session boundary marker. ALWAYS returns 0 (fail-open)."""
    try:
        offset = _changed_files_line_count()
        head = _git_head()
        with open(_MARKER_PATH, "w", encoding="utf-8") as fh:
            fh.write("{0}\n{1}\n".format(offset, head))
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
