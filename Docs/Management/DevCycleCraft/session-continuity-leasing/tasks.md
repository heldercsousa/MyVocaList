# Session Continuity — Task Leasing & Auto-Resume — Tasks

> Source plan: [`plan.md`](./plan.md) · Spec: [`requirements.md`](./requirements.md), [`design.md`](./design.md), [`findings.md`](./findings.md)
> Markers: `[ ]` available · `[~]` claimed · `[x]` done · `[P]` parallelizable · `[SEQUENTIAL]` strict order
> NON-NEGOTIABLE: `.claude/rules/workflow.md` is WRITE-PROTECTED (settings.json deny + CLAUDE.md amend process). Phase 5 is a Helder manual handoff — see Task 8.
>
> **Remaining Helder handoff gates:** ~~(1) apply the workflow.md Rule 4/7/8 edits per
> [`workflow-edits-proposed.md`](./workflow-edits-proposed.md) with an `amend:` commit + changelog entry~~
> **— DONE 2026-06-14** (`amend:` commit on branch `session-continuity-leasing` → merged to `develop`;
> changelog entry added). (2) ⏳ **STILL PENDING (Helder):** run the live two-terminal demo in
> [`demo-and-traceability.md`](./demo-and-traceability.md) Part 1.
> All other tasks (T1–T10) are code-complete, tested, and committed.

---

## Phase 1 — Pure logic library (innermost, no deps)

- [x] **Task 1: Lease classification library** [SEQUENTIAL]
  - **Produces:** `.claude/scripts/lease/lease_lib.py` (`LEASE_TTL_SECONDS=1800`, `parse_claim`, `pid_alive`, `classify`)
  - **Consumes:** nothing
  - **Risk:** Medium — R2 (pid reuse): `pid_alive` must be conservative.
  - **Files owned:** `.claude/scripts/lease/lease_lib.py`
  - **Demo:** `python -c "from lease_lib import classify, LEASE_TTL_SECONDS; print(LEASE_TTL_SECONDS)"` prints `1800`.
  - **Review lane:** Elevated (concurrency/correctness logic).

  - [x] **Step 1.1 — Write the library (stdlib only).** Create `.claude/scripts/lease/lease_lib.py`:

```python
"""Pure, side-effect-free lease logic. Unit-testable; no hook I/O, no file writes."""
import json
import os
from datetime import datetime, timezone

# Single source of truth for the freshness window (confirmed by Helder 2026-06-14).
LEASE_TTL_SECONDS = 1800  # 30 minutes


def parse_claim(raw_text):
    """Parse a claim file body. Returns a dict, or None if corrupt/half-written (AC-2.5)."""
    if not raw_text or not raw_text.strip():
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
    try:
        if os.name == "nt":
            import subprocess
            out = subprocess.run(
                ["tasklist", "/FI", f"PID eq {pid}", "/NH"],
                capture_output=True, text=True, timeout=5)
            return str(pid) in out.stdout
        os.kill(pid, 0)  # POSIX: signal 0 = existence check
        return True
    except (OSError, ProcessLookupError, subprocess.SubprocessError):
        return False
    except PermissionError:
        return True  # exists but not ours -> alive


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
```

  - [x] **Step 1.2 — Smoke check.** Run: `python .claude/scripts/lease/lease_lib.py` (no output expected; module must import cleanly).
    Then: `python -c "import sys; sys.path.insert(0,'.claude/scripts/lease'); import lease_lib; print(lease_lib.LEASE_TTL_SECONDS)"` → Expected: `1800`.
  - [x] **Step 1.3 — Commit.** `git add .claude/scripts/lease/lease_lib.py && git commit -m "feat(lease): pure freshness-classification library (TTL=1800s)"`

---

## Phase 2 — Unit tests for the library [P after Task 1]

- [x] **Task 2: lease_lib unit tests** [P]
  - **Produces:** `.claude/scripts/lease/tests/test_lease_lib.py`
  - **Consumes:** `lease_lib.py`
  - **Risk:** Low. Uses stdlib `unittest` (R5 — no new dependency).
  - **Files owned:** `.claude/scripts/lease/tests/test_lease_lib.py`
  - **Demo:** `python -m unittest discover -s .claude/scripts/lease/tests` → all pass.
  - **Review lane:** Standard.

  - [x] **Step 2.1 — Write the tests (TDD: these encode the ACs).** Create `.claude/scripts/lease/tests/test_lease_lib.py`:

