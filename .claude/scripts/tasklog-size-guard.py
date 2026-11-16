"""Task-log size guard.

A `task-log.md` is capped at 300 lines (CLAUDE.md, "Task-log rotation"). Over the cap, entries that are
READY move to `task-log-archive-YYYY-MM.md` in the same folder; open entries always stay. What counts
as ready is decided by `tasklog_rotate.py` (explicit status markers), never by judgment.

Modes:
  python tasklog-size-guard.py --check   -> pre-commit: exit 1 when a STAGED task-log.md is over the cap
                                            AND has entries ready to rotate. Over the cap with only open
                                            entries -> warning, commit allowed (nothing may be rotated).
                                            Counts the staged content, so it covers every write path.
  python tasklog-size-guard.py (stdin)   -> PostToolUse hook (Edit|Write): same test on the edited file;
                                            the notice is returned as additionalContext so the model sees it.
"""
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tasklog_rotate  # noqa: E402

MAX_TASKLOG_LINES = 300
NAME = "task-log.md"


def is_tasklog(path: str) -> bool:
    p = path.replace("\\", "/")
    return p == NAME or p.endswith("/" + NAME)


def notice(path: str, lines: int, ready: int) -> str:
    p = path.replace("\\", "/")
    if ready:
        return (
            f"TASK-LOG ROTATION REQUIRED: `{p}` is {lines} lines (cap: {MAX_TASKLOG_LINES}) and {ready} "
            f"entries are ready to rotate. Run: python .claude/scripts/tasklog_rotate.py \"{p}\" "
            "(add --dry-run to preview). It moves only closed entries and keeps open ones. "
            "Do not move entries by hand and do not read the whole file."
        )
    return (
        f"TASK-LOG OVER CAP, NOTHING TO ROTATE: `{p}` is {lines} lines (cap: {MAX_TASKLOG_LINES}) but every "
        "entry is still open (To Review, Checkpoint, open checklist). Leave them in place; rotate with "
        "tasklog_rotate.py once entries are marked Done or the item is closed."
    )


def git(*args: str) -> str:
    r = subprocess.run(["git", *args], capture_output=True)
    return r.stdout.decode("utf-8", errors="ignore") if r.returncode == 0 else ""


def check_staged() -> int:
    staged = git("-c", "core.quotepath=false", "diff", "--cached", "--name-only", "--diff-filter=ACMR")
    failed = False
    for path in staged.splitlines():
        if not is_tasklog(path):
            continue
        text = git("show", ":" + path)
        lines = len(text.splitlines())
        if lines <= MAX_TASKLOG_LINES:
            continue
        ready = tasklog_rotate.ready_count(text, os.path.dirname(path) or ".")
        if ready:
            failed = True
            print("tasklog-size-guard: FAILED\n  " + notice(path, lines, ready))
        else:
            print("tasklog-size-guard: WARNING\n  " + notice(path, lines, ready))
    return 1 if failed else 0


def hook() -> int:
    try:
        payload = json.load(sys.stdin)
    except Exception:
        return 0
    fp = (payload.get("tool_input") or {}).get("file_path") or ""
    if not is_tasklog(fp) or not os.path.isfile(fp):
        return 0
    with open(fp, encoding="utf-8", errors="ignore", newline="") as f:
        text = f.read()
    lines = len(text.splitlines())
    if lines > MAX_TASKLOG_LINES:
        ready = tasklog_rotate.ready_count(text, os.path.dirname(fp) or ".")
        print(json.dumps({
            "hookSpecificOutput": {
                "hookEventName": "PostToolUse",
                "additionalContext": notice(fp, lines, ready),
            }
        }))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(check_staged() if "--check" in sys.argv else hook())
    except Exception:
        sys.exit(0)
