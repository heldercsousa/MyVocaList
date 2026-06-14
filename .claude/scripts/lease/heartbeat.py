"""PostToolUse/Stop hook (T4, AC-3.1/3.2/3.3/3.4): atomically heartbeat the OWNING
(parent) session's claim file. Reads the hook JSON payload from stdin. Stdlib only.

Design notes:
- COST: the real cost of this hook is the per-call interpreter spawn, so the body must
  stay cheap — one small JSON read + one small JSON write, NO network and NO git calls
  (AC-3.3: no timers/background work; R4: PostToolUse fires after every tool call).
- PARENT-KEYED (AC-3.4): the claim is always keyed off the parent `session_id` in the
  payload, even when `agent_id`/`agent_type` are present (a subagent wave). One claim per
  session — never a separate claim per subagent — so a long subagent wave keeps the
  PARENT claim fresh instead of letting it age into staleness.
- ATOMIC WRITE: a temp file is created IN THE SAME .claude/leases/ dir, then os.replace()
  renames it over the target (same-filesystem atomic rename). Readers never see a
  half-written file (AC-2.5). NOTE: intra-session writers (this heartbeat vs
  resume.py --set) on the SAME file are not mutually atomic, but that is safe because a
  single session owns its own file exclusively.
- FAIL OPEN + SILENT: the whole body is wrapped so a hook error never disrupts the
  harness; we always exit 0 (mirrors the repo's existing `2>/dev/null || true` hook style).
"""
import json
import os
import sys
import tempfile
from datetime import datetime, timezone

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lease_lib  # noqa: E402


def _read_existing(target):
    """Return the existing claim dict (to preserve resume_pointer), or None."""
    try:
        with open(target, encoding="utf-8") as fh:
            return lease_lib.parse_claim(fh.read())
    except OSError:
        return None


def main():
    try:
        payload = json.load(sys.stdin)
    except (ValueError, OSError):
        return  # no/garbled payload -> nothing to do
    if not isinstance(payload, dict):
        return
    # AC-3.4: always key off the PARENT session_id (never agent_id/agent_type).
    session_id = payload.get("session_id")
    if not session_id:
        return

    proj = os.environ.get("CLAUDE_PROJECT_DIR") or payload.get("cwd") or "."
    leases_dir = os.path.join(proj, ".claude", "leases")
    os.makedirs(leases_dir, exist_ok=True)
    target = os.path.join(leases_dir, f"{session_id}.json")

    claim = lease_lib.build_heartbeat_claim(
        session_id,
        os.getppid(),  # the Claude Code session process (parent of this hook)
        datetime.now(timezone.utc).isoformat(),
        existing_claim=_read_existing(target),  # preserves resume_pointer (AC-4.3)
    )

    # Atomic write: temp IN THE SAME DIR, then os.replace over the target.
    fd, tmp = tempfile.mkstemp(dir=leases_dir, suffix=".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as fh:
            json.dump(claim, fh)
        os.replace(tmp, target)
    except OSError:
        try:
            os.unlink(tmp)
        except OSError:
            pass


if __name__ == "__main__":
    try:
        main()
    except Exception:
        # Fail open + silent: never let a hook error disrupt the harness.
        pass
    sys.exit(0)