```python
import os, sys, unittest
from datetime import datetime, timezone, timedelta
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lease_lib  # noqa: E402

NOW = datetime(2026, 6, 14, 12, 0, 0, tzinfo=timezone.utc)


def iso(dt):
    return dt.isoformat()


class TestClassify(unittest.TestCase):
    def test_within_ttl_is_fresh(self):  # AC-1.1
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=5)), "pid": None}
        self.assertEqual(lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "fresh")

    def test_old_ttl_but_live_pid_is_fresh(self):  # AC-1.2
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=40)), "pid": 999}
        self.assertEqual(lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: True), "fresh")

    def test_old_ttl_and_dead_pid_is_stale(self):  # AC-2.1
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=40)), "pid": 999}
        self.assertEqual(lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "stale")

    def test_dead_pid_before_ttl_still_classifiable_for_fast_reclaim(self):  # AC-2.2
        # Within TTL is fresh by last_active; fast-reclaim is the reclaim CLI's job (Task 6).
        # Here we assert: if last_active is stale AND pid dead -> stale (reclaimable now).
        claim = {"owner": "s1", "last_active": iso(NOW - timedelta(minutes=31)), "pid": 12345}
        self.assertEqual(lease_lib.classify(claim, now=NOW, pid_alive_fn=lambda p: False), "stale")

    def test_corrupt_claim_is_stale(self):  # AC-2.5
        self.assertIsNone(lease_lib.parse_claim('{"owner": "s1"'))  # half-written
        self.assertIsNone(lease_lib.parse_claim(""))
        self.assertEqual(lease_lib.classify(None, now=NOW), "stale")

    def test_ttl_constant_is_1800(self):
        self.assertEqual(lease_lib.LEASE_TTL_SECONDS, 1800)


if __name__ == "__main__":
    unittest.main()
```

  - [x] **Step 2.2 — Run the tests.** Run: `python -m unittest discover -s .claude/scripts/lease/tests -v`
    Expected: 6 tests, all PASS (Task 1 already implemented the logic; if any fails, fix `lease_lib.py`, not the test).
  - [x] **Step 2.3 — Commit.** `git add .claude/scripts/lease/tests/test_lease_lib.py && git commit -m "test(lease): unit tests for freshness classification (AC-1.x/2.x)"`

---

## Phase 3 — Hook + helper scripts [SEQUENTIAL after Phase 1; T5/T6 are [P] with each other]

- [x] **Task 4: Heartbeat hook script** [SEQUENTIAL]
  - **Produces:** `.claude/scripts/lease/heartbeat.py`
  - **Consumes:** `lease_lib.py`
  - **Risk:** Medium — R3 (portability, stdlib only), R4 (must be cheap), AC-3.4 (parent session_id).
  - **Files owned:** `.claude/scripts/lease/heartbeat.py`
  - **Demo:** `echo '{"session_id":"abc","cwd":"."}' | python .claude/scripts/lease/heartbeat.py` writes `.claude/leases/abc.json` with `owner=abc`, ISO `last_active`, current `pid`.
  - **Review lane:** Elevated (atomic write + parent-id keying).

  - [x] **Step 4.1 — Write the hook.** Create `.claude/scripts/lease/heartbeat.py`:

