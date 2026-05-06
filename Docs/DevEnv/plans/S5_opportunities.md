# S5 — Agent Patterns: Enhancement Opportunities
> Analyzed against current .claude state (see _current_state_summary.md)

## Summary

**Previously captured:** 10 opportunities (OPP-5-1 through OPP-5-10)
**Validated as-is:** 8 (OPP-5-1, OPP-5-2, OPP-5-3, OPP-5-5, OPP-5-6, OPP-5-7, OPP-5-8, OPP-5-10)
**Refined:** 2 (OPP-5-4, OPP-5-9 — scope or rationale tightened based on S5.2 and S5.1.1 deep content)
**New opportunities identified:** 6 (OPP-5-11 through OPP-5-16)

---

## Validated Opportunities

### OPP-5-1: Verifier subagent guidance for critical features ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.1 — Adversarial Agent Pattern (CIV architecture)
**Rationale:** The current workflow has Coordinator + Implementors but no Verifier role. For critical features (authentication, data persistence, payment), an independent Verifier subagent reading spec + artifacts (not the implementor's reasoning) would catch spec-level mismatches that the main agent's `dotnet build` check cannot. The SDD literature documents 3–5 round convergence and 90%+ issue elimination vs. self-review.
**Suggested content/change:** Add a "Verifier subagent (optional, recommended for critical features)" section to Rule 2. Define: Verifier receives spec file paths + task-log entry + git diff only — never the Implementor's conversation. Verifier outputs structured pass/fail verdict to task-log. Coordinator decides whether to replan or proceed. Specify which feature classes warrant it: auth, data persistence, migrations, DI registration.

---

### OPP-5-2: Subagent scope constraint — no unilateral redesign ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.1.1 — Persona/Role Confusion (Role Scope Expansion)
**Rationale:** The SDD research identifies a common failure: an Implementor given a scoped task begins reasoning about system-wide implications and redesigns things outside its assigned scope. This is not yet addressed in workflow.md. In MyVocaList's pattern, a subagent that "notices" an architectural issue and refactors DI registrations or changes domain models outside its briefed scope will silently break other parallel subagents' work.
**Suggested content/change:** Add to the subagent briefing protocol in Rule 2: "Subagents must follow the provided task exactly. If a concern outside the task scope is discovered (architectural, security, naming inconsistency), it must be written to the task-log as `blocked: spec gap` and the agent must stop. Subagents do not redesign, rename, or refactor outside their assigned file list."

---

### OPP-5-3: Dependency pre-check — shared artifacts enumeration before wave dispatch ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2.1 — Dependency Ordering Fragility
**Rationale:** The current workflow states "tasks marked [P] may be dispatched simultaneously" but gives no guidance on what makes a task safe to parallelize. The SDD research documents that hidden dependencies on shared artifacts (interfaces, DTOs, `MauiProgram.cs`, `AppDbContext`, migrations, `Directory.Build.props`) are the primary cause of parallel wave failures. For MyVocaList specifically, two subagents touching `MauiProgram.cs` or migration files in the same wave will produce unsolvable conflicts.
**Suggested content/change:** Add a "Pre-wave dependency check" step to Rule 2, before any wave dispatch: "Before assigning tasks to a wave, enumerate all shared artifacts that any task in the wave modifies: interfaces, DTOs, MauiProgram.cs, AppDbContext, migration files, Directory.Build.props, GlobalUsings.cs. If two tasks in the planned wave write to the same artifact, they are not parallel-safe — one must move to the next wave." Include a concrete MyVocaList-specific list of files that are always sequential-only (MauiProgram.cs, AppDbContext, migration files).

---

### OPP-5-4: Wave handoff — inject actual contracts, not references ♻️ Refined
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2 — Parallel Agent Execution (Wave-Based Execution, Best Practice #2); S5.2.1 — Dependency Ordering Fragility (Tertiary mitigation)
**Rationale:** The current briefing protocol says "give file paths, not content." This is correct for existing code, but creates a gap for Wave N+1 agents that depend on Wave N's newly produced artifacts. The S5.2 deep content is explicit: paste actual interface definitions, DTO field lists, and migration schemas directly into Wave N+1 spawn prompts — do not write "use the schema Agent A created in src/schema.cs." The distinction is between *existing* files (read independently) and *newly produced contracts from the current wave* (injected as inline content). The previous rationale conflated these two cases.
**Suggested content/change:** Add a clarification to the briefing protocol: "For Wave N+1 subagents that depend on Wave N outputs (new interfaces, new DTOs, new migration schemas): paste the actual produced artifact content into the spawn prompt — not a file path reference. Subagents read *existing* files independently; they receive *new contracts from upstream waves* as inline content in their spawn prompt. Example: Agent B's prompt contains the actual C# interface Agent A just wrote, not 'see the interface in src/Domain/...' This eliminates guesswork about what was built."

---

### OPP-5-5: Spec contracts section required before parallel implementation ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2.2 — Cross-Agent Spec Conflicts (Strategy 1: Shared Contracts)
**Rationale:** The current spec structure (requirements.md + design.md + tasks.md) does not require a shared contracts definition. When parallel agents implement interdependent services, field naming conflicts, DI lifetime mismatches, and error pattern mismatches are the documented failure modes. The MyVocaList codebase already has confirmed patterns (tuple returns, scoped repositories, no DisplayAlert) but these are in rules files, not in feature specs. Parallel agents don't read rules files deeply enough to avoid spec-level conflicts.
**Suggested content/change:** Add to Rule 1 (Spec-First): "For any feature involving 2+ subagents working on interdependent code, the design.md must include a `## Shared Contracts` section before parallel tasks are assigned. It must define: DTO field names and types, new interface method signatures, service lifetime for any new DI registrations, error return pattern (tuple or exception), and any new configuration keys. Subagents implement against these contracts; they do not infer them."

---

### OPP-5-6: Silent task completion — post-edit re-read requirement ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.3.1 — Silent Task Completion (Mitigation 2: Mandatory Post-Edit Re-read)
**Rationale:** The SDD research documents that the most common form of silent completion in Claude Code is a subagent claiming a file was edited when the edit did not persist. GitHub issues #46755 and #38200 document this pattern specifically. The current workflow exit checklist (build → commit → push) catches build errors but not cases where files were never actually written (the build passes on unchanged files).
**Suggested content/change:** Add to the subagent exit checklist in Rule 2: "After every Edit or Write tool call, re-read the changed lines to confirm the change is present before proceeding. Do not rely on the tool returning success — verify the change appears in the file."

---

### OPP-5-7: Silent task completion — structured task-log entry with evidence ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S5.3.1 — Silent Task Completion (Mitigation 4: Structured Completion Contract)
**Rationale:** The current task-log format records status (To Review, Build failure, etc.) and changed files, but does not require evidence. Research reduces false completion from ~60% to ~10% when agents must explicitly confirm each gate. The current format allows a subagent to write "To Review" after partial work.
**Suggested content/change:** Update the task-log format in Rule 5 to require a `### Verification evidence` block under Build notes when status is `To Review`: "Build: passed (0 errors, 0 warnings) | Tests: N passed, 0 failed | Commit SHA: <sha> | Files written and re-read: list". This makes it harder to claim completion without producing auditable evidence.

---

### OPP-5-8: Main agent must run build after every wave, not trust subagent self-report ✅ Validated
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.3.1 — Silent Task Completion (Mitigation: Main Agent Verification)
**Rationale:** The current workflow says the main agent runs shell steps. The SDD research is explicit: the main agent must run `dotnet build` and `dotnet test` independently after each wave completes, not trust the subagent's task-log entry. This is not currently stated as a rule — it is implicit. Making it explicit closes the gap where a subagent's `To Review` entry is accepted without independent verification.
**Suggested content/change:** Add a "Post-wave verification" step to Rule 2: "After every wave completes (all subagents have committed and pushed), the main agent must run `dotnet build` and `dotnet test` before dispatching the next wave, regardless of subagent task-log status. A `To Review` entry from a subagent is a signal to verify, not a verification itself. If build or tests fail, the affected subagent task is retroactively marked `Build failure` and replanned before the next wave."

---

### OPP-5-9: Subagent briefing must state role scope explicitly ♻️ Refined
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.1.1 — Persona/Role Confusion (Mitigation 2: Structured Output and Role Re-Declaration; Mitigation 3: Constraint Tracking)
**Rationale:** The current briefing protocol specifies "give file paths, not inline content." It does not specify that the subagent's role scope must be declared explicitly. The S5.1.1 deep content adds an important nuance: beyond just declaring scope, the briefing must also enumerate explicit "do NOT" constraints as a typed constraint list — not just a prose description. Research shows echoing (role drift) drops from 32–37% to ~9% with structured role re-declaration, but never reaches zero. The briefing template must include: assigned role, file scope, prohibited actions, and escalation path for out-of-scope discoveries.
**Suggested content/change:** Add a mandatory "Role scope declaration" block to the subagent briefing template in Rule 2:
```
Role: Implementor
File scope: [list — you own these files; do not modify others]
Prohibited: architectural decisions, DI lifetime changes, domain model changes outside scope
Out-of-scope discovery: write to task-log as `blocked: spec gap` with one-line question, then stop
```
This is a constraint list, not a paragraph — structured so violations are detectable.

---

### OPP-5-10: review.md — add spec vs. code consistency check ✅ Validated
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S5.2.2 — Cross-Agent Spec Conflicts (Verifier validates cross-service contracts)
**Rationale:** The current review.md checklist covers build quality, MAUI specifics, architecture, and DevExpress patterns. It does not include a check for spec-drift: cases where code was written that diverges from the feature's design.md. After parallel agent waves, code may be locally correct but misalign with the approved spec (field names, service lifetimes, error patterns). The review step is the natural gate for this check.
**Suggested content/change:** Add a "Spec compliance" section to review.md: "1. For each modified service or DTO, verify field names and types match the spec's Shared Contracts section. 2. Verify new DI registrations use the lifetime defined in the spec (scoped/singleton/transient). 3. Verify error return pattern matches code-principles.md (tuple returns, not exceptions, for expected failures). 4. If the spec has no Shared Contracts section and the feature had parallel implementation tasks, flag it: the spec is incomplete."

---

## New Opportunities

### OPP-5-11: Custom subagent definitions in `.claude/agents/` for reusable roles 🆕 New
**Target:** `.claude/agents/` (new directory and files)
**Action:** Create
**Source topic:** S5.3 — Subagent Delegation (Claude Code Subagent Mechanics, Custom Subagents)
**Gap in current setup:** The S5.3 research documents that Claude Code supports reusable specialist roles defined in `.claude/agents/` — named subagents with their own system prompts, tool access, and role constraints that the orchestrator invokes by name. MyVocaList currently has no `.claude/agents/` directory; all subagents are defined ad-hoc by the main agent's briefing at dispatch time. This means role constraints (no architectural decisions, read rules files first, exit checklist) must be re-stated in every briefing, and are subject to prompt drift across sessions.
**Suggested content/change:** Create `.claude/agents/` with at minimum two named subagents:
- `implementor.md` — system prompt enforcing file-scope restriction, rules file reading, exit checklist, and task-log update protocol
- `verifier.md` — system prompt for the optional Verifier role: receives spec + git diff only; outputs structured pass/fail; prohibited from suggesting code changes

The orchestrator then invokes these by name rather than re-briefing the role constraints each time. Role constraints are version-controlled and consistent across sessions.

---

### OPP-5-12: Living spec protocol — subagents must write spec decisions back before stopping 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2.2 — Cross-Agent Spec Conflicts (Strategy 2: Living Spec Updates); S5.2.1 — Dependency Ordering Fragility (Quaternary mitigation)
**Gap in current setup:** The current subagent return protocol (update task-log, commit, push, stop) captures status but not decisions. When a subagent makes an implementation-level choice not specified in the design.md (e.g., chooses a field name, decides a validation rule, selects an enum variant), that decision is invisible to Wave N+1 agents. Those agents either read the code to infer the decision (risking misinterpretation) or make an independent choice (risking conflict). The "living spec mechanism" from S5.2.2 requires that spec-level decisions made during implementation are written back to the spec before downstream agents run.
**Suggested content/change:** Add to Rule 2 (subagent return protocol), step 0 (before committing): "If during implementation you made any decision not specified in the design.md (field names, enum values, validation rules, method signatures, error codes), update the relevant spec file (`design.md` or its `## Shared Contracts` section) with the decision before committing. Tag the update with a `<!-- impl decision: <one-line reason> -->` comment. The main agent reads spec changes after each wave as part of post-wave verification."

---

### OPP-5-13: Sequential-only file registry — explicit list in workflow.md 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2 — Parallel Agent Execution (When Parallelism Is Valid); S5.2.1 — Dependency Ordering Fragility (Pattern 4: File Ownership with Strict Scope Isolation)
**Gap in current setup:** OPP-5-3 proposes a pre-wave enumeration step but relies on the main agent performing analysis ad-hoc each time. The S5.2 and S5.2.1 research documents that certain files in any MAUI project are structurally always-sequential: DI registration, global settings, migration files, and shared configuration. Rather than rediscovering this list every sprint, it should be codified as a standing rule. This prevents the pre-wave check from being skipped when the main agent is operating under context pressure.
**Suggested content/change:** Add a "Sequential-only files registry" block to Rule 2 (parallel execution cap section):
```
The following MyVocaList files are ALWAYS sequential — never assign to 2+ agents in the same wave:
- MyVocaList/MauiProgram.cs (DI registration)
- MyVocaList.Infra/AppDbContext.cs (entity configuration)
- MyVocaList.Infra/Migrations/** (migration files)
- Directory.Build.props (shared properties)
- Any project's GlobalUsings.cs
- .claude/rules/*.md (rules files — only main agent modifies)
```
Any task that modifies these files gets its own wave, regardless of how independent its other changes appear.

---

### OPP-5-14: Wave completion — compress and relay discovery briefs to next wave 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2 — Parallel Agent Execution (Wave-Based Execution, step 4: Discovery Relay / Handoff Blocks)
**Gap in current setup:** The current workflow reads: "After a subagent completes, its context is discarded." This is correct for preventing context bloat in the main agent, but it means that discoveries made by Wave N agents (an API is rate-limited, a field requires special encoding, a migration needed a backfill) are lost unless the subagent put them in the task-log. The S5.2 research specifies that after each wave, outputs should be compressed to 300–500 token "discovery briefs" and injected into the next wave's spawn prompts. This gives downstream agents knowledge of what upstream agents found, without full context inheritance.
**Suggested content/change:** Add to the post-wave verification step in Rule 2: "After verifying the build, the main agent reads each subagent's task-log entry and git diff. For any discovery noted (unexpected constraint, spec decision made, API behavior discovered), create a one-paragraph 'discovery brief' per agent and include it in Wave N+1 spawn prompts. Format: 'Agent [name] found: [one-paragraph summary of non-obvious findings]. This affects your task because: [connection].'"

---

### OPP-5-15: Context reset discipline — orchestrator must not accumulate implementation-role context 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.1.1 — Persona/Role Confusion (Mitigation 1: Session Isolation and Context Reset; Mitigation 5: Asymmetric Information Flow)
**Gap in current setup:** The current Rule 2 correctly states "the main agent handles shell-only steps." However, there is no explicit rule preventing the main agent from reading subagent file outputs in detail (e.g., reading a newly written service implementation to verify it). The S5.1.1 research on asymmetric information flow is clear: execution traces, debugging outputs, and intermediate failures must not flow backward to the orchestrator in full — only structured diagnostics. When the main agent reads full implementation files, it gradually accumulates implementation-role context and its planning quality degrades.
**Suggested content/change:** Add to Rule 2 (main agent / subagent boundary): "The main agent reads: spec files, task-log entries, git commit messages, and `dotnet build`/`dotnet test` output. The main agent does NOT read: subagent-written implementation files (services, repositories, view models, XAML). If the build passes, implementation correctness is assumed. If the build fails, the main agent reads only the compiler error output — not the full file. Full file reading is reserved for the subagent that owns that file. This prevents the main agent from accumulating implementation context that degrades planning quality."

---

### OPP-5-16: Retry cap — subagent must stop after 3 failed build attempts, not loop indefinitely 🆕 New
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.1 — Adversarial Agent Pattern (CIV architecture, VeriMAP retry cap); S5.3 — Subagent Delegation (failure modes)
**Gap in current setup:** The current exit checklist says "Build (0 errors required)" but gives no instruction for what a subagent should do if the build cannot be made to pass. The SDD literature (VeriMAP, CIV) uses a per-subtask retry cap of 3 attempts as a hard limit: after 3 failed attempts, the subagent reports `Build failure` and stops. Without this cap, a subagent can loop indefinitely on a broken compile state, consuming token budget and blocking the wave. The workflow.md mentions "Build failure" as a task status but never defines when to transition to it.
**Suggested content/change:** Add to the subagent exit checklist in Rule 2: "Build retry cap: if `dotnet build` fails, fix the error and retry. Maximum 3 build attempts. After 3 failed attempts, update task-log status to `Build failure | Reason: <compiler error summary>` and stop immediately. Do not attempt further fixes. The main agent replans the task. This cap prevents runaway subagents from consuming the wave's token budget on unresolvable errors."
