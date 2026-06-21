# Plan — BACKLOG-first Registration Enforcement

> Independent planner output. READ-ONLY task: this file is the only artifact; no rules/hooks/specs were edited.
> Source backlog row: `Docs/Management/BACKLOG.md` line 150 (Dev Cycle Craft table, `💡 Pending`, 2026-06-20).

---

## 0. Verified facts (grounding for every decision below)

Confirmed by direct inspection during planning:

1. **Two memory channels — only the device one is the target.** Device auto-memory lives at
   `C:\Users\helde\.claude\projects\C--Users-helde-source-repos-MyVocaList\memory\` (16 files: `MEMORY.md` + `project_*` + `feedback_*`). It is **outside** `CLAUDE_PROJECT_DIR`, not git-tracked, not in `.sln`, not a tiered-governance tier. The in-repo `.claude/memory-bank/MEMORY.md` directory is **empty** (Analysis B's "in-repo memory-bank" is currently unused). So enforcement targets **only** the device dir. (Analysis B's core discovery is correct.)
2. **Device memory writes are NOT observable by the existing PostToolUse `Edit|Write` matcher.** The harness writes memory; it does not flow through the project `Write`/`Edit` tools. The `changed-files.txt` appender (settings.json lines 47–48) and `sync-docs-to-sln.ps1` only ever see `tool_input.file_path` of project file edits. **Whether ANY hook event fires on a memory write is unknown → this is the spike.**
3. **Stop hook STEP 5 already exists** (settings.json line 127) and correlates `*task-log*` vs `BACKLOG.md` via `git diff --name-only HEAD` + `git ls-files --others`. It is **non-blocking** ("Continue regardless — this is a reminder, not a blocker") and **structurally blind to memory** (memory is outside the repo → never in `git diff`).
4. **Hook health expected-keys list** (settings.json line 93): `['TaskCreated','PreToolUse','PostToolUse','PostCompact','TaskCompleted','Stop','SessionStart']`. Any new top-level hook key must be added here or SessionStart will warn.
5. **Rules dir + CLAUDE.md are Edit/Write-denied** (settings.json lines 12–15). `.claude/scripts/**`, `.claude/library/**`, `.claude/leases/**` are **NOT** denied → tooling is directly implementable; rule text changes must be delivered as **proposed diffs** for Helder to apply under the Amending-These-Rules process.
6. **`lease_lib.py` is the precedent** for a pure, unit-testable, side-effect-free decision function with a companion thin hook wrapper (`heartbeat.py`/`reclaim.py`). The new logic must mirror this split.
7. **`.sln` auto-registration works for new DevCycleCraft spec files.** `sync-docs-to-sln.ps1` maps `Docs\Management\DevCycleCraft\` → GUID `{0C4BA720-519E-4818-BD9B-34AC19E4FCD7}` (folderMap line 51) and runs on every `Write`. A new `Docs/Management/DevCycleCraft/backlog-first-registration/` folder auto-registers; no manual `.sln` edit or GUID allocation needed (next free `FA1234BC` seq is `...029`, but it will not be required).
8. **Fail-open house style is universal**: every command hook ends `2>/dev/null || true`; lease scripts `sys.exit(0)` on error. New tooling must follow this — never break a session on its own failure.

---

## 1. Recommended approach — **Hybrid D + C, with a spike-gated B add-on (NOT A)**

Both analysts recommend a hybrid. They agree on the spine and diverge only on the hook flavour. My recommendation:

| Layer | Decision | Rationale |
|-------|----------|-----------|
| **D — Rule strengthening (PRIMARY, ships first)** | **ADOPT.** Sharpen Rule 1's "Proactive BACKLOG triage" into a hard, defined obligation + add device auto-memory as a named **sixth tier** in the tiered-memory governance table, explicitly stating memory is *not* a registration surface. Define "work item" and the three exempt categories. | Closes the definition gap both analyses flag. The only layer that is correct *independently of any hook capability*. Unenforced-but-clear beats enforced-but-wrong. |
| **C — Review-gate / checklist backstop (PRIMARY, ships first)** | **ADOPT.** Add a "BACKLOG orphan check" line to the subagent exit checklist + a `/project:review` lane. Provide a deterministic helper script (`backlog_orphan_check.py`) a reviewer/Stop hook can call to *list* memory files changed this session and ask "does any describe untracked work?" | Highest false-positive discrimination because a reasoning agent applies the exempt-category discriminator. Works even if no memory hook event exists. |
| **A — Stop-hook gate upgrade** | **REJECT as a blocker; ADOPT only as a non-blocking reminder folded into the existing STEP 5.** | Blocking on a memory-vs-BACKLOG diff has unacceptable false-positive fatigue (feedback_*/resume-pointer/status writes), can strand **background sessions that have no human to override**, and risks clashing with the auto-commit Stop agent. mtime/hash baseline of an out-of-tree dir is brittle across OS/profile. A's only safe form is an advisory line. |
| **B — PostToolUse memory interception** | **SPIKE-GATE.** Implement only if the spike proves a memory write emits a hook-observable event carrying a usable path. | Cheapest, most precise *if* the event exists; **provably zero efficacy if it does not** (fact #2). Must not be built on an unverified assumption — that would be governance theater. |

**Net posture: advisory + definitional, never hard-blocking.** This is deliberate and matches the project's fail-open discipline and the existence of background/headless sessions. The "block session end" wording in the backlog row is softened to "remind at session end + reviewer-enforced" — recorded as a scope decision in the spec for Helder's sign-off.

**Why not pure D?** Pure rules already exist and are ignored under pressure (the exact failure the item names). C adds a checkpoint with a human/agent judgment at a natural gate. **Why not lead with B?** Its premise is unverified (fact #2) — leading with it risks shipping a dead hook.

---

## 2. The spike (gates whether B is built at all)

```markdown
- [ ] **[SPIKE] Is a device auto-memory write observable by ANY Claude Code hook?**
  - Time-box: 60 min — hard stop
  - Question: When the harness writes/updates a file under the device memory dir
    (`~/.claude/projects/<mangled-key>/memory/*.md`), does any hook event fire
    (PostToolUse, a memory-specific event, Stop, or other), and if so does its
    payload carry a path/identifier usable to detect the write?
  - Method: temporarily add a logging command hook on every candidate event that
    appends `event + json.dumps(payload)` to a scratch log; trigger a memory write
    in a throwaway session; inspect the scratch log. Throwaway only — no production hook kept.
  - Success criterion: at least one event fires with a payload from which the memory
    write is detectable → Option B is viable; build the interception buffer.
  - Failure criterion: no event fires, OR events fire but no payload identifies the
    memory write → Option B is DEAD; ship D + C only; the Stop-hook stays advisory and
    memory-blind by design (documented limitation).
  - Mirrors: Session-Continuity AC-5 spike (which proved hooks expose `session_id`).
  - Artifact: `Docs/Management/DevCycleCraft/backlog-first-registration/findings.md`
  - Files owned: throwaway scratch hook + scratch log ONLY; no production files.
```

Main agent reads `findings.md` and updates the spec/plan before any B task is dispatched.

---

## 3. Definitions the spec must pin down (the discriminator)

Both analyses flag that without a precise "work item" definition the whole thing misfires. The spec must enumerate:

**A "work item"** (MUST get a BACKLOG row, nested under parent feature per bug-tracking.md nesting): a new business feature, a new Dev Cycle Craft activity, a bug, a deferred follow-up, or a material one-off investigation — i.e. anything that represents *future or tracked work the team should sequence*.

**Exempt — legitimately memory-only, MUST NOT be flagged** (the discriminator both analyses demand):
1. **`feedback_*` learnings** — preference/lesson captures, not work.
2. **`project_*` continuation pointers** — "NEXT:" / resume-from-here breadcrumbs for an *already-BACKLOG-tracked* item (they point *at* a tracked item; they are not a new item).
3. **Reference-fact caches** — email, date, architecture snapshots (e.g. `userEmail`, `currentDate`).
4. **Harness-AUTOMATIC captures** — auto-memory the agent did not author (Analysis B's last open question: agents are not responsible for BACKLOG rows for captures they did not write).

The discriminator is a **content heuristic in a pure function** (`lease_lib.py` precedent): given a changed memory file's diff/content, classify `work-item-candidate` vs `exempt`. Heuristic signals: filename prefix (`feedback_`/`project_` lean exempt), presence of "NEXT:"/"resume" (exempt pointer), vs new-noun work language ("add", "implement", "bug:", "build"). The heuristic only *raises a question for a human/reviewer*; it never auto-blocks.

---

## 4. Files to create / change

### Created directly (tooling — NOT write-protected)

| Path | Purpose |
|------|---------|
| `Docs/Management/DevCycleCraft/backlog-first-registration/requirements.md` | ACs, exempt categories, scope decision (advisory not blocking) |
| `…/backlog-first-registration/design.md` | Layered mechanism, interface signatures, hook integration |
| `…/backlog-first-registration/tasks.md` | Phased task list (§6) |
| `…/backlog-first-registration/plan.md` | Execution plan |
| `…/backlog-first-registration/task-log.md` | Activity log |
| `…/backlog-first-registration/findings.md` | Spike result |
| `.claude/scripts/backlog/backlog_lib.py` | **Pure** discriminator: `classify_memory_change(filename, diff_text) -> "work-item-candidate" | "exempt"`. Side-effect-free, unit-testable. Mirrors `lease_lib.py`. |
| `.claude/scripts/backlog/orphan_check.py` | Thin wrapper: enumerate memory files modified since session baseline, run `classify_*`, print human-readable "possible untracked work" list. Fail-open (`sys.exit(0)` on error). Reviewer/Stop hook calls this. |
| `.claude/scripts/backlog/test_backlog_lib.py` | Unit tests for the pure classifier (exempt vs candidate cases, incl. all 4 exempt categories). |
| `.claude/scripts/backlog/snapshot.py` *(only if spike fails AND we keep the mtime/baseline fallback)* | SessionStart: write memory-dir mtime/hash baseline to `.claude/.session-memory-baseline.json` so `orphan_check.py` can diff "changed this session". |

### Changed via `.claude/settings.json` (NOT write-protected — direct edit allowed)

| Change | Where | Detail |
|--------|-------|--------|
| Extend Stop STEP 5 | `Stop` agent prompt | Add: after the existing task-log/BACKLOG correlation, call `python .claude/scripts/backlog/orphan_check.py` and surface its output as a **reminder** (keep "Continue regardless — reminder, not blocker"). Do **not** add a new top-level hook key — fold into existing STEP 5 to avoid duplicate/colliding checks (both analyses warn against a second competing check). |
| *(spike-pass only)* Add memory-write buffer | `PostToolUse` | New command hook on the spike-confirmed event that appends detected memory writes to `.claude/changed-files-memory.txt` (parallel to `changed-files.txt`), consumed by `orphan_check.py`. |
| *(spike-fail only)* Add baseline snapshot | `SessionStart` | Prepend `python .claude/scripts/backlog/snapshot.py` before the hook-health check. |
| Hook-health expected-keys | `SessionStart` health line | **Only if** a brand-new top-level hook key is introduced. The chosen design folds into existing keys → **no expected-keys change needed**. Documented so a reviewer doesn't add one spuriously. |

### Delivered as PROPOSED DIFFS for Helder (write-protected — Amending These Rules)

| File | Change | Process |
|------|--------|---------|
| `.claude/rules/workflow.md` | Rule 1 "Proactive BACKLOG triage": upgrade from prose to a defined obligation — "A work item (def §3) MUST have a BACKLOG row in the same session it is identified; **memory is never the sole home for a work item**." Add to the Hook-enforced/Self-enforced split table. | `amend:` commit by Helder + `Docs/Changelog/changelog.md` entry (old rule / new rule / effective date). |
| `.claude/library/session-ops.md` | Tiered-memory governance: add **device auto-memory as a named sixth tier**, marked "single-device cache, NOT a registration surface; never the sole home of a work item." | Same `amend:` + changelog process. |
| `CLAUDE.md` *(only if needed)* | If the work-item definition needs constitutional visibility, a one-line pointer to the rule. Prefer keeping detail in `workflow.md`/`session-ops.md` (CLAUDE.md < 600-line governance). | `amend:` + changelog. |

> Deliver these three as a single `proposed-diffs.md` (or inline in the spec) so Helder applies them in one `amend:` commit. The implementing agent must **not** Edit these files (deny rules will block it; that is the intended gate).

### `.sln`
**No manual `.sln` edit required.** The new spec folder under `Docs/Management/DevCycleCraft/` auto-registers via `sync-docs-to-sln.ps1` (folderMap → `{0C4BA720…}`) on first `Write`. Verify after creation that each new `.md` appears in the `.sln` (HARD GATE check in the exit checklist). If `sync-docs-to-sln` warns "no mapping", fall back to a manual nested entry under DevCycleCraft GUID (it won't — the prefix matches).

---

## 5. Acceptance criteria

- **AC-1** (definition): The spec enumerates "work item" + the 4 exempt categories; a reader can classify any example without asking. *(Testability gate)*
- **AC-2** (rule, proposed-diff): `workflow.md` Rule 1 states memory is never the sole home for a work item, same-session BACKLOG-row obligation. Delivered as `amend:` diff with changelog old/new/effective. **Not self-applied** by the implementing agent.
- **AC-3** (tier): `session-ops.md` lists device auto-memory as a sixth governance tier marked non-registration-surface. Same `amend:` process.
- **AC-4** (pure classifier): `backlog_lib.classify_memory_change` returns `exempt` for all 4 exempt categories and `work-item-candidate` for new-work language — proven by `test_backlog_lib.py` (red→green per `testing.md`, Tester/Builder split or single-agent one-at-a-time).
- **AC-5** (advisory hook): At session end, if a memory file classifies as a work-item-candidate and `BACKLOG.md` was not changed this session, STEP 5 prints a reminder. **Never blocks.** Background sessions complete normally.
- **AC-6** (fail-open): `orphan_check.py` / classifier errors → silent `exit(0)`, session unaffected (verified by feeding malformed input).
- **AC-7** (no false positive on legit use): A session that writes only a `feedback_*` learning or a `project_*` "NEXT:" pointer produces **no** reminder.
- **AC-8** (spike gate): Option B is implemented **iff** the spike's success criterion is met; otherwise findings record it dead and only D + C + advisory-A ship.
- **AC-9** (.sln): every new spec `.md` is registered in `MyVocaList.sln` (auto or manual) in the same commit.
- **AC-10** (no STEP-5 collision): exactly one BACKLOG-freshness check exists at Stop; the memory check is folded into it, not duplicated.

---

## 6. Phased task breakdown (DRY-onion order: rules/defs → pure logic → tooling → hook wiring → backstop)

**Phase 0 — Spec (full ceremony; this is a Dev Cycle Craft activity, ≥ cross-cutting).**
1. Brainstorm + write `requirements.md`/`design.md`/`tasks.md` (def §3, ACs §5, scope decision = advisory). Spec-reviewer subagent → Helder approval. Update BACKLOG row → `📋 Spec` then `🗺️ Plan` → `🟢 Ready`.

**Phase 1 — Spike (blocks Phase 4 only).**
2. Run the §2 spike → `findings.md`. Update design with B verdict.

**Phase 2 — Definitions / rule diffs (innermost; no code).**
3. Author `proposed-diffs.md` for `workflow.md` + `session-ops.md` (+ optional CLAUDE.md pointer). Hand to Helder for `amend:` + changelog. *(Gate: Helder applies; agent does not.)*

**Phase 3 — Pure logic (Tester then Builder).**
4. `test_backlog_lib.py` — write failing tests for the 4 exempt categories + candidate cases (red).
5. `backlog_lib.py` — implement `classify_memory_change` to green. Pure, no I/O.

**Phase 4 — Tooling + hook wiring (outer).**
6. `orphan_check.py` thin wrapper (fail-open). + `snapshot.py` baseline *(only if spike failed)* / `changed-files-memory.txt` buffer *(only if spike passed)*.
7. Wire into `settings.json` Stop STEP 5 (fold-in, advisory). + PostToolUse/SessionStart edit per spike verdict. Verify hook-health expected-keys unchanged (or updated if a new key was unavoidable).

**Phase 5 — Backstop + close.**
8. Add "BACKLOG orphan check" line to subagent exit checklist (workflow.md proposed-diff — fold into Phase 2 diff) and a `/project:review` lane note.
9. Verification pass (§7); update BACKLOG row → `✅ Done`; session-end spec ritual; commit/push.

> Sequential constraints: Phase 3 before Phase 4 (tooling consumes the classifier). Phase 1 gates only task 6's B-branch. Phase 2 is independent and can run parallel to Phase 1/3 but its application is a Helder gate. Single-writer hotspots: `settings.json` (one writer for tasks 6–7), `tasks.md`, `MauiProgram.cs` (untouched here).

---

## 7. Verification approach

- **Pure classifier**: `python .claude/scripts/backlog/test_backlog_lib.py` (or pytest) — red before, green after; cover all 4 exempt + candidate cases; malformed-input → exempt/no-crash.
- **Fail-open**: pipe garbage / point at a missing memory dir → `orphan_check.py` exits 0, prints nothing harmful.
- **Advisory behaviour, manual E2E** (UI-untestable, document in task-log per bug-tracking Major rule): (a) session writes a new-work `project_*` note + NO BACKLOG edit → Stop prints reminder; (b) session writes only a `feedback_*` learning → no reminder (AC-7); (c) session writes work note AND a BACKLOG row → no reminder.
- **Background-session safety**: confirm the hook never returns a non-zero/blocking exit (grep the hook command ends `|| true`; the agent prompt says "Continue regardless").
- **.sln gate**: after spec-folder creation, confirm each `.md` is present in `MyVocaList.sln`.
- **Rule diffs**: confirm `amend:` prefix + changelog old/new/effective present in Helder's commit before marking `✅ Done` (the agent verifies, does not author the commit).
- **No STEP-5 duplication**: read final Stop prompt — exactly one BACKLOG-freshness block (AC-10).

---

## 8. Risks & mitigations

| Risk | Mitigation |
|------|-----------|
| **B built on false premise** (memory write unobservable) | Spike §2 gates B entirely; D+C ship regardless and are independently sufficient. |
| **False-positive fatigue** (feedback/pointer/status writes flagged) | 4-category exempt discriminator in a *tested* pure function; advisory-only so a false positive costs a glance, not a blocked session. AC-7 explicitly tests legit-use silence. |
| **Blocking strands background/headless sessions** (no human to override) | Posture is advisory by design; never a hard block. Scope decision recorded for Helder (softens the backlog row's "block session end"). |
| **Brittle out-of-tree path resolution** (mangled key, OS/profile home) | Prefer the spike-confirmed hook payload (carries the path) over hand-resolving `~/.claude/projects/<key>/`. If we must resolve it (spike-fail fallback), derive the mangled key from `CLAUDE_PROJECT_DIR` and fail-open if the dir is absent. |
| **Collision with auto-commit Stop agent / duplicate STEP-5 check** | Fold into existing STEP 5; run before STEP 6 auto-commit; add no new top-level Stop hook (AC-10). |
| **Rule self-edit blocked / silently skipped** | Deny rules block the agent (intended). Deliver `proposed-diffs.md`; mark the rule ACs as "pending Helder `amend:`"; do not mark `✅ Done` until applied. |
| **Hook-health WARNING from a stray new key** | Design folds into existing keys → no expected-keys change. If a new key becomes unavoidable, the same commit updates the expected list (settings.json line 93). |
| **Breaking legitimate memory use** | Constitution of the feature: memory remains valid for the 4 exempt categories; the rule says memory is not the *sole* home of a *work item* — it does not forbid memory. |
| **Heuristic mis-classifies novel cases** | Heuristic only raises a question to a reasoning agent/human (Layer C); the human applies final judgment. Escalation: reclassify + add a test case (mirrors testing.md escalation). |

---

## 9. Where Helder must decide (call-outs)

1. **Posture confirmation**: advisory-only (recommended) vs the backlog row's literal "block session end". Recommendation: advisory, because of background sessions + fail-open discipline. *(Recorded as a scope decision in requirements.md.)*
2. **CLAUDE.md touch**: keep the work-item definition in `workflow.md`/`session-ops.md` only (recommended, respects the 600-line governance) vs a one-line constitutional pointer.
3. **Spike-fail fallback**: if memory writes are unobservable, accept the mtime/hash baseline (`snapshot.py`) despite its brittleness, or drop the hook entirely and rely on C (reviewer) alone. Recommendation: drop the baseline; rely on C — a brittle baseline reintroduces the false-positive risk we rejected in Option A.
