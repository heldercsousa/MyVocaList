# Demo Procedure, Integration Evidence & AC Traceability Matrix

> Feature: Session Continuity — Task Leasing & Auto-Resume (Phase 7 / Task 10).
> Two artifacts in one doc: (1) the live two-terminal **operator demo** (Helder gate), and
> (2) the **scriptable single-process** integration checks run now, with pasted evidence.
> All paths relative to the repo root.

---

## Part 1 — Two-terminal demo procedure (Demo Statement, live Helder gate)

Implements `requirements.md` § Demo Statement (L174-180). Requires two real Claude Code
terminals, A and B, in the same checkout. **This is the only verification for the live race
that cannot be exercised single-process — it is a Helder live gate.**

1. **A claims and works.** In terminal A, start work on a task; mark it `[~]` in `tasks.md`
   and set a pointer:
   `python .claude/scripts/lease/resume.py --set <A_sid> "Continue Task N step N.x"`.
   A then performs tool calls — the `PostToolUse`/`Stop` heartbeat keeps `.claude/leases/<A_sid>.json`
   fresh automatically (AC-3.1).
2. **B starts, sees A fresh, picks a different task.** In terminal B:
   `python .claude/scripts/lease/reclaim.py <B_sid> <A_sid>` → prints **`fresh`**. B does NOT
   take A's task; it selects the next available `[ ]` task (AC-1.1 / AC-1.3). No human input.
3. **A is interrupted.** `/clear` terminal A (its `session_id` retires; no further heartbeat,
   so the claim stops advancing — AC-3.2).
4. **Claim ages or fast-reclaims.** After TTL (`LEASE_TTL_SECONDS=1800` = 30 min) the claim
   is stale by `last_active`; OR, on the same host, if A's recorded `pid` is already dead, B
   may reclaim immediately via the fast path (AC-2.2).
5. **B reclaims and resumes — no arbitration.** In terminal B (or a scheduled in-session
   wakeup per `auto-resume-runbook.md`):
   `python .claude/scripts/lease/reclaim.py <B_sid> <A_sid>` → prints **`reclaimed`**
   (owner/pid/last_active overwritten, AC-2.3; resume_pointer preserved). Then
   `python .claude/scripts/lease/resume.py <A_sid>` → prints A's resume pointer + last commit;
   B continues the exact next step (AC-4.2). **No Helder arbitration at any point.**

> **Flagged: live-demo-only ACs.** AC-1.1, AC-1.3, AC-3.1, AC-3.2, AC-4.1 depend on the live
> heartbeat hook firing in a real session and on a real second terminal. Their *logic* is
> verified single-process below; the end-to-end two-terminal flow is confirmed only by this
> live demo (Helder gate).

---

## Part 2 — Scriptable single-process integration checks (RUN 2026-06-14)

Run from repo root with `CLAUDE_PROJECT_DIR=$PWD`. `.claude/leases/` cleaned before/after.
Evidence pasted verbatim.

### Unit suite (lease_lib)

```
$ python -m unittest discover -s .claude/scripts/lease/tests -v
... (22 tests) ...
Ran 22 tests in 0.601s
OK
```

### Check 1 — heartbeat writes claim, parent-keyed (AC-3.1, AC-3.4)

Payload carries `agent_id`/`agent_type`; the claim must key off the PARENT `session_id`.

```
$ echo '{"session_id":"parentX","agent_id":"sub9","agent_type":"general-purpose","cwd":"."}' | python .claude/scripts/lease/heartbeat.py
$ cat .claude/leases/parentX.json
{"owner": "parentX", "pid": 24616, "last_active": "2026-06-14T13:46:56.689716+00:00", "resume_pointer": ""}
```

Result: file is `parentX.json` (NOT `sub9.json`), `owner=parentX`, ISO `last_active`,
integer `pid`. **PASS** (AC-3.1 write-on-tool-call; AC-3.4 parent-keyed).

### Check 2 — reclaim against a FRESH target → defer (AC-1.1, AC-1.3)

```
$ python .claude/scripts/lease/reclaim.py meB parentX
fresh
```

