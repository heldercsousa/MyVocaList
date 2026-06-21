# 03 — Plan 1 (Wave 2, agent `aecddf1c7554c4008`) — verbatim

# PLAN — BACKLOG-first Registration Enforcement

## 1. Verified facts (from reading the live system)
- Two memory channels confirmed. (a) Device-scoped auto-memory at `…\.claude\projects\C--…-MyVocaList\memory\` — outside repo, outside git, dir currently empty/lazy-created, path depends on mangled project key + user profile. (b) In-repo `.claude/memory-bank/` — git-tracked, team-visible, .sln-registerable. Only (b) reliably observable by a project hook; (a) is not.
- Hook payload exposes `session_id`, `cwd`, `tool_input.file_path`, `tool_name` (heartbeat.py 41–54 + the PostToolUse changed-files hook). A PostToolUse hook can see in-tree writes (already appends to changed-files.txt), but a device-memory write does NOT flow through a project Write/Edit the matcher observes — written by the harness. Therefore Option B is dead for the device channel and only partially works for the in-tree channel (already captured by changed-files).
- Existing Stop STEP 5 already does the exact correlation shape (git-diff task-log vs BACKLOG, advisory). Integration point — extend, not duplicate.
- Governance gates: rules dir + CLAUDE.md Edit/Write-denied (12–15) → rule edits = proposed diffs. Hook/script/library not denied → tooling directly. SessionStart expected-keys (line 93) — a new top-level key requires updating; a sub-hook under an existing key does not. Fail-open house style. .sln HARD GATE. lease_lib.py = pure-function precedent.
- Lease layer exists (Session Continuity, merged); deferred sibling row line 149. This plan addresses registration of new items, not claiming existing rows.

## 2. Chosen approach — Hybrid (D + A-extended + C), B explicitly rejected, gated by one spike
REJECT Option B (device writes not observable; in-tree channel already captured by changed-files.txt). Layer 1 — Rule strengthening (D) normative foundation, proposed diff + amend: + changelog. Layer 2 — Stop-hook advisory extending STEP 5 (A, non-blocking): widen the trigger from "task-log changed" to "any substantive Docs/.claude work-artifact changed without a BACKLOG.md change" and flag in-tree memory-bank writes with no BACKLOG change; stays advisory (high FP rate; fail-open; STEP 5 history). Layer 3 — Review-gate semantic backstop (C): one line to the Rule 2 exit checklist + one check to /project:review. The spike gates whether Layer 2 can also watch the device channel; default it cannot, so Layer 2 watches in-tree only and the device channel is governed by Layers 1+3.

## 3. The spike
[SPIKE] Is a device-scoped auto-memory write observable to any project hook? 60 min. Question: when the harness writes to ~/.claude/projects/<mangled-key>/memory/*.md, does ANY hook receive a payload/signal letting a project script detect it? Method: trigger an auto-memory write; inspect PostToolUse; at Stop test path resolution + mtime-baseline reliability. Success → Layer 2 MAY add a device-memory mtime-baseline diff (still advisory). Failure → Layer 2 watches in-tree only; device channel = Layers 1+3. findings.md. No Layer-2 hook code until this returns.

## 4. Files
Spec folder `Docs/Management/DevCycleCraft/backlog-first-registration/` (requirements/design/tasks/findings/task-log/spec-changelog). .sln HARD GATE: add Solution Folder + fresh GUID (verify current max; constraints-registry says 0014 but Session Continuity added folders) + register files + NestedProjects under DevCycleCraft {0C4BA720-…}; existing sync-docs-to-sln.ps1 may auto-handle on Write — verify/reconcile, don't double-register. Tooling (not deny-listed): `.claude/scripts/backlog/registration_lib.py` (pure `needs_backlog_reminder(changed_files, memorybank_changed, backlog_changed) -> (bool,str)`); `.claude/scripts/backlog/tests/test_registration_lib.py`; `.claude/scripts/backlog/stop_check.py` (thin Stop wrapper, fail-open; prefer the script over inline). settings.json — extend existing Stop STEP 5 (no new top-level key; sub-hook under the existing Stop array). Rules (deny-listed → proposed diffs + amend: + changelog): workflow.md Rule 1 (work-item def + "memory is never the sole home" + exemptions; + Rule 2 exit-checklist line); session-ops.md (device auto-memory as a sixth tier); Docs/Changelog/changelog.md amend: entry.

## 5. ACs
AC-1 definition (work item + 3 exempt). AC-2 rule states memory non-registration + same-session. AC-3 advisory fires when a work artifact OR in-tree memory-bank changed and BACKLOG did not. AC-4 no FP when only feedback/pointer/reference files changed. AC-5 fail-open: never blocks, exits 0. AC-6 device-channel boundary per spike. AC-7 review backstop in /project:review + exit checklist. AC-8 governance compliance (proposed diffs not applied; amend:+changelog; .sln; expected-keys unchanged/updated).

## 6. Phases
Phase 0 Spec (full ceremony, Helder gate). Phase 1 Spike (gates Layer 2 scope). Phase 2 Pure discriminator + tests (Tester→Builder; Level A full TDD). Phase 3 Stop-hook integration (sequential; settings.json hotspot). Phase 4 Review/rule handoff (proposed diffs). Phase 5 Authorship gate (Helder applies; BACKLOG ✅).

## 7. Risks
Device channel unobservable → spike makes the boundary explicit; Layers 1+3 cover honestly. FP fatigue → unit-tested pure function + exempt tests; fire only on substantive artifacts. Breaking legit memory → 3 exempt categories + negative tests; auto-captured out of scope. settings.json conflict → hotspot single-writer; Phase 3 sequential. .sln missed → Phase 0 registers all; reconcile with sync hook. Rule applied without review → deny-list blocks; diffs handoff-only. Collision with line-149 → registration of new items vs claiming existing rows; out-of-scope. Hook cost → heavy git scan stays in the once-per-session Stop.

## 8. Verification
Pure lib Red→Green; hook synthetic git states; governance (rule files NOT modified by the agent; amend: + changelog; .sln; expected-keys); spike findings.md PASS/FAIL; design.md updated before Phase 3.