```python
"""PostToolUse/Stop hook: atomically heartbeat the OWNING (parent) session's claim.
Reads hook JSON from stdin. Stdlib only. Never raises into the harness (best-effort)."""
import json
import os
import sys
import tempfile
from datetime import datetime, timezone


def main():
    try:
        payload = json.load(sys.stdin)
    except (ValueError, OSError):
        return  # no payload -> nothing to do
    # AC-3.4: key off the PARENT session_id even when agent_id/agent_type is present.
    # session_id in the payload is always the parent session id; agent_id is separate.
    session_id = payload.get("session_id")
    if not session_id:
        return
    proj = os.environ.get("CLAUDE_PROJECT_DIR") or payload.get("cwd") or "."
    leases_dir = os.path.join(proj, ".claude", "leases")
    os.makedirs(leases_dir, exist_ok=True)
    target = os.path.join(leases_dir, f"{session_id}.json")

    # Preserve an existing resume_pointer if present (heartbeat must not erase it; AC-4.3).
    existing_pointer = ""
    if os.path.exists(target):
        try:
            with open(target, encoding="utf-8") as fh:
                existing_pointer = (json.load(fh) or {}).get("resume_pointer", "")
        except (ValueError, OSError):
            existing_pointer = ""

    claim = {
        "owner": session_id,
        "pid": os.getppid(),  # the Claude Code session process (parent of this hook)
        "last_active": datetime.now(timezone.utc).isoformat(),
        "resume_pointer": existing_pointer,
    }
    # Atomic write: tmp + rename (AC-2.5 prevents readers seeing half-written files).
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
    main()
```

  - [x] **Step 4.2 — Manual test.** Run: `echo '{"session_id":"hbtest","cwd":"."}' | python .claude/scripts/lease/heartbeat.py && cat .claude/leases/hbtest.json`
    Expected: JSON with `"owner":"hbtest"`, an ISO `last_active`, an integer `pid`, `"resume_pointer":""`.
  - [x] **Step 4.3 — Subagent-id test (AC-3.4).** Run: `echo '{"session_id":"parent1","agent_id":"sub9","agent_type":"general-purpose","cwd":"."}' | python .claude/scripts/lease/heartbeat.py && cat .claude/leases/parent1.json`
    Expected: file named `parent1.json` with `"owner":"parent1"` (NOT `sub9`).
  - [x] **Step 4.4 — Cleanup + commit.** `rm -f .claude/leases/hbtest.json .claude/leases/parent1.json` then `git add .claude/scripts/lease/heartbeat.py && git commit -m "feat(lease): PostToolUse/Stop heartbeat hook (atomic, parent-session-keyed)"`

- [x] **Task 5: Resume-pointer / auto-resume reader** [P]
  - **Produces:** `.claude/scripts/lease/resume.py`
  - **Consumes:** `lease_lib.py`, claim files
  - **Risk:** Low. In-session scope only (R: fully-closed terminal out of scope).
  - **Files owned:** `.claude/scripts/lease/resume.py`
  - **Demo:** with a claim file holding a `resume_pointer`, `python .claude/scripts/lease/resume.py <session_id>` prints the pointer + last commit subject.
  - **Review lane:** Standard.

  - [x] **Step 5.1 — Write the reader.** Create `.claude/scripts/lease/resume.py`:

```python
"""In-session auto-resume reader (AC-4.1/4.2). Given a session_id, print the exact
next step: resume_pointer + last commit subject + a hint to read tasks.md.
Also supports `--set <session_id> <pointer text>` to write a resume_pointer (AC-4.3)."""
import json
import os
import subprocess
import sys
import tempfile
from datetime import datetime, timezone

PROJ = os.environ.get("CLAUDE_PROJECT_DIR", ".")
LEASES = os.path.join(PROJ, ".claude", "leases")


def _path(session_id):
    return os.path.join(LEASES, f"{session_id}.json")


def _read(session_id):
    try:
        with open(_path(session_id), encoding="utf-8") as fh:
            return json.load(fh)
    except (OSError, ValueError):
        return None


def set_pointer(session_id, text):
    os.makedirs(LEASES, exist_ok=True)
    claim = _read(session_id) or {
        "owner": session_id, "pid": os.getppid(),
        "last_active": datetime.now(timezone.utc).isoformat(), "resume_pointer": "",
    }
    claim["resume_pointer"] = text[:200]
    fd, tmp = tempfile.mkstemp(dir=LEASES, suffix=".tmp")
    with os.fdopen(fd, "w", encoding="utf-8") as fh:
        json.dump(claim, fh)
    os.replace(tmp, _path(session_id))


def show(session_id):
    claim = _read(session_id)
    if not claim:
        print("NO CLAIM FOUND for session", session_id)
        return 1
    pointer = claim.get("resume_pointer") or "(no resume pointer recorded)"
    try:
        last = subprocess.run(["git", "log", "-1", "--format=%s"], cwd=PROJ,
                              capture_output=True, text=True, timeout=10).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        last = "(git log unavailable)"
    print("RESUME POINTER:", pointer)
    print("LAST COMMIT:", last)
    print("NEXT: read the active feature tasks.md, find the [~] step, and continue from the pointer.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) >= 4 and sys.argv[1] == "--set":
        set_pointer(sys.argv[2], " ".join(sys.argv[3:]))
    elif len(sys.argv) == 2:
        sys.exit(show(sys.argv[1]))
    else:
        print("usage: resume.py <session_id> | resume.py --set <session_id> <pointer text>")
        sys.exit(2)
```

  - [x] **Step 5.2 — Test set + show.** Run: `python .claude/scripts/lease/resume.py --set rtest "Continue Task 4 step 4.3" && python .claude/scripts/lease/resume.py rtest`
    Expected: prints `RESUME POINTER: Continue Task 4 step 4.3`, a `LAST COMMIT:` line, and the `NEXT:` hint.
  - [x] **Step 5.3 — Cleanup + commit.** `rm -f .claude/leases/rtest.json` then `git add .claude/scripts/lease/resume.py && git commit -m "feat(lease): resume-pointer reader/writer for in-session auto-resume"`