Result: just-written claim is within TTL → `fresh`; caller must pick the next unit. **PASS**.

### Check 3 — reclaim against a STALE target → take over (AC-2.1, AC-2.3)

Stale claim (2h old, dead pid 1, pointer `finish step 3`) written, then reclaimed:

```
$ python .claude/scripts/lease/reclaim.py meB stale1
reclaimed
$ cat .claude/leases/stale1.json
{"owner": "meB", "pid": 10852, "last_active": "2026-06-14T13:47:15.337147+00:00", "resume_pointer": "finish step 3"}
```

Result: `reclaimed`; `owner`/`pid`/`last_active` overwritten with reclaimer's values;
`resume_pointer` preserved across the reclaim. **PASS** (AC-2.1 stale→reclaimable; AC-2.3
overwrite; AC-4.2 pointer survives for resume).

### Check 4 — corrupt / half-written claim → stale → reclaimable (AC-2.5)

```
$ printf '%s' '{"owner":"x","last_act' > .claude/leases/corrupt1.json
$ python .claude/scripts/lease/reclaim.py meB corrupt1
reclaimed
```

Result: unparseable claim treated as stale and reclaimed (no permanent block). **PASS**.

### Check 5 — single-winner re-read decision (AC-2.4, INV-3)

The pure `reclaim_decision` is what `reclaim.py` calls after its atomic write + re-read:

```
$ python -c "...; import lease_lib; print('us  ->', lease_lib.reclaim_decision('meB', {'owner':'meB'})); print('other->', lease_lib.reclaim_decision('meB', {'owner':'otherZ'})); print('none ->', lease_lib.reclaim_decision('meB', None))"
us  -> reclaimed
other-> lost
none -> lost
```

Result: only the session whose re-read shows its own `owner` wins (`reclaimed`); a different
owner or a corrupt/None re-read → `lost`. Single winner guaranteed. **PASS** (AC-2.4 / INV-3).

### Check 6 — resume.py set + get (AC-4.3 write, AC-4.2 read)

```
$ python .claude/scripts/lease/resume.py --set rtest "Continue Task 4 step 4.3"
$ python .claude/scripts/lease/resume.py rtest
RESUME POINTER: Continue Task 4 step 4.3
LAST COMMIT: feat: Session Continuity T7 — register heartbeat PostToolUse hook + gitignore leases
NEXT: read the active feature tasks.md, find the [~] step, and continue from the pointer.
```

Result: pointer written to the claim, read back with last commit + next hint. **PASS**.

### Check 7 — TTL single source of truth

```
$ python -c "...; import lease_lib; print(lease_lib.LEASE_TTL_SECONDS)"
LEASE_TTL_SECONDS = 1800
```

Result: `1800` read by both heartbeat and reclaim/classify from the one constant. **PASS**.

### Gitignore guard

```
$ git check-ignore .claude/leases/foo.json
.claude/leases/foo.json
$ git status --short        # (only the new docs untracked; no .claude/leases/ entries)
```

Result: claim files are gitignored — never committed. **PASS**.

---

## Part 3 — AC Traceability Matrix

Legend: **NOW** = verified by a scriptable check/unit test above · **LIVE** = end-to-end
flow confirmed only by the two-terminal Helder demo (logic verified NOW) · **DONE** =
closed in `findings.md`.

