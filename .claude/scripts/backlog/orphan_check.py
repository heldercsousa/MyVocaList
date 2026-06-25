"""Fail-open Stop-hook advisory for memory-only BACKLOG orphans (Phase 4, AC-5/6/12).

Thin wrapper around the pure `backlog_lib` classifier. Stdlib only. Mirrors the
`heartbeat.py` fail-open style: the whole body is guarded and `main()` ALWAYS
returns 0 — it warns only, it never blocks a session (INV-1).

Session-scoped signal
---------------------
`.claude/changed-files.txt` is appended by the existing PostToolUse `Edit|Write`
hook and is **cumulative across sessions**, so it cannot be read whole. A
SessionStart command (`session_marker.py`) records the line count of that log at
session start into `.session-marker`; `enumerate_changed_memory_files` reads only
the lines AFTER that boundary and keeps the ones that touch the project memory dir
(matched by the substring `projects/<mangled>/memory/`, since the log records
cwd-relative paths). Missing marker / unreadable log -> empty list (fail open).

Path resolution (findings.md §Q1)
--------------------------------
The device memory dir is keyed to the MAIN repo path, not the worktree. Resolve it
via `git rev-parse --git-common-dir` -> strip trailing `/.git` -> mangle `[:/\\]->-`
-> `~/.claude/projects/<mangled>/memory/`. Never use cwd or $CLAUDE_PROJECT_DIR.
On any git failure -> None (fail open).
"""
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import backlog_lib  # noqa: E402
from backlog_lib import classify_memory_change, should_remind  # noqa: E402

_THIS_DIR = os.path.dirname(os.path.abspath(__file__))
# Bound a read of the cumulative changed-files log to THIS session.
_MARKER_PATH = os.path.join(_THIS_DIR, ".session-marker")


def _changed_files_log():
    """Absolute path to the cumulative PostToolUse changed-files log."""
    proj = os.environ.get("CLAUDE_PROJECT_DIR") or "."
    return os.path.join(proj, ".claude", "changed-files.txt")


def resolve_device_dir(device_memory_dir=None):
    """Resolve the project's device auto-memory directory.

    If `device_memory_dir` is supplied (test fixture) it is returned verbatim.
    Otherwise derive it from the MAIN repo path via `git rev-parse
    --git-common-dir` (worktree-safe): strip a trailing `/.git`, mangle every
    `:`, `/`, `\\` to `-`, and join under `~/.claude/projects/<mangled>/memory/`.
    On ANY failure return None — never raise out (AC-6 / INV-1).
    """
    if device_memory_dir is not None:
        return device_memory_dir
    try:
        common = subprocess.run(
            ["git", "rev-parse", "--git-common-dir"],
            capture_output=True,
            text=True,
        )
        if common.returncode != 0:
            return None
        git_dir = common.stdout.strip()
        if not git_dir:
            return None
        # Make absolute (git may print a relative ".git" in the main repo root).
        git_dir = os.path.abspath(git_dir)
        # Strip a trailing ".git" segment -> canonical main repo root.
        root = git_dir
        if os.path.basename(root) == ".git":
            root = os.path.dirname(root)
        # Normalise separators to forward slash before mangling so the result
        # matches the on-disk mangled directory name regardless of platform.
        root_fs = root.replace("\\", "/")
        mangled = re.sub(r"[:/\\]", "-", root_fs)
        return os.path.join(os.path.expanduser("~"), ".claude", "projects", mangled, "memory")
    except Exception:
        return None


def _session_offset():
    """Line count recorded by the SessionStart marker, or None if absent/bad."""
    try:
        with open(_MARKER_PATH, encoding="utf-8") as fh:
            return int(fh.read().strip())
    except (OSError, ValueError):
        return None


def enumerate_changed_memory_files(signal_source=None, device_memory_dir=None):
    """Return [(filename, line)] for THIS SESSION's memory writes only.

    `signal_source` is the changed-files log path (parameterised for tests).
    Each returned `line` is the recorded log line (the path), which the caller
    feeds to `classify_memory_change` along with the basename. Only log lines
    AFTER the session-start offset that touch the project memory dir are kept.
    Any problem (no marker, unreadable log, unresolved memory dir) -> [].
    """
    try:
        log_path = signal_source or _changed_files_log()
        mem_dir = resolve_device_dir(device_memory_dir)
        if not mem_dir:
            return []
        # Match logged cwd-relative paths by the `projects/<mangled>/memory/`
        # substring (findings.md §Q2): the log records paths relative to cwd.
        mangled = os.path.basename(os.path.dirname(mem_dir.replace("\\", "/").rstrip("/")))
        marker = "projects/{0}/memory/".format(mangled)

        with open(log_path, encoding="utf-8") as fh:
            all_lines = fh.read().splitlines()
        offset = _session_offset()
        session_lines = all_lines if offset is None else all_lines[offset:]

        results = []
        for raw in session_lines:
            norm = raw.replace("\\", "/").strip()
            if not norm:
                continue
            if marker in norm:
                results.append((os.path.basename(norm), norm))
        return results
    except Exception:
        return []


def backlog_changed_this_session():
    """True iff BACKLOG.md changed at any point THIS SESSION (committed OR working-tree).

    MUST NOT use a bare `git diff HEAD` (design §2.2/§7): that misses BACKLOG edits
    already auto-committed in-session. Union the working-tree diff (vs HEAD) with the
    in-session commit log (commits since the session-start HEAD recorded by the
    marker, if available). On any git failure return False (fail open -> the advisory
    may over-warn, never over-suppress).
    """
    backlog = "Docs/Management/BACKLOG.md"
    try:
        # 1. Working-tree changes vs HEAD (covers uncommitted + staged BACKLOG edits).
        wt = subprocess.run(
            ["git", "diff", "--name-only", "HEAD", "--", backlog],
            capture_output=True,
            text=True,
        )
        if wt.returncode == 0 and wt.stdout.strip():
            return True
        # 2. Untracked (e.g. a freshly created BACKLOG) — defensive.
        others = subprocess.run(
            ["git", "ls-files", "--others", "--exclude-standard", "--", backlog],
            capture_output=True,
            text=True,
        )
        if others.returncode == 0 and others.stdout.strip():
            return True
        # 3. In-session commits: anything touching BACKLOG since the session-start ref.
        start_ref = _session_start_ref()
        if start_ref:
            committed = subprocess.run(
                ["git", "diff", "--name-only", "{0}..HEAD".format(start_ref), "--", backlog],
                capture_output=True,
                text=True,
            )
            if committed.returncode == 0 and committed.stdout.strip():
                return True
        return False
    except Exception:
        return False


def _session_start_ref():
    """The git HEAD recorded at session start, or None. Stored on the marker's 2nd line."""
    try:
        with open(_MARKER_PATH, encoding="utf-8") as fh:
            lines = fh.read().splitlines()
        if len(lines) >= 2 and lines[1].strip():
            return lines[1].strip()
        return None
    except OSError:
        return None


def main(device_memory_dir=None, signal_source=None):
    """Run the advisory. ALWAYS returns 0 (INV-1). Prints a reminder at most once."""
    try:
        changed = enumerate_changed_memory_files(
            signal_source=signal_source, device_memory_dir=device_memory_dir
        )
        classified = [classify_memory_change(name, line) for name, line in changed]
        remind, msg = should_remind(classified, backlog_changed_this_session())
        if remind:
            print(msg)
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