- [x] **Task 6: Reclaim CLI (single-winner)** [SEQUENTIAL after Task 4]
  - **Produces:** `.claude/scripts/lease/reclaim.py`
  - **Consumes:** `lease_lib.py`
  - **Risk:** High — implements INV-3 / AC-2.4 write-then-re-read single-winner.
  - **Files owned:** `.claude/scripts/lease/reclaim.py`
  - **Demo:** running reclaim against a stale target prints `reclaimed`; against a fresh target prints `fresh`; the loser of a concurrent race prints `lost`.
  - **Review lane:** Elevated.

  - [x] **Step 6.1 — Write the CLI.** Create `.claude/scripts/lease/reclaim.py`:

```python
"""Reclaim helper (AC-2.3/2.4, INV-3). Evaluate a TARGET session's claim:
- fresh  -> caller must pick the next work unit (AC-1.3)
- reclaimed -> caller now owns the work unit (we overwrote owner/pid/last_active)
- lost   -> a concurrent reclaimer won (re-read showed a different owner)
Usage: reclaim.py <my_session_id> <target_session_id>"""
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
        print("fresh")  # AC-1.3 -> caller selects next work unit
        return 0
    # Stale -> attempt reclaim. AC-2.3: overwrite owner/pid/last_active onto OUR claim,
    # and stamp ownership transfer onto the target path so concurrent reclaimers collide here.
    new_claim = {
        "owner": my_id,
        "pid": os.getppid(),
        "last_active": datetime.now(timezone.utc).isoformat(),
        "resume_pointer": (claim or {}).get("resume_pointer", ""),
    }
    os.makedirs(LEASES, exist_ok=True)
    fd, tmp = tempfile.mkstemp(dir=LEASES, suffix=".tmp")
    with os.fdopen(fd, "w", encoding="utf-8") as fh:
        json.dump(new_claim, fh)
    os.replace(tmp, target_path)  # atomic
    # AC-2.4 / INV-3: RE-READ and proceed only if owner is us.
    reread = lease_lib.parse_claim(_read_raw(target_path))
    if reread and reread.get("owner") == my_id:
        print("reclaimed")
        return 0
    print("lost")  # a concurrent reclaimer overwrote us -> select next work unit
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("usage: reclaim.py <my_session_id> <target_session_id>")
        sys.exit(2)
    sys.exit(main(sys.argv[1], sys.argv[2]))
```

  - [x] **Step 6.2 — Test fresh path.** Run: `echo '{"session_id":"freshowner","cwd":"."}' | python .claude/scripts/lease/heartbeat.py && python .claude/scripts/lease/reclaim.py me freshowner`
    Expected: `fresh` (just-written claim is within TTL).
  - [x] **Step 6.3 — Test reclaim path.** Manually write a stale claim, then reclaim:
    `python -c "import json,datetime;open('.claude/leases/stale1.json','w').write(json.dumps({'owner':'dead','pid':1,'last_active':(datetime.datetime.now(datetime.timezone.utc)-datetime.timedelta(hours=2)).isoformat(),'resume_pointer':'finish step 3'}))"`
    then `python .claude/scripts/lease/reclaim.py me stale1 && cat .claude/leases/stale1.json`
    Expected: prints `reclaimed`; file now shows `"owner":"me"` and preserved `"resume_pointer":"finish step 3"`.
  - [x] **Step 6.4 — Cleanup + commit.** `rm -f .claude/leases/freshowner.json .claude/leases/stale1.json` then `git add .claude/scripts/lease/reclaim.py && git commit -m "feat(lease): single-winner reclaim CLI (re-read enforces INV-3)"`