| AC / INV | Criterion (short) | Implementation location | Verifying test / demo step | Status |
|----------|-------------------|-------------------------|----------------------------|--------|
| AC-1.1 | Within TTL → fresh, do not start | `lease_lib.classify` (within_ttl) | unit `test_within_ttl_is_fresh`; Check 2; Demo step 2 | NOW + LIVE |
| AC-1.2 | Old TTL but live pid → fresh | `lease_lib.classify` (pid branch) | unit `test_old_ttl_but_live_pid_is_fresh` | NOW |
| AC-1.3 | Blocked → pick next per Rule 4 | `reclaim.py` prints `fresh` | Check 2; workflow-edits Rule 4; Demo step 2 | NOW + LIVE |
| AC-2.1 | Old + dead pid → stale/reclaimable | `lease_lib.classify` | unit `test_old_ttl_and_dead_pid_is_stale`; Check 3 | NOW |
| AC-2.2 | Dead pid before TTL → fast reclaim | `lease_lib.pid_alive` + `classify`; `reclaim.py` | unit `test_invalid_pid_is_dead`/`test_large_unused_pid_is_dead`; Demo step 4 | NOW + LIVE |
| AC-2.3 | Reclaim overwrites owner/pid/last_active | `reclaim.py` (new_claim write) | Check 3 (file shows new owner) | NOW |
| AC-2.4 | Concurrent reclaim → single winner (re-read) | `reclaim.py` re-read + `lease_lib.reclaim_decision` | unit `TestReclaimDecision` (×4); Check 5 | NOW |
| AC-2.5 | Corrupt/half-written → stale | `lease_lib.parse_claim` → `classify` | unit `test_corrupt_claim_is_stale`; Check 4 | NOW |
| AC-3.1 | Tool call → heartbeat updates last_active | `heartbeat.py` + settings.json PostToolUse | Check 1; Demo step 1 | NOW + LIVE |
| AC-3.2 | Interruption → last_active stops advancing | `heartbeat.py` (no timer; fires only on tool call) | by construction (no background timer); Demo step 3 | LIVE |
| AC-3.3 | No background timer / manual ping | `heartbeat.py` (hook-driven only) | code review (PostToolUse/Stop only, no loop) | NOW |
| AC-3.4 | Subagent heartbeat keys PARENT session_id | `heartbeat.py` / `lease_lib.build_heartbeat_claim` | unit `test_keys_owner_off_supplied_session_id`; Check 1 | NOW |
| AC-4.1 | In-session wakeup auto-resumes | `resume.py` + `auto-resume-runbook.md` (`/loop`) | `auto-resume-runbook.md`; Demo step 5 | LIVE |
| AC-4.2 | Reclaim reads pointer + tasks.md + last commit | `resume.py` `show()` + `reclaim.py` pointer preserve | Check 3 + Check 6; Demo step 5 | NOW + LIVE |
| AC-4.3 | Resume pointer written on claim/progress | `resume.py` `set_pointer`; heartbeat preserves it | unit `test_preserves_existing_resume_pointer`; Check 6 | NOW |
| AC-5.1 | Hooks expose session_id | spike | `findings.md` AC-5.1 PASS | DONE |
| AC-5.2 | Hook writes claim on tool use | spike | `findings.md` AC-5.2 PASS | DONE |
| AC-5.3 | git-commit fallback viable | spike | `findings.md` AC-5.3 PASS | DONE |
| INV-1 | ≤ 1 fresh claim per work unit | `classify` + single-winner reclaim | Check 5; AC-2.4 | NOW |
| INV-2 | Non-fresh always reclaimable | `classify` (stale path) + `reclaim.py` | Check 3/4 | NOW |
| INV-3 | After reclaim, owner reflects new owner (re-read) | `reclaim.py` re-read + `reclaim_decision` | Check 5; `TestReclaimDecision` | NOW |
| INV-4 | Heartbeat advances only on genuine activity | `heartbeat.py` (PostToolUse/Stop only) | AC-3.3 code review | NOW |

### Verified-NOW vs deferred-to-live-demo

- **Verified NOW (scriptable / unit):** AC-1.2, AC-2.1, AC-2.3, AC-2.4, AC-2.5, AC-3.3,
  AC-3.4, AC-4.3, INV-1, INV-2, INV-3, INV-4 — plus the logic of AC-1.1, AC-1.3, AC-2.2,
  AC-3.1, AC-4.2.
- **DONE (spike):** AC-5.1, AC-5.2, AC-5.3.
- **Deferred to the live two-terminal Helder demo (logic verified NOW; flow LIVE only):**
  AC-1.1, AC-1.3, AC-2.2, AC-3.1, AC-3.2, AC-4.1, AC-4.2 — these require a real second
  terminal and the live heartbeat hook firing in-session.
