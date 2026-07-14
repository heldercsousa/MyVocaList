"""Reclaim helper (T6, AC-1.3/2.1/2.2/2.3/2.4/2.5, INV-3). Evaluate a TARGET session's
claim and either defer to a live owner or take it over with a single-winner protocol:

  fresh      -> the owner is still alive; caller must pick the next work unit (AC-1.3).
  reclaimed  -> the target was stale; we overwrote owner/pid/last_active and a RE-READ
                confirmed we own it (AC-2.3/2.4 / INV-3).
  lost       -> a concurrent reclaimer won the race (re-read shows a different owner).

Corrupt/half-written claims are treated as stale via lease_lib.parse_claim/classify
(AC-2.5). Atomic write: temp IN THE SAME .claude/leases/ dir, then os.replace().
Stdlib only. Usage: reclaim.py <my_session_id> <target_session_id>
"""
import json
import os
import sys
import tempfile
from datetime import datetime, timezone

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lease_lib  # noqa: E402

PROJ = os.environ.get("CLAUDE_PROJECT_DIR", ".")
LEASES = os.path.join(PROJ, ".claude", "leases")


def _read_raw(path):
    try:
        with open(path, encoding="utf-8") as fh:
            return fh.read()
    except OSError:
        return ""


def main(my_id, target_id):
    target_path = os.path.join(LEASES, f"{target_id}.json")
    claim = lease_lib.parse_claim(_read_raw(target_path))

    if lease_lib.classify(claim) == "fresh":
        print("fresh")  # AC-1.3 -> caller selects the next work unit
        return 0

    # Stale (or corrupt/absent). Attempt reclaim. AC-2.3: overwrite owner/pid/last_active,
    # preserving the prior resume_pointer so the cold resume keeps its continuation hint.
    # T11: also preserve the prior claim's branch/worktree/task_id — the takeover needs
    # the ORIGINAL work location, not the reclaimer's current location.
    prior = claim or {}
    new_claim = {
        "owner": my_id,
        "pid": os.getppid(),
        "last_active": datetime.now(timezone.utc).isoformat(),
        "resume_pointer": prior.get("resume_pointer", ""),
        "branch": prior.get("branch", ""),
        "worktree": prior.get("worktree", ""),
        "task_id": prior.get("task_id", ""),
    }
    os.makedirs(LEASES, exist_ok=True)
    fd, tmp = tempfile.mkstemp(dir=LEASES, suffix=".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as fh:
            json.dump(new_claim, fh)
        os.replace(tmp, target_path)  # atomic
    except OSError:
        try:
            os.unlink(tmp)
        except OSError:
            pass
        print("lost")  # could not stamp ownership -> back off (AC-2.4)
        return 0

    # AC-2.4 / INV-3: RE-READ and let the pure decision pick the single winner.
    reread = lease_lib.parse_claim(_read_raw(target_path))
    print(lease_lib.reclaim_decision(my_id, reread))
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("usage: reclaim.py <my_session_id> <target_session_id>")
        sys.exit(2)
    sys.exit(main(sys.argv[1], sys.argv[2]))
