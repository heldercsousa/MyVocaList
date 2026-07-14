"""In-session auto-resume reader/writer (T5, AC-4.1/4.2/4.3).

- `resume.py <session_id>`        prints resume_pointer + location + last commit subject.
- `resume.py --set <session_id> <pointer text...>`  writes/updates resume_pointer
  (one line, <=~200 chars) in the session's claim file. The claim file field is the
  CANONICAL resume-pointer location (requirements.md Storage Locations).

CANONICAL POINTER VALUE (T11): the resume_pointer is a pointer to the task-log's
live `### Checkpoint` block (session-ops.md § Checkpoint Ping & Context Manifest),
e.g. `Docs/Management/DevCycleCraft/<feature>/task-log.md § Checkpoint`.
The resumer reads that block + its Context manifest — never a prose to-do.
The heartbeat self-defaults an empty pointer to this form from active-task.json.

Atomic write follows the heartbeat pattern: temp IN THE SAME .claude/leases/ dir, then
os.replace() over the target. Single session owns its own file, so intra-session writers
(heartbeat vs --set) are safe though not mutually atomic. Stdlib only.
"""
import json
import os
import subprocess
import sys
import tempfile
from datetime import datetime, timezone

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lease_lib  # noqa: E402

PROJ = os.environ.get("CLAUDE_PROJECT_DIR", ".")
LEASES = os.path.join(PROJ, ".claude", "leases")
POINTER_MAX = 200


def _path(session_id):
    return os.path.join(LEASES, f"{session_id}.json")


def _read(session_id):
    try:
        with open(_path(session_id), encoding="utf-8") as fh:
            return lease_lib.parse_claim(fh.read())
    except OSError:
        return None


def set_pointer(session_id, text):
    os.makedirs(LEASES, exist_ok=True)
    claim = _read(session_id) or {
        "owner": session_id,
        "pid": os.getppid(),
        "last_active": datetime.now(timezone.utc).isoformat(),
        "resume_pointer": "",
    }
    claim["resume_pointer"] = (text or "")[:POINTER_MAX]
    fd, tmp = tempfile.mkstemp(dir=LEASES, suffix=".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as fh:
            json.dump(claim, fh)
        os.replace(tmp, _path(session_id))
    except OSError:
        try:
            os.unlink(tmp)
        except OSError:
            pass


def show(session_id):
    claim = _read(session_id)
    if not claim:
        print("NO CLAIM FOUND for session", session_id)
        return 1
    pointer = claim.get("resume_pointer") or "(no resume pointer recorded)"
    try:
        last = subprocess.run(
            ["git", "log", "-1", "--format=%s"], cwd=PROJ,
            capture_output=True, text=True, timeout=10).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        last = "(git log unavailable)"
    print("RESUME POINTER:", pointer)
    # T11 location fields (present on heartbeat-written claims; may be absent on old ones).
    if claim.get("branch"):
        print("BRANCH:", claim["branch"])
    if claim.get("worktree"):
        print("WORKTREE:", claim["worktree"])
    if claim.get("task_id"):
        print("TASK:", claim["task_id"])
    print("LAST COMMIT:", last or "(no commits)")
    print("NEXT: read the active feature tasks.md, find the [~] step, "
          "and continue from the pointer.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) >= 4 and sys.argv[1] == "--set":
        set_pointer(sys.argv[2], " ".join(sys.argv[3:]))
    elif len(sys.argv) == 2 and sys.argv[1] not in ("--set",):
        sys.exit(show(sys.argv[1]))
    else:
        print("usage: resume.py <session_id> | "
              "resume.py --set <session_id> <pointer text>")
        sys.exit(2)