---

## Phase 4 — Config wiring [SEQUENTIAL — single-writer on settings.json]

- [x] **Task 7: Register heartbeat hook + gitignore leases** [SEQUENTIAL]
  - **Produces:** edits to `.claude/settings.json` (PostToolUse + Stop entries) and `.gitignore` (`.claude/leases/`)
  - **Consumes:** `heartbeat.py`
  - **Risk:** Medium — `.claude/settings.json` is a hotspot/single-writer file (workflow.md Sequential-only registry). Use the `update-config` skill mechanism; do not hand-merge concurrently.
  - **Files owned:** `.claude/settings.json`, `.gitignore`
  - **Demo:** after wiring, performing any tool call in a live session creates/updates `.claude/leases/<session_id>.json`; `git status` never lists files under `.claude/leases/`.
  - **Review lane:** Architectural (touches harness config).

  - [x] **Step 7.1 — Gitignore the leases dir.** Append to `.gitignore`:

```
# Session-continuity lease claim files (ephemeral, per-machine — never commit)
.claude/leases/
```

  - [x] **Step 7.2 — Register the heartbeat under PostToolUse (all tools).** Using the `update-config` skill, add to `.claude/settings.json` `hooks.PostToolUse` a NEW entry (do not modify existing entries):

```json
{
  "hooks": [
    { "type": "command",
      "command": "python .claude/scripts/lease/heartbeat.py 2>/dev/null || true" }
  ]
}
```

  (No `matcher` key = fires for every tool, satisfying AC-3.1.)

  - [x] **Step 7.3 — Register the heartbeat under Stop.** Add to `.claude/settings.json` `hooks.Stop` a NEW command entry alongside the existing ones:

```json
{
  "hooks": [
    { "type": "command",
      "command": "python .claude/scripts/lease/heartbeat.py 2>/dev/null || true" }
  ]
}
```

  - [x] **Step 7.4 — Validate JSON.** Run: `python -c "import json; json.load(open('.claude/settings.json')); print('settings.json OK')"`
    Expected: `settings.json OK`.
  - [x] **Step 7.5 — Commit.** `git add .claude/settings.json .gitignore && git commit -m "chore(lease): wire heartbeat hook (PostToolUse+Stop), gitignore .claude/leases/"`

---

## Phase 5 — workflow.md rule edits [HANDOFF — WRITE-PROTECTED]

- [x] **Task 8: Propose workflow.md Rule 4 / 7 / 8 edits (Helder applies)** [SEQUENTIAL]
  - **Produces:** a proposed-diff artifact ONLY (e.g. `Docs/Management/DevCycleCraft/session-continuity-leasing/workflow-edits-proposed.md`) — NOT an edit to `workflow.md`.
  - **Consumes:** `reclaim.py`, `resume.py`, the claim-file mechanism
  - **Risk:** HIGH / HANDOFF — `.claude/settings.json` denies `Edit/Write(.claude/rules/*.md)`; CLAUDE.md § Amending These Rules requires an `amend:` commit + changelog. **A subagent must NOT and CANNOT edit `workflow.md`.** Produce the text; Helder applies it.
  - **Files owned:** `Docs/.../workflow-edits-proposed.md` only.
  - **Demo:** the proposed-edits doc lists exact insert text for Rule 4 (`[~]` reclaim semantics: run `reclaim.py`; on `reclaimed` proceed, on `fresh`/`lost` pick next), Rule 7 (session-start: run reclaim/freshness check before claiming), Rule 8 (collision check gains a liveness step via `lease_lib.classify`).
  - **Review lane:** Architectural — Helder sign-off mandatory.

  - [x] **Step 8.1 — Write the proposed-edits doc.** Create `workflow-edits-proposed.md` describing, with exact before/after snippets, the three edits:
    - **Rule 4** — extend the `[~]` semantics: "Before treating a `[~]` task as blocked, run `.claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>`. `reclaimed` → take over and read the resume pointer; `fresh` → leave it and select the next `[ ]` task; `lost` → another session won, select next."
    - **Rule 7** — add a session-start step: "After reading the spec files, for each `[~]`/`🟡 In Progress` work unit, classify its claim (`.claude/scripts/lease/reclaim.py`) and reclaim any stale unit before starting new work; run `.claude/scripts/lease/resume.py <session_id>` to load the resume pointer."
    - **Rule 8** — add liveness to the collision check: "When a `[~]` task exists with no known running agent, classify the claim via `lease_lib.classify` instead of assuming abandonment; reset to `[ ]` only if `stale`."
  - [x] **Step 8.2 — Commit the artifact (not workflow.md).** `git add Docs/Management/DevCycleCraft/session-continuity-leasing/workflow-edits-proposed.md && git commit -m "docs(lease): propose workflow.md Rule 4/7/8 edits for Helder amend (rules dir write-protected)"`
  - [x] **Step 8.3 — HANDOFF.** Notify Helder: apply the proposed edits to `.claude/rules/workflow.md` with an `amend:` commit prefix + `Docs/Changelog/changelog.md` entry (CLAUDE.md § Amending These Rules). Register the new proposed-edits doc in `.sln` if it is to remain; otherwise delete after Helder applies.

