# 02 — Analysis B (Wave 1, agent `ac8f53e17f1fc88dd`) — verbatim

# Analysis: BACKLOG-first Registration Enforcement

## Key discovery: there are TWO "memory" channels, only one is the problem
1. Device-scoped auto-memory — `C:\Users\helde\.claude\projects\C--Users-helde-source-repos-MyVocaList\memory\MEMORY.md` + `project_*`/`feedback_*`. Lives entirely OUTSIDE the git repo under the user's home `.claude`. Not git-tracked, not in .sln, not team-visible, not even listed in the tiered-memory governance table. This is the channel the item means.
2. In-repo memory-bank — `.claude/memory-bank/MEMORY.md`. IS git-tracked and team-visible.

Any enforcement must target channel (1), which is physically outside the repo and outside git's view — eliminating several otherwise-obvious mechanisms.

## 1. Problem statement
Work items end up registered ONLY in device-scoped auto-memory and never make it into BACKLOG.md (single source of truth). Device memory is not team-visible and not in the repo, so that work becomes invisible to anyone not on this exact machine — and invisible to the BACKLOG-driven workflow itself (Rule 1 step 0, Rule 7, the Stop freshness check all read BACKLOG.md, never device memory). Agents drift to memory because it's frictionless/automatic (captures feedback_*/project_* as a side effect; the current store already holds items with no BACKLOG row), while BACKLOG.md is a deliberate structured act. Rule 1 is advisory prose, no hook backing (the Hook-enforced table lists Stop/PostCompact/PostToolUse-Services/SessionStart, NOT BACKLOG triage), self-assessment fails under pressure, never mentions memory. Genuine risk: a memory-only item can't be picked up by Rule 1 step 0, is lost if the device is replaced / store cleared, never seen by Helder, invisible to a second concurrent session (Session Continuity leasing). Device memory is a single-device cache masquerading as a registry.

## 2. Current state
Stop STEP 5 correlates task-log vs BACKLOG via git diff + ls-files, ignores device memory (outside repo → never in git diff), non-blocking. PostToolUse(Edit|Write) appends changed-files.txt + TDD; only project-internal file_path; device-memory writes don't flow through observed Write/Edit. SessionStart hook-health only. PostCompact non-negotiables. TaskCreated/TaskCompleted maintain task-log/changelog. .sln HARD GATE only in-repo; device memory is an unguarded side door. Tiered governance (session-ops.md) has 5 tiers; device auto-memory is NOT one. Net cause: BACKLOG-touching hooks correlate git-visible files; the offending writes go to a git-invisible repo-external store; the nearby check is non-blocking and watches task-logs not memory.

## 3. Enforcement options
A — Stop-hook gate: snapshot device memory mtimes/hashes at SessionStart to `.claude/.session-memory-baseline.json`, diff at Stop. High false positives (feedback/pointer/status — alarm fatigue like STEP 5), false negatives; medium effort; resolve `~/.claude/projects/<mangled-key>/memory/` outside CLAUDE_PROJECT_DIR (brittle OS/profile); blocking strands the session; fail-open weakens to advisory.

B — PostToolUse interception: fatal flaw — device memory not written via the project Write/Edit the matchers observe; likely NO observable event; low to write, ~zero efficacy; spike needed; governance-theater risk.

C — Review-gate/checklist + optional reviewer subagent reads device memory + BACKLOG and reports orphans; high accuracy if executed (judgment discriminates); best FP discrimination; self-enforced weakness.

D — Pure rule strengthening: "Memory is never the sole home for a work item; write a work item to memory → MUST add a BACKLOG row same session." Add device memory as a sixth governance tier. Triggers Amending These Rules; proposed diff for Helder; lowest risk, closes the definition gap, unenforced.

E — Hybrid (recommended): (1) rule strengthening (D) + sixth tier; (2) Stop-hook advisory NON-blocking mirroring STEP 5, lists changed memory files asking "did any describe untracked work?"; (3) review-gate (C) for the reasoning correlation the blunt hook can't do. Rule defines, hook reminds, reviewer judges.

## 4. Constraints
Amending These Rules (what's wrong, backward-compat, amend: prefix, changelog old/new/effective); rules dir write-protected (deny Edit on .claude/rules/*.md + CLAUDE.md) → proposed diffs; hook/script/library NOT denied so tooling implementable directly; Authorship/human-review hard gate before Done; hook health expected-keys must be updated if a new hook key added; fail-open (`2>/dev/null || true`, lease sys.exit(0)); .sln HARD GATE for spec files; English-only, Dev Cycle Craft table; legitimacy of memory for non-work-item facts MUST NOT be flagged (feedback_* learnings, project_* "NEXT:" pointers, reference facts email/date/arch caches) — need a discriminator; don't duplicate/collide with the existing STEP 5 reminder.

## 5. Open questions
1. Definition of "work item" (enumerate in vs exempt the legit categories). 2. How can a hook correlate a memory write to a missing BACKLOG row — blunt diff vs content heuristic vs reasoning subagent (put heuristic in a unit-testable pure function, lease_lib.py precedent). 3. Path resolution to the device memory dir (mangled key, OS/profile home) — may confirm advisory + fail-open only. 4. Is a memory write observable by ANY hook? SPIKE needed. 5. Scope per-session vs per-write. 6. Blocking vs advisory + escape hatch for false positives. 7. Relationship to in-repo `.claude/memory-bank/MEMORY.md`. 8. Interaction with auto-memory's AUTOMATIC capture (don't make agents responsible for BACKLOG rows for captures they didn't author).
