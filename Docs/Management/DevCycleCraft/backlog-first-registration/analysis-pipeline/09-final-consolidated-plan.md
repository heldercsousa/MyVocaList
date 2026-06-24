# 09 — FINAL CONSOLIDATED PLAN (authoritative deliverable)

> Wave-3 synthesis plan (`05-final-plan-synthesis.md`) with EVERY required review change applied:
> all 8 of Review 1's required changes + Review 2's two unique correctness catches + R2's documented
> limitation. **This is the plan that feeds the feature's Phase 0 spec.** Tags like (R1-4)/(R2-Corr2)
> trace each change to its source review item.

## 3.0 Pre-spec gate — RAISED TO TOP (was buried as a reserved decision)
**DEVIATION FROM BACKLOG TEXT — requires Helder ratification BEFORE Phase 0.** BACKLOG line 150
literally asks for a gate that *"blocks session end unless BACKLOG was updated."* This plan recommends
**advisory, never-blocking** instead, on three live-verified grounds: (a) fail-open house style (every
command hook ends `2>/dev/null || true`; lease scripts `sys.exit(0)`); (b) background/headless sessions
(like the pipeline that produced this plan) have no human to override a block; (c) the existing Stop
STEP 5 was deliberately authored as "a reminder, not a blocker." Per the SDD invariant ("spec changes
before code changes"), this reversal must be ratified by Helder at the Phase 0 gate, and **Phase 4
hook-wiring must not proceed while the posture decision is open.** (R1-2 / R2-#1)

## 3.1 Chosen approach
**Hybrid: D (rule strengthening) + C (review/checklist backstop) ship unconditionally; A folded into
Stop as a non-blocking, classifier-driven advisory; B (memory-write interception) built only if the
spike proves a device-memory write emits a hook-observable event. No hard-blocking. No mtime baseline.**

Corrected rationale (R1-1): device memory (16 live out-of-tree files) is the real target because it is
**not team-visible and not in git**. The prior "in-repo `memory-bank/` is an empty stub already covered
by `changed-files.txt`" rationale is **FALSE and removed** — `.claude/memory-bank/MEMORY.md` is
git-tracked (~2410 bytes), and `changed-files.txt` only records in-session Edit/Write paths so it would
not capture a harness-injected memory file anyway. The device-only scope still holds, re-derived from
the true fact (the device dir is the genuinely team-invisible surface).

## 3.2 Files to create / change

### A. Directly editable (NOT deny-listed) — implement in-session
- `Docs/Management/DevCycleCraft/backlog-first-registration/{requirements,design,tasks,plan,findings,task-log}.md`
- `…/proposed-diffs.md` — the write-protected rule diffs for Helder (see B)
- `.claude/scripts/backlog/backlog_lib.py` — pure `classify_memory_change(filename, line_or_diff)` +
  pure `should_remind(classified_changes, backlog_changed_this_session) -> (bool,str)`. Mirrors
  `lease_lib.py`. **(R2-Corr2)** classification is **line/content-level**, so a new-work line inside
  `MEMORY.md` is a candidate even though `MEMORY.md` is otherwise auto-captured. **(R2-precedence)**
  explicit precedence rule when an exempt marker and a new-work verb co-occur on the same line.
- `.claude/scripts/backlog/tests/test_backlog_lib.py` — **(R2-test-path:** `tests/` subdir to match the
  lease precedent). Covers: 4 exempt categories → exempt; new-work → candidate; **adversarial conflict
  cases** ("NEXT: implement X") per the precedence rule; backlog-already-changed → no reminder;
  empty/garbage → exempt + no crash.
- `.claude/scripts/backlog/orphan_check.py` — thin fail-open (`sys.exit(0)`) wrapper. **(R1-5/R2)** the
  device-dir path is **injected/parameterized**, not hardcoded-mangled, so it is unit-testable against a
  fixture dir; enumerates changed memory files (signal per spike), calls the classifier, prints advisory.
- `.claude/settings.json` — **(R1-3)** add `orphan_check.py` as a **NEW command-type entry under the
  existing `Stop` key** (mirroring the `heartbeat.py` command entry), NOT woven into the Stop
  agent-prompt. Non-blocking. No new top-level key → SessionStart expected-keys unchanged (AC-10).
  (spike-pass only) add a memory-write buffer command hook under the existing `PostToolUse` key.
- `.claude/library/session-ops.md` — directly editable (in `library/`, NOT under the `rules/*.md` deny
  glob). Add device auto-memory as a **6th tier** ("single-device cache — NOT a registration surface").
  Route through Helder Authorship review anyway (it documents the governance model being amended). (D7)
- `MyVocaList.sln` — spec `.md` auto-register via `sync-docs-to-sln.ps1` (DevCycleCraft prefix →
  `{0C4BA720-…}`, on Write). **(R1-4)** ALSO add an explicit **manual** `.sln` registration task for
  `.claude/scripts/backlog/*.py` — the sync hook only handles `Docs\` paths (line 28) on Write (line 71),
  so the `.py` files are NOT auto-covered. **(R2-Corr1)** elevate per-file verification to an explicit
  gate (sync hook is Write-only + self-skips). Verify every new file (`.md` AND `.py`) appears in `.sln`.

### B. Write-protected — deliver as proposed diffs in `proposed-diffs.md`, applied by Helder
- `.claude/rules/workflow.md` (deny-listed) → Helder `amend:` + changelog triple. Upgrade Rule 1
  "Proactive BACKLOG triage" to a defined obligation: *"A work item MUST have a BACKLOG row in the same
  session it is identified; memory is never the sole home for a work item"* + the work-item definition +
  4 exempt categories; add a "BACKLOG orphan check" line to the Rule 2 exit checklist; add a row to the
  Hook-enforced/Self-enforced table. **(R1-8)** Authorship gate: Helder must *read and edit*, not
  rubber-stamp, the generated rule prose.
- `CLAUDE.md` (deny-listed) → recommend NO change (600-line budget); Helder-reserved one-line pointer
  only if wanted.
- `Docs/Changelog/changelog.md` → **(R1-8)** the `amend:` changelog triple lives in `proposed-diffs.md`
  until Helder applies it; the agent must NOT pre-write it into `changelog.md` (the `TaskCompleted`/
  `Stop` agent hooks already auto-touch that file and would collide).

## 3.3 The spike (run first; gates Option B only; D/C/A ship regardless)
**[SPIKE] Is a device-scoped auto-memory write observable by ANY Claude Code hook, AND is the device
dir path deterministically resolvable?** Time-box 60 min, hard stop. **(R1-5/R2:** lead with
path-determinism, since NO repo precedent resolves the out-of-tree mangled path — all lease scripts
resolve only `CLAUDE_PROJECT_DIR`; Option-B event-observability is secondary.) Method: throwaway logging
hook on candidate events + scratch log; trigger a memory write; inspect. Success → Option B viable.
Failure → Option B DEAD; ship D+C+advisory-A; advisory operates on the spike-confirmed signal or is
reviewer-driven; **do NOT** fall back to an mtime baseline. Artifact: `findings.md`. Mirrors the
Session-Continuity AC-5 spike.

## 3.4 Work-item vs exempt discriminator
**Work item** (MUST get a BACKLOG row, nested per `bug-tracking.md`): a new business feature, a new Dev
Cycle Craft activity, a bug, a deferred follow-up, or a material one-off investigation.
**Exempt (4 categories):** (1) `feedback_*` learnings; (2) `project_*` continuation pointers ("NEXT:"/
resume for an already-tracked item); (3) reference-fact caches (email, date, arch snapshots);
(4) harness-AUTOMATIC captures the agent did not author. **(R2-Corr2)** category 4 is applied at line
level, never as a blanket file exemption for `MEMORY.md`. **(R2-precedence)** documented precedence when
an exempt marker and a new-work verb co-occur.

## 3.5 Acceptance criteria
Keep AC-1..AC-11 from the synthesis plan, with these revisions:
- **AC-5** reworded to branch on spike outcome **(R1-6)**: "candidate among the session's changed files
  *as reported by the spike-confirmed signal, else reviewer-supplied*."
- **AC-9** broadened to include `.claude/scripts/backlog/*.py` **(R1-4)**.
- **AC-2** adds the Authorship read-and-edit requirement for workflow.md prose **(R1-8)**.
- **New AC-12** — `orphan_check.py` has deterministic unit tests for path-resolution/enumeration
  (fixture dir) and fail-open **(R2)**.
- **New AC-13** — classifier precedence proven by adversarial tests **(R2)**.

Base AC-1..AC-11 (from synthesis): AC-1 work-item def + 4 exempt categories classifiable without asking;
AC-2 workflow.md proposed-diff states "memory never the sole home" + same-session obligation,
amend:+changelog, not self-applied; AC-3 session-ops.md lists device memory as the 6th tier (NOT a
registration surface), edited directly + Authorship review; AC-4 `classify_memory_change` returns exempt
for all 4 exempt categories + candidate for new-work, red→green; AC-5 advisory fires at session end iff a
memory line classifies candidate AND BACKLOG not changed this session — never blocks, background sessions
complete; AC-6 fail-open (errors/missing dir → silent exit 0); AC-7 no false positive on legitimate use;
AC-8 spike gate (B iff spike success, else findings record DEAD, ship D+C+advisory-A); AC-9 every new file
registered in `.sln` same commit; AC-10 exactly one BACKLOG-freshness block at Stop, no new top-level key,
expected-keys unchanged; AC-11 non-negotiable — no legitimate non-work-item memory use is ever flagged.

## 3.6 Phased breakdown (DRY-onion) + sequencing
- **Phase 0 — Spec** (full ceremony; **posture ratified here per §3.0**). Brainstorm → requirements/
  design/tasks → spec-reviewer → Helder approval. BACKLOG `💡 → 📋 → 🗺️ → 🟢`. Create + register spec
  files in `.sln` (verify).
- **Phase 1 — Spike** (gates Phase 4 B-branch + path-resolution). Throwaway only → `findings.md` →
  update `design.md`.
- **Phase 2 — Rule/def diffs** (innermost, no code). workflow.md proposed-diff (Helder-gated);
  session-ops.md direct edit + Authorship review.
- **Phase 3 — Pure logic** (Tester→Builder; Level A full TDD). Line-level classifier + precedence +
  adversarial tests → red→green.
- **Phase 4 — Tooling + hook wiring** (**gated on §3.0 posture**; SEQUENTIAL — `settings.json` hotspot
  single-writer). `orphan_check.py`; command-type Stop entry; (spike-pass) PostToolUse buffer; manual
  `.sln` for the `.py` files; verify expected-keys unchanged.
- **Phase 5 — Backstop + close.** **(R2)** apply the `review.md` lane note SEPARATELY from / after the
  workflow.md `amend:` so the two halves of the same change don't diverge in git history. Verification
  pass; session-end ritual; BACKLOG → `✅ Done` only after Helder applies the workflow.md `amend:`.

Sequencing: Phase 3 before Phase 4; Phase 1 gates only Phase 4's B-branch; Phase 2's workflow.md change
is Helder-gated/independent. Single-writer hotspots: `.claude/settings.json` (Phase 4 sequential),
`tasks.md`, `MyVocaList.sln`.

## 3.7 Known documented limitation (R2)
The advisory inherits STEP 5's coarse correlation: a session that updates BACKLOG for feature X while
writing a memory-only orphan for feature Y will suppress the reminder (BACKLOG "changed at all").
Defensible (matches existing STEP 5) but must be documented in `design.md`. Also specify the
`should_remind` suppression window precisely vs. the auto-commit hook — `git diff HEAD` won't see
already-committed in-session BACKLOG edits **(R1-7)**.

## 3.8 Decisions reserved for Helder
1. **Posture:** advisory (recommended) vs the literal "block session end" — **§3.0, ratify at Phase 0.**
2. **CLAUDE.md touch:** keep the definition in workflow.md/session-ops.md only (recommended) vs a
   one-line pointer.
3. **Spike-fail fallback:** reviewer-driven advisory (recommended; drop the mtime baseline) vs a brittle
   baseline.
4. **Dedicated `.sln` subfolder** for the spec (flat under DevCycleCraft default) vs a dedicated GUID row
   in `sync-docs-to-sln.ps1`'s folderMap.
5. **workflow.md wording:** approve the exact obligation phrasing + the 4 exempt categories before the
   `amend:` is applied.
