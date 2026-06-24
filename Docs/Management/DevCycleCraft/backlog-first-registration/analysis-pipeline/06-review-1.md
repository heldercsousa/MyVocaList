# 06 — Review 1 (Wave 4, agent `aa352bb79bbf7368b`) — verbatim — VERDICT: Approve with changes

**Single most important issue:** the plan's central rationale rests on a FALSE premise about the
in-repo `memory-bank/` AND on an unflagged reversal of the BACKLOG "block session end" intent.
- (1) `.claude/memory-bank/MEMORY.md` is NOT an empty stub — it is 2410 bytes and git-tracked (`git ls-files` returns it). "Already covered by changed-files.txt" is also shaky: changed-files.txt only captures in-session Edit/Write paths; a harness-injected MEMORY.md never appears there either. Re-derive the device-only scope from true facts.
- (2) BACKLOG line 150 literally says "blocks session end unless BACKLOG was updated." The plan reverses to advisory but buries it as reserved-decision #1; per the SDD invariant this reversal must be elevated to a top-level pre-spec Helder ratification.

**Correctness table:** deny list = CLAUDE.md + .claude/rules/*.md (session-ops.md NOT denied) ✅; review.md editable ✅; expected-keys unchanged if folding into existing keys ✅; device memory = 16 files ✅; spec .md auto-register under {0C4BA720-…} ✅; "memory-bank empty stub" ❌ WRONG; "changed-files.txt already covers memory-bank" ⚠ misleading; "extend STEP 5 by editing the agent prompt" ⚠ imprecise/feasibility risk.

**Completeness:** (1) `.claude/scripts/` files are NOT auto-registered — sync hook only acts on `Docs\` (line 28) and only on Write (line 71); the new `.claude/scripts/backlog/*.py` need a MANUAL .sln task; AC-9 silently omits them. (2) Authorship gate under-specified for the workflow.md proposed-diff prose. (3) changelog triple risk: TaskCompleted/Stop hooks auto-touch changelog.md (lines 115,138) — keep the triple in proposed-diffs.md until Helder applies, never agent-written to changelog.md. (4) No AC for the device-path resolution failure mode beyond "reviewer-driven" — AC-5 promises a reminder that may have no signal to fire on.

**Consistency:** AC-5 vs the spike outcome is internally unstable (its "a memory file classifies candidate" antecedent needs the very signal the spike gates) — branch AC-5 on spike outcome. session-ops.md is the operational reference for the governance model being amended — Helder review is substantive, not courtesy. No contradiction with Amending-These-Rules, line-149, or the 600-line budget.

**Feasibility:** Stop is an AGENT-type hook (line 124) — weaving "run orphan_check.py and surface output" into the prompt is non-deterministic; the robust pattern (lease heartbeat is a separate COMMAND-type Stop entry, lines 143-148) is to add orphan_check.py as a command-type entry under the existing Stop key (deterministic, fail-open, expected-keys unchanged). Device-path mangling has ZERO repo precedent (all lease scripts resolve only CLAUDE_PROJECT_DIR) — the spike's PRIMARY deliverable should be path determinism, Option-B observability secondary. Pure classifier + tests fully feasible (lease_lib.py precedent). Advisory posture justified on the merits; the only defect is procedural (reverses BACKLOG text without flagging).

**Risks/gaps:** Option B over-engineering (likely the spike fails → D+C+classifier-for-reviewer is the probable shipped scope); `should_remind` suppression depends on `git diff HEAD` which won't see already-committed in-session BACKLOG edits (auto-commit hook interaction) → specify the window; mis-scope vs line-149 is clean.

**Prioritized required changes:**
1. [BLOCKER] correct the memory-bank false premise.
2. [BLOCKER] elevate the advisory-reverses-"block session end" to pre-spec Helder ratification.
3. [HIGH] switch Stop integration to a command-type entry.
4. [HIGH] add manual .sln registration for `.claude/scripts/backlog/*.py` + broaden AC-9.
5. [HIGH] spike leads with device-path determinism.
6. [MED] reconcile AC-5 with the spike branch.
7. [MED] specify the should_remind suppression window.
8. [MED] Authorship-gate AC for workflow.md prose + lock the changelog triple in proposed-diffs.md.