---

## Phase 6 — In-session auto-resume wiring [SEQUENTIAL after Phase 4]

- [x] **Task 9: Scheduled-wakeup auto-resume wiring** [SEQUENTIAL]
  - **Produces:** documented `/loop`-based wakeup procedure + a `resume.py`-driven continuation; optional `SessionStart` resume hint entry in `.claude/settings.json`.
  - **Consumes:** `resume.py`, claim files, config wiring (Task 7)
  - **Risk:** Medium — `/loop` is session-bound (findings.md): in-session reset only; fully-closed terminal OUT of scope (AC-4.1 scope note).
  - **Files owned:** `Docs/.../auto-resume-runbook.md`; optionally `.claude/settings.json` (SessionStart, single-writer — serialize after Task 7).
  - **Demo:** after a simulated usage-window reset within the same session, the scheduled wakeup runs `resume.py <session_id>`, reads the resume pointer, and the agent continues the exact next step with no manual prompt.
  - **Review lane:** Architectural.

  - [x] **Step 9.1 — Write the runbook.** Create `auto-resume-runbook.md`: how to arm an in-session `/loop` wakeup that, on fire, runs `python .claude/scripts/lease/resume.py <session_id>` and feeds the printed pointer back as the continuation instruction. State explicitly: fully-closed terminal requires a cloud routine (`/schedule`) and is out of scope.
  - [x] **Step 9.2 — (Optional) SessionStart resume hint.** If desired, add a `SessionStart` command entry (single-writer on settings.json — only after Task 7 is committed) that, when `source` is `resume`, echoes a reminder to run `resume.py`. Validate JSON as in Step 7.4.
  - [x] **Step 9.3 — Commit.** `git add Docs/Management/DevCycleCraft/session-continuity-leasing/auto-resume-runbook.md .claude/settings.json && git commit -m "feat(lease): in-session auto-resume runbook + optional SessionStart resume hint"`

---

## Phase 7 — Verification (outermost) [SEQUENTIAL — last]

- [x] **Task 10: Two-terminal demo + final verification** [SEQUENTIAL]
  - **Produces:** a verification record appended to `task-log.md` (Demo Statement evidence + AC traceability matrix).
  - **Consumes:** everything above.
  - **Risk:** Low. Integration acceptance.
  - **Files owned:** `Docs/Management/DevCycleCraft/session-continuity-leasing/task-log.md`
  - **Demo:** executes the spec's Demo Statement end-to-end (two terminals: A claims+works, B finds fresh→picks other task; A `/clear`ed; after TTL B finds stale→reclaims→reads pointer→continues) with NO Helder arbitration.
  - **Review lane:** Architectural.

  - [x] **Step 10.1 — Run the unit suite.** Run: `python -m unittest discover -s .claude/scripts/lease/tests -v` → all PASS.
  - [x] **Step 10.2 — Two-terminal collision check (AC-1.1/1.3).** Terminal A does a tool call (heartbeat writes `A.json`). In terminal B run `reclaim.py B <A_session_id>` → expect `fresh`; B selects another task.
  - [x] **Step 10.3 — Reclaim-after-interruption (AC-2.x/4.x).** `/clear` terminal A (its `session_id` retires). Age/inspect the claim; in B run `reclaim.py B <A_session_id>` → expect `reclaimed`; then `resume.py B` prints A's resume pointer; B continues the exact next step.
  - [x] **Step 10.4 — Record evidence.** Append to `task-log.md`: Demo Statement PASS/FAIL, the AC traceability matrix from plan.md with the test method / command that verified each row.
  - [x] **Step 10.5 — Commit.** `git add Docs/Management/DevCycleCraft/session-continuity-leasing/task-log.md && git commit -m "test(lease): two-terminal demo verification + AC traceability evidence"`

