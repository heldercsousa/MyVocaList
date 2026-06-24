# 05 — Synthesis: comparison + final plan (Wave 3, agent `a3f03a2e7d41cc8c4`) — verbatim

> The synthesis agent received Plan 1 and Plan 2, resolved their factual disputes against live
> ground truth, and wrote the final consolidated plan. The review-corrected authoritative version is
> `09-final-consolidated-plan.md`; this file preserves the synthesis agent's original wording.

## 1. COMPARISON (ground-truth resolution of Plan 1 vs Plan 2)
- **D1 Device dir empty vs 16 files** → ground truth **16 files**; Plan 2 right. Strengthens the content-classifier approach, weakens Plan 1's mtime emphasis.
- **D2 In-repo memory-bank empty vs used / real second channel** → `memory-bank/MEMORY.md` exists but is a near-dead stub; the live problem is entirely the device dir. Plan 2's device-only targeting is more correct; Plan 1's "watch in-tree memory-bank" watches the wrong surface.
- **D3 `.sln` manual GUID vs auto** → ground truth: `sync-docs-to-sln.ps1` longest-prefix-wins; `DevCycleCraft\` → `{0C4BA720-…}`; a new folder auto-registers into the PARENT DevCycleCraft solution folder (flat, no dedicated subfolder GUID). Plan 2 right that no manual GUID is needed; caveat: files land flat (add a verify step + an optional dedicated-subfolder Helder decision).
- **D4 Spike-fail mtime fallback keep vs drop** → drop it (brittle out-of-tree path, non-discriminating → reintroduces FP fatigue). Plan 2 right.
- **D5 Stop layer A framing** → converge on advisory folded into STEP 5; Plan 2's content-classifier call is the higher-signal trigger; keep STEP 5's task-log↔BACKLOG correlation + ADD a device-memory classifier call; do NOT broaden to the empty in-tree memory-bank.
- **D6 Exempt count 3 vs 4** → 4 (add harness-AUTOMATIC captures). Plan 2 right.
- **D7 session-ops.md routing** → BOTH plans WRONG: `session-ops.md` is in `.claude/library/`, NOT under the `rules/*.md` deny glob → directly editable. Only `workflow.md` + `CLAUDE.md` are hard-blocked. Still route session-ops.md through Authorship review (governance/context file).
- **D8 Discriminator shape** → complementary: Plan 2's `classify_memory_change(filename, diff_text)` primitive + Plan 1's `should_remind(...)` orchestrator predicate. Both pure, both unit-tested.
- **D9 CLAUDE.md touch** → recommend none (600-line budget); Plan 2's explicit reservation is better.
- **D10 Helder decision section** → Plan 2's explicit list is superior; required.
- **Net:** Plan 2 the more accurate base on every contested fact; Plan 1 contributes the Stop-STEP-5 extension framing + the orchestrator-predicate + slightly more `.sln` rigor; both miss D7.

## 2. FINAL PLAN (as written by the synthesis agent)
Chosen approach: Hybrid **D + C ship unconditionally**; **A folded into Stop STEP 5** as a non-blocking, classifier-driven advisory; **B (memory-write interception) built only if the spike** proves a device-memory write emits a hook-observable event. No hard-blocking. No mtime baseline.

Files (A directly editable): spec folder `…/backlog-first-registration/{requirements,design,tasks,plan,findings,task-log}.md` + `proposed-diffs.md`; `.claude/scripts/backlog/backlog_lib.py` (`classify_memory_change` + `should_remind`); `…/test_backlog_lib.py`; `…/orphan_check.py` (fail-open); `.claude/settings.json` (extend Stop STEP 5; spike-pass adds a PostToolUse buffer); `.claude/library/session-ops.md` (6th tier, directly editable, Authorship-reviewed); `MyVocaList.sln` (auto via sync hook, verify).

Files (B write-protected → proposed diffs): `.claude/rules/workflow.md` (amend:+changelog), `CLAUDE.md` (no change recommended), `Docs/Changelog/changelog.md` (triple).

Spike: 60 min device-memory observability + path determinism. Discriminator: work-item def + 4 exempt categories; content heuristic. AC-1..AC-11. Phases 0–5 DRY-onion. Risk table. 5 Helder decisions (posture; CLAUDE.md; spike-fail fallback; dedicated .sln subfolder; workflow.md wording).

Synthesis correction of record: `session-ops.md` is in `library/`, NOT deny-listed — only `workflow.md` and `CLAUDE.md` require the `amend:` proposed-diff path.

> Ground-truth verifications performed by the synthesis agent: `.claude/settings.json` (Stop STEP 5 + expected-keys + fail-open style), `lease_lib.py` (pure-function precedent), `sync-docs-to-sln.ps1` (DevCycleCraft prefix → `{0C4BA720-…}`, auto on Write), the deny list (`rules/*.md` + CLAUDE.md only — session-ops.md in `library/` NOT denied), `session-ops.md` (5 existing tiers, no device tier), `BACKLOG.md` (line-150 target vs line-149 deferred sibling), and the device memory dir (16 files, not empty).
