"""Task-log size guard.

A `task-log.md` is capped at 300 lines (CLAUDE.md, "Task-log rotation"). Over the cap, closed/merged
entries move to `task-log-archive-YYYY-MM.md` in the same folder; the header and active/open entries stay.

Modes:
  python tasklog-size-guard.py --check   -> pre-commit: exit 1 when a STAGED task-log.md is over the cap.
                                            Counts the staged content, so it covers every write path
                                            (Edit/Write tools, Bash, scripts). Untouched logs are not checked.
  python tasklog-size-guard.py (stdin)   -> PostToolUse hook (Edit|Write): when the edited file is an
                                            oversize task-log.md, returns the notice as additionalContext
                                            so the model sees it (plain stdout is not shown to the model).
"""
import json
import os
import subprocess
import sys

MAX_TASKLOG_LINES = 300
NAME = "task-log.md"


def is_tasklog(path: str) -> bool:
    p = path.replace("\\", "/")
    return p == NAME or p.endswith("/" + NAME)


def notice(path: str, lines: int) -> str:
    p = path.replace("\\", "/")
    folder = os.path.dirname(p) or "."
    return (
        f"TASK-LOG ROTATION REQUIRED: `{p}` is {lines} lines (cap: {MAX_TASKLOG_LINES}). "
        f"Move closed/merged entries into `{folder}/task-log-archive-YYYY-MM.md` (register a new archive "
        "file in MyVocaList.sln), keeping only the header and active/open entries. Do not read the whole "
        "file to do it: list the `## ` headings with Grep and move line ranges with a script."
    )


def git(*args: str) -> str:
    r = subprocess.run(["git", *args], capture_output=True)
    return r.stdout.decode("utf-8", errors="ignore") if r.returncode == 0 else ""


def check_staged() -> int:
    staged = git("-c", "core.quotepath=false", "diff", "--cached", "--name-only", "--diff-filter=ACMR")
    bad = []
    for path in staged.splitlines():
        if not is_tasklog(path):
            continue
        lines = len(git("show", ":" + path).splitlines())
        if lines > MAX_TASKLOG_LINES:
            bad.append(notice(path, lines))
    if bad:
        print("tasklog-size-guard: FAILED\n  " + "\n  ".join(bad))
        return 1
    return 0


def hook() -> int:
    try:
        payload = json.load(sys.stdin)
    except Exception:
        return 0
    fp = (payload.get("tool_input") or {}).get("file_path") or ""
    if not is_tasklog(fp) or not os.path.isfile(fp):
        return 0
    with open(fp, encoding="utf-8", errors="ignore") as f:
        lines = sum(1 for _ in f)
    if lines > MAX_TASKLOG_LINES:
        print(json.dumps({
            "hookSpecificOutput": {
                "hookEventName": "PostToolUse",
                "additionalContext": notice(fp, lines),
            }
        }))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(check_staged() if "--check" in sys.argv else hook())
    except Exception:
        sys.exit(0)