---

## Phase 8 — Enhancements: lease↔ledger↔checkpoint linking (APPROVED by Helder 2026-07-14, BACKLOG row 2026-07-14)

- [x] **Task 11: Extend claim record with location fields + canonical resume-pointer redefinition**
  - **Produces:** claim schema gains `branch` / `worktree` / `task_id`; heartbeat populates them cheaply (read `.git/HEAD` from cwd — NO `git` subprocess, per heartbeat cost budget); `resume_pointer` canonical value documented as a pointer to the task-log `### Checkpoint` block (session-ops.md § Checkpoint Ping).
  - **Consumes:** `heartbeat.py`, `lease_lib.py`, `resume.py`, `reclaim.py` (preserve new fields on reclaim), tests.
  - **Risk:** Medium — hook runs after every tool call; must stay cheap and fail-open.
  - **Files owned:** `.claude/scripts/lease/*.py`, `.claude/scripts/lease/tests/test_lease_lib.py`
  - **Demo:** a heartbeat-written claim file contains correct `branch`/`worktree`/`task_id`; unit suite green.

- [x] **Task 12: Self-maintaining resume pointer**
  - **Produces:** heartbeat defaults an EMPTY `resume_pointer` from `.claude/active-task.json` (best-effort — file may be stale; never overwrite a non-empty pointer); Checkpoint Ping step in `session-ops.md` gains "run `resume.py --set`" line.
  - **Files owned:** `.claude/scripts/lease/heartbeat.py`, `lease_lib.py`, tests, `.claude/library/session-ops.md`
  - **Demo:** fresh session with no `--set` still yields a non-empty pointer naming the active task-log Checkpoint.

- [x] **Task 13: Lease GC**
  - **Produces:** heartbeat deletes claim files with `last_active` older than 7 days (skip its own; fail-open; bounded cost — tolerate one `os.scandir` of the small leases dir).
  - **Files owned:** `.claude/scripts/lease/heartbeat.py`, `lease_lib.py`, tests
  - **Demo:** 86-file leases dir shrinks to live claims after one heartbeat.

- [ ] **Task 14: Verification — demo re-run + in-session wakeup status**
  - **Produces:** task-log evidence — unit suite PASS; scripted reclaim/resume demo with new fields; in-session wakeup re-verified post-`asyncRewake` removal, or AC-4.1 marked SUPERSEDED by the LEDGER→Checkpoint→manifest chain. Two-terminal live demo remains a Helder gate (hand off).
  - **Files owned:** `task-log.md`

- [ ] **Task 15: Cleanup — merged branch/worktree + stale agent worktree triage**
  - **Produces:** `feature/session-continuity-leasing` branch + `.worktrees/session-continuity-leasing` worktree deleted (fully merged, verified); triage report of ~30 stale `.claude/worktrees/agent-*` worktrees (Helder decision before mass delete).
  - **Files owned:** git state only + `task-log.md`

---

## Post-completion handoffs (Helder)

1. ~~**Apply Phase 5** — edit `.claude/rules/workflow.md` Rules 4/7/8 per `workflow-edits-proposed.md` with an `amend:` commit + changelog entry (write-protected, R1).~~ **DONE 2026-06-14** — block temporarily lifted under Helder authorization, all three edits applied to `workflow.md`, `amend:` commit made on branch `session-continuity-leasing`, changelog entry added, block restored. Merged to `develop`. Only the live two-terminal manual demo remains pending (Helder).
2. **Decide feature-scope BACKLOG claiming** — this plan implements the session/`[~]`-step layer; whether BACKLOG `🟡 In Progress` rows also get a claim file is flagged as a follow-up (plan.md § Deferred).
3. **Register any new docs** (`workflow-edits-proposed.md`, `auto-resume-runbook.md`) in `MyVocaList.sln` if they are to persist.
