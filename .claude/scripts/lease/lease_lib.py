"""Pure, side-effect-free lease logic. Unit-testable; no hook I/O, no file writes."""
import json
import os
from datetime import datetime, timezone

# Single source of truth for the freshness window (confirmed by Helder 2026-06-14).
LEASE_TTL_SECONDS = 1800  # 30 minutes

# T13: claim files whose last_active (or file mtime, if corrupt) is older than this
# are garbage-collected by the heartbeat.
LEASE_GC_SECONDS = 7 * 24 * 3600  # 7 days

# resume_pointer is truncated to this length everywhere it is written.
POINTER_MAX = 200


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


def build_heartbeat_claim(session_id, pid, now_iso, existing_claim=None,
                          branch="", worktree="", task_id=""):
    """Pure: produce the claim dict a heartbeat should write for `session_id`.

    Preserves an existing resume_pointer (AC-4.3 — the heartbeat must never erase it)
    and always keys `owner` off the supplied (parent) session_id (AC-3.4).
    T11: location fields (`branch`/`worktree`/`task_id`) are passed in by the caller —
    the heartbeat always writes fresh current-location values for its OWN session
    (it never preserves location from an existing claim)."""
    existing_pointer = ""
    if isinstance(existing_claim, dict):
        existing_pointer = existing_claim.get("resume_pointer", "") or ""
    return {
        "owner": session_id,
        "pid": pid,
        "last_active": now_iso,
        "resume_pointer": existing_pointer,
        "branch": branch or "",
        "worktree": worktree or "",
        "task_id": task_id or "",
    }


def parse_git_head(head_text):
    """Pure: extract a branch name from a `.git/HEAD` file body.
    Symbolic ref -> branch name; detached HEAD -> 7-char short hash; else ''."""
    if not head_text or not isinstance(head_text, str):
        return ""
    text = head_text.strip()
    if text.startswith("ref:"):
        ref = text[len("ref:"):].strip()
        prefix = "refs/heads/"
        return ref[len(prefix):] if ref.startswith(prefix) else ref
    # Detached HEAD: a bare commit hash.
    if len(text) >= 7 and all(c in "0123456789abcdefABCDEF" for c in text):
        return text[:7]
    return ""


def parse_gitdir(gitfile_text):
    """Pure: extract the gitdir path from a worktree `.git` FILE body
    (`gitdir: <path>`). Returns '' if the text is not a gitdir pointer."""
    if not gitfile_text or not isinstance(gitfile_text, str):
        return ""
    text = gitfile_text.strip()
    if text.startswith("gitdir:"):
        return text[len("gitdir:"):].strip()
    return ""


def worktree_value(cwd, project_dir):
    """Pure: the claim's `worktree` field — the cwd when it differs from the
    project dir, else 'main' (main working tree)."""
    if not cwd:
        return "main"
    norm = lambda p: os.path.normcase(os.path.normpath(str(p)))  # noqa: E731
    if project_dir and norm(cwd) == norm(project_dir):
        return "main"
    return cwd


def format_task_id(active_task):
    """Pure: best-effort task identifier from an active-task.json dict —
    '<taskId>: <taskTitle>' (either part alone if the other is missing), else ''."""
    if not isinstance(active_task, dict):
        return ""
    task_id = str(active_task.get("taskId", "") or "").strip()
    title = str(active_task.get("taskTitle", "") or "").strip()
    if task_id and title:
        return f"{task_id}: {title}"
    return task_id or title


def default_resume_pointer(current_pointer, active_task):
    """Pure (T12): if `current_pointer` is empty, default it from active-task.json to
    the CANONICAL pointer form — '<taskLogFile> § Checkpoint (task <id>: <title>)',
    truncated to POINTER_MAX. NEVER overwrites a non-empty pointer."""
    if current_pointer:
        return current_pointer
    if not isinstance(active_task, dict):
        return ""
    log_file = str(active_task.get("taskLogFile", "") or "").strip()
    if not log_file:
        return ""
    task = format_task_id(active_task)
    pointer = f"{log_file} § Checkpoint"
    if task:
        pointer += f" (task {task})"
    return pointer[:POINTER_MAX]


def should_gc(claim, mtime_ts, now=None, gc_seconds=LEASE_GC_SECONDS):
    """Pure (T13): True if a lease file is garbage-collectable.
    Parseable claim -> GC when last_active is older than gc_seconds.
    Corrupt claim (or unparseable last_active) -> GC only when the file mtime
    is also older than gc_seconds."""
    now = now or datetime.now(timezone.utc)
    last = None
    if isinstance(claim, dict):
        try:
            last = datetime.fromisoformat(
                str(claim.get("last_active", "")).replace("Z", "+00:00"))
            if last.tzinfo is None:
                last = last.replace(tzinfo=timezone.utc)
        except (ValueError, TypeError):
            last = None
    if last is not None:
        return (now - last).total_seconds() > gc_seconds
    try:
        return (now.timestamp() - float(mtime_ts)) > gc_seconds
    except (ValueError, TypeError):
        return False


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
