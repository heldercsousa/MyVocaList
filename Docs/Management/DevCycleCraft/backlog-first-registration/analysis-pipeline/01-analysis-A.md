# 01 — Analysis A (Wave 1, agent `aa47aa0cb9ee49ffe`) — verbatim

# Analysis — BACKLOG-first Registration Enforcement (`Docs/Management/BACKLOG.md` line 150)

## 1. Problem statement
The precise problem: Work items are getting *recorded* during a session, but in the wrong, team-invisible place — the agent's persistent memory — instead of, or before, `Docs/Management/BACKLOG.md`. The backlog item targets the gap between an *aspirational* rule and *actual* agent behaviour.

Why memory-only registrations happen: two parallel "remember this" systems with overlapping purposes but very different visibility — BACKLOG.md (committed, team-visible, declared source of truth for sequencing; two tables, `↳` nesting) vs agent memory (user-global auto-memory at `C:\Users\helde\.claude\projects\C--Users-helde-source-repos-MyVocaList\memory\MEMORY.md` + `project_*`/`feedback_*` satellites, device-scoped NOT committed; AND in-repo `.claude/memory-bank/MEMORY.md`). Auto-memory is injected into every session's context → immediate low-friction reward; a BACKLOG row requires choosing table, nesting, format, commit+push. Path of least resistance is memory — the asymmetry is the root cause.

Where the rule/behaviour gap is: workflow.md Rule 1 "Proactive BACKLOG triage — Untracked work" says "Any work identified during a session that is not already in BACKLOG.md must get a brief entry before proceeding" with trigger questions — prose to the agent's discretion; no machine checks it. An agent under context pressure writes to memory and never reaches the BACKLOG row; nothing fails; the session ends green.

Why memory is a poor substitute: (1) not team-visible (user-global memory dir on Helder's machine only, never pushed); (2) not the declared source of truth (Rule 1 step 0 / Rule 7 route selection through BACKLOG.md — a memory-only item is invisible to scheduling); (3) different lifecycle/ownership per session-ops.md tiered memory governance.

## 2. Current state — and why nothing enforces it
Rule text (all advisory): workflow.md Rule 1, Rule 3 session-end ritual, Rule 7 reads BACKLOG, bug-tracking.md nesting, session-ops.md tiered governance.

Hooks today (`.claude/settings.json` + `.claude/settings.local.json`): Stop hook (agent) STEP 5 BACKLOG.MD FRESHNESS CHECK runs `git diff --name-only HEAD` + `git ls-files --others`; if `*task-log*` changed but `Docs/Management/BACKLOG.md` did NOT, prints a warning then "Continue regardless — this is a reminder, not a blocker." Closest existing thing; explicitly non-blocking; knows nothing about memory. UserPromptSubmit injects a "WORKFLOW GATE" reminder incl. "update Docs/BACKLOG.md status." Stop command hook checks `git status --porcelain` for uncommitted only. PostToolUse: (a) appends edited paths to `.claude/changed-files.txt`; (b) TDD reminder on Services; (c) lease heartbeat.py. PreToolUse(Write|Edit) warns when the main agent edits .cs/.xaml directly. SessionStart verifies hook health (expected keys).

Why NOT enforced: (1) the only BACKLOG check (STEP 5) is explicitly advisory and ignores memory; (2) hooks cannot see memory writes — user-global memory lives outside CLAUDE_PROJECT_DIR and outside git, so git-diff/changed-files.txt are blind; (3) no hook intercepts the memory-write tool (auto-memory is written by the harness, not a project Write/Edit); (4) rules-as-prose are unenforceable and workflow.md is itself Edit/Write-denied.

## 3. Enforcement options
A — Stop-hook gate: upgrade STEP 5 into a blocker comparing BACKLOG vs memory writes. Detects "memory changed, BACKLOG did not" at session granularity. High false-positive (legit feedback/pointer notes trip it); false-negative (item registered in a prior session). Cannot see the out-of-tree memory dir without special path handling. Low–medium cost. Failure modes: false-positive fatigue → demoted to advisory (as STEP 5 was); clashes with the auto-commit Stop agent.

B — PostToolUse interception of memory writes (record-and-flag): append a marker to a session buffer on memory write (parallels changed-files.txt). Pivotal unknown: does the harness expose memory writes as a matchable tool? Needs a spike (Session Continuity AC-5 style). Cheap if interceptable; blocked entirely if not.

C — Review-gate/checklist: add BACKLOG-registration to the subagent exit checklist (Rule 2) + /project:review. Semantic judgement possible — but discretion again, same failure class.

D — Convention-only rule strengthening: define "work item," state "memory is not a registration surface." No behavioural teeth alone; the normative half of a hybrid.

E — Hybrid (recommended): (D) sharpen Rule 1 + define work-item vs note + declare memory a non-registration surface; (B) PostToolUse marker IF the spike confirms interceptable; (A) upgrade STEP 5 to a soft blocking-with-override gate; (C) add a BACKLOG line to the exit checklist + /project:review as a semantic backstop. Full ceremony, three spec files.

## 4. Constraints
1. Amending These Rules (amend: prefix + changelog old/new/effective). 2. Rules-file write-protection (deny Edit/Write on .claude/rules/*.md and CLAUDE.md → proposed diff to Helder). 3. Authorship/human-review gate. 4. Hook health verification — update SessionStart expected-keys if a new hook is added. 5. .sln registration BLOCKING gate for new Docs/.claude files. 6. Constitutional constraints + fail-open hook house style (blocking must be deliberate/narrow/overridable). 7. Legitimate memory use must not break (feedback, active-feature pointers, continuation hints). 8. Token/cost discipline — PostToolUse cheap; heavy git-diff in the once-per-session Stop.

## 5. Open questions
1. Is a memory write interceptable at all? (spike before any hook option). 2. What counts as a "work item" vs a transient note? 3. How does a hook correlate a memory write with the absence of a BACKLOG edit (session buffer + clear point + session id)? 4. Which memory is in scope — out-of-tree user-global vs in-repo memory-bank? 5. Block vs warn + override semantics (unattended/background sessions can't be overridden by a human). 6. Granularity mismatch with the lease system. 7. Interaction with existing advisory layers (replace vs stack STEP 5).
