"""Pure, side-effect-free lease logic. Unit-testable; no hook I/O, no file writes."""
import json
import os
from datetime import datetime, timezone

# Single source of truth for the freshness window (confirmed by Helder 2026-06-14).
LEASE_TTL_SECONDS = 1800  # 30 minutes


def parse_claim(raw_text):
    """Parse a claim file body. Returns a dict, or None if corrupt/half-written (AC-2.5)."""
    if not raw_text or not str(raw_text).strip():
        return None
    try:
        data = json.loads(raw_text)
    except (ValueError, TypeError):
        return None
    if not isinstance(data, dict) or "owner" not in data or "last_active" not in data:
        return None
    return data


def pid_alive(pid):
    """True only if pid is provably a running process on this host. Conservative:
    any uncertainty returns False so the unit ages out via TTL rather than freezing (R2)."""
    if pid is None:
        return False
    try:
        pid = int(pid)
    except (ValueError, TypeError):
        return False
    if pid <= 0:
        return False
    import subprocess
    try:
        if os.name == "nt":
            out = subprocess.run(
                ["tasklist", "/FI", f"PID eq {pid}", "/NH"],
                capture_output=True, text=True, timeout=5)
            return str(pid) in out.stdout
        os.kill(pid, 0)  # POSIX: signal 0 = existence check
        return True
    except PermissionError:
        return True  # exists but not ours -> alive
    except (OSError, ProcessLookupError, subprocess.SubprocessError):
        return False


def classify(claim, now=None, pid_alive_fn=pid_alive, ttl=LEASE_TTL_SECONDS):
    """Two-fact model: fresh if last_active within TTL OR pid alive on host.
    A None/corrupt claim is 'stale' (reclaimable, AC-2.5)."""
    if claim is None:
        return "stale"
    now = now or datetime.now(timezone.utc)
    try:
        last = datetime.fromisoformat(str(claim.get("last_active", "")).replace("Z", "+00:00"))
        if last.tzinfo is None:
            last = last.replace(tzinfo=timezone.utc)
    except (ValueError, TypeError):
        last = None
    within_ttl = last is not None and (now - last).total_seconds() < ttl
    if within_ttl:
        return "fresh"  # AC-1.1
    if pid_alive_fn(claim.get("pid")):
        return "fresh"  # AC-1.2 (live pid is sufficient on its own)
    return "stale"  # AC-2.1 / AC-2.2 (old + dead pid)


def build_heartbeat_claim(session_id, pid, now_iso, existing_claim=None):
    """Pure: produce the claim dict a heartbeat should write for `session_id`.

    Preserves an existing resume_pointer (AC-4.3 — the heartbeat must never erase it)
    and always keys `owner` off the supplied (parent) session_id (AC-3.4)."""
    existing_pointer = ""
    if isinstance(existing_claim, dict):
        existing_pointer = existing_claim.get("resume_pointer", "") or ""
    return {
        "owner": session_id,
        "pid": pid,
        "last_active": now_iso,
        "resume_pointer": existing_pointer,
    }


def reclaim_decision(my_session_id, reread_claim):
    """Pure single-winner decision (AC-2.4 / INV-3): after a reclaimer atomically writes
    its own owner into the claim, it RE-READS and calls this with the re-read result.
    Returns 'reclaimed' iff the re-read owner is us; otherwise 'lost' (a concurrent
    reclaimer overwrote us). A corrupt/None re-read counts as lost."""
    if isinstance(reread_claim, dict) and reread_claim.get("owner") == my_session_id:
        return "reclaimed"
    return "lost"


if __name__ == "__main__":
    # Importable smoke check: no output expected on clean import.
    pass
