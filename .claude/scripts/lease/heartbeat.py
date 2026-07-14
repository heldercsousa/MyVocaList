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
- LOCATION FIELDS (T11): `branch` (from `.git/HEAD` file reads — NEVER a git
  subprocess), `worktree` (payload cwd vs CLAUDE_PROJECT_DIR, else "main"),
  `task_id` (best-effort from `.claude/active-task.json`). All fail-open to "".
- SELF-MAINTAINING POINTER (T12): an EMPTY resume_pointer is defaulted from
  active-task.json to "<taskLogFile> § Checkpoint (task <id>: <title>)"; a
  non-empty pointer is NEVER overwritten.
- LEASE GC (T13): after writing its own claim, deletes sibling lease files whose
  last_active (or mtime, if corrupt) is older than LEASE_GC_SECONDS (7 days).
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


def _read_text(path):
    try:
        with open(path, encoding="utf-8") as fh:
            return fh.read()
    except OSError:
        return ""


def _read_branch(cwd):
    """T11: current branch WITHOUT spawning git (cost budget — PostToolUse fires after
    every tool call). Reads `.git/HEAD` under cwd; handles the worktree case where
    `.git` is a FILE containing `gitdir: <path>`. Any failure -> '' (fail-open)."""
    try:
        dotgit = os.path.join(cwd, ".git")
        if os.path.isdir(dotgit):
            head_dir = dotgit
        elif os.path.isfile(dotgit):
            gitdir = lease_lib.parse_gitdir(_read_text(dotgit))
            if not gitdir:
                return ""
            if not os.path.isabs(gitdir):
                gitdir = os.path.join(cwd, gitdir)
            head_dir = gitdir
        else:
            return ""
        return lease_lib.parse_git_head(_read_text(os.path.join(head_dir, "HEAD")))
    except OSError:
        return ""


def _read_active_task(proj):
    """T11/T12: best-effort dict from <proj>/.claude/active-task.json (may be stale
    or absent — fail-open to None)."""
    try:
        data = json.loads(_read_text(os.path.join(proj, ".claude", "active-task.json")))
        return data if isinstance(data, dict) else None
    except (ValueError, TypeError):
        return None


def _gc_stale_leases(leases_dir, own_target):
    """T13: delete lease files older than LEASE_GC_SECONDS (skip our own file;
    corrupt files GC only when their mtime is also old). Entirely fail-open."""
    try:
        own = os.path.normcase(os.path.abspath(own_target))
        with os.scandir(leases_dir) as entries:
            for entry in entries:
                try:
                    if not entry.name.endswith(".json"):
                        continue
                    if os.path.normcase(os.path.abspath(entry.path)) == own:
                        continue
                    claim = lease_lib.parse_claim(_read_text(entry.path))
                    if lease_lib.should_gc(claim, entry.stat().st_mtime):
                        os.unlink(entry.path)
                except (OSError, ValueError):
                    continue
    except OSError:
        pass


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

    # T11: cheap location fields — no subprocess, no git calls.
    cwd = payload.get("cwd") or proj
    active_task = _read_active_task(proj)
    claim = lease_lib.build_heartbeat_claim(
        session_id,
        os.getppid(),  # the Claude Code session process (parent of this hook)
        datetime.now(timezone.utc).isoformat(),
        existing_claim=_read_existing(target),  # preserves resume_pointer (AC-4.3)
        branch=_read_branch(cwd),
        worktree=lease_lib.worktree_value(cwd, proj),
        task_id=lease_lib.format_task_id(active_task),
    )
    # T12: self-maintaining pointer — default an EMPTY resume_pointer from
    # active-task.json (never overwrite a non-empty pointer).
    claim["resume_pointer"] = lease_lib.default_resume_pointer(
        claim.get("resume_pointer", ""), active_task)

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

    # T13: GC lease files >7 days stale (fail-open, one scandir of a small dir).
    _gc_stale_leases(leases_dir, target)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        # Fail open + silent: never let a hook error disrupt the harness.
        pass
    sys.exit(0)
