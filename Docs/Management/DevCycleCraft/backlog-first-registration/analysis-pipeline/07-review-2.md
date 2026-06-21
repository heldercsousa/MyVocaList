# 07 — Review 2 (Wave 4, agent `aedbcd1090153f1ea`) — verbatim — VERDICT: Approve with changes

**Correctness:** verified true — deny-list scope (settings.json 11-16; session-ops.md in library/ NOT denied) ✅; SessionStart expected-keys (line 93) unchanged when folding into existing PostToolUse/Stop keys ✅; Stop STEP 5 advisory (line 127) ✅; device memory = 16 .md files ✅ (real out-of-tree channel, not in git, not in changed-files.txt); sync-docs-to-sln.ps1 on PostToolUse Write (71-77), DevCycleCraft→{0C4BA720-…} (line 51) ✅; lease_lib.py pure ✅.

**CORRECTION 1:** the sync hook fires on Write only, not Edit, and self-skips already-present paths — the auto-writer is best-effort, not a guarantee; elevate the per-file .sln verification to an explicit gate.

**CORRECTION 2:** `MEMORY.md` exempt-category-4 claim is only half-true — MEMORY.md is AGENT-CURATED (hand-written "Active Feature" pointers), not purely harness-automatic; the classifier must handle a new-work line added to MEMORY.md itself — classify by line content, not by blanket-exempting the file. Otherwise the feature has the exact blind spot it exists to close.

**Governance:** Amending-These-Rules honored (workflow.md proposed-diff + amend: + changelog; matches the Session-Continuity precedent at BACKLOG 147); rules-dir protection enforced mechanically; Authorship gate applied to session-ops.md; 600-line budget respected (no CLAUDE.md change); fail-open ✅; no new top-level key ✅. Over-reach to tighten: Phase 5 bundles a Helder-gated workflow.md diff with a directly-applied review.md edit — split/sequence so the two halves of the same change don't diverge in git history.

**Core bet:** advisory-not-blocking is correct (fail-open verified; background sessions; STEP 5 demotion) — but the plan BUILDS advisory as committed while Decision 1 (posture) is open, and BACKLOG line 150 says "block session end"; gate Phase 4 on Decision 1 or strengthen the recommendation and ratify at Phase 0. Content classifier is the right primitive, but the heuristic is fragile: a `project_*` pointer reading "NEXT: implement the X service" has BOTH a resume marker AND a new-work verb; the plan states no precedence on conflict → AC-11 is asserted not proven; add an explicit precedence rule + enumerate the adversarial cases in the test matrix. Spike well-formed; gating only Option B on it is correct.

**Completeness/testability:** pure functions properly TDD'd ✅; `orphan_check.py` (the I/O wrapper) essentially untested — the path-mangling step is the most brittle part yet falls into "manual E2E"; add a deterministic test against an injected fixture dir + a real unit test for AC-6 fail-open. Missing failure path: BACKLOG updated this session for a DIFFERENT item than the orphan suppresses the reminder (inherited STEP 5 coarse correlation) — document as a known limitation.

**Practicality/scope:** disjoint from line-149 ✅; precedent mismatch — lease tests live at `lease/tests/test_lease_lib.py`, so backlog tests should be `backlog/tests/test_backlog_lib.py` (not flat); proposed-diffs filename convention should match whatever Session-Continuity used. bug-tracking.md integration correct.

**Prioritized required changes:**
1. resolve blocking-vs-advisory up front — gate Phase 4 on Decision 1 or ratify posture at Phase 0 before any hook code.
2. add deterministic tests for orphan_check.py (path-resolution/enumeration fixture + fail-open).
3. specify classifier precedence + adversarial test cases.
4. fix the MEMORY.md exemption (line-content, not whole-file).
5. elevate per-file .sln verification (sync hook is Write-only + self-skips).

**Nice-to-have:** align test path to the lease precedent; split Phase 5 review.md edit from the workflow.md proposed-diff; document the STEP 5 coarse-correlation limitation.

**Highest-impact concern:** the plan commits to building advisory while the posture decision is open and the backlog row literally says "block session end."
