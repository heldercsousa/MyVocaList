# S5 — Agent Patterns: Enhancement Opportunities
> Analyzed against current .claude state (see _current_state_summary.md)

---

### OPP-5-1: Verifier subagent guidance for critical features
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.1 — Adversarial Agent Pattern (CIV architecture)
**Rationale:** The current workflow has Coordinator + Implementors but no Verifier role. For critical features (authentication, data persistence, payment), an independent Verifier subagent reading spec + artifacts (not the implementor's reasoning) would catch spec-level mismatches that the main agent's `dotnet build` check cannot. The SDD literature documents 3–5 round convergence and 90%+ issue elimination vs. self-review.
**Suggested content/change:** Add a "Verifier subagent (optional, recommended for critical features)" section to Rule 2. Define: Verifier receives spec file paths + task-log entry + git diff only — never the Implementor's conversation. Verifier outputs structured pass/fail verdict to task-log. Coordinator decides whether to replan or proceed. Specify which feature classes warrant it: auth, data persistence, migrations, DI registration.

---

### OPP-5-2: Subagent scope constraint — no unilateral redesign
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.1.1 — Persona/Role Confusion (Role Scope Expansion)
**Rationale:** The SDD research identifies a common failure: an Implementor given a scoped task begins reasoning about system-wide implications and redesigns things outside its assigned scope. This is not yet addressed in workflow.md. In MyVocaList's pattern, a subagent that "notices" an architectural issue and refactors DI registrations or changes domain models outside its briefed scope will silently break other parallel subagents' work.
**Suggested content/change:** Add to the subagent briefing protocol in Rule 2: "Subagents must follow the provided task exactly. If a concern outside the task scope is discovered (architectural, security, naming inconsistency), it must be written to the task-log as `blocked: spec gap` and the agent must stop. Subagents do not redesign, rename, or refactor outside their assigned file list."

---

### OPP-5-3: Dependency pre-check — shared artifacts enumeration before wave dispatch
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2.1 — Dependency Ordering Fragility
**Rationale:** The current workflow states "tasks marked [P] may be dispatched simultaneously" but gives no guidance on what makes a task safe to parallelize. The SDD research documents that hidden dependencies on shared artifacts (interfaces, DTOs, `MauiProgram.cs`, `AppDbContext`, migrations, `Directory.Build.props`) are the primary cause of parallel wave failures. For MyVocaList specifically, two subagents touching `MauiProgram.cs` or migration files in the same wave will produce unsolvable conflicts.
**Suggested content/change:** Add a "Pre-wave dependency check" step to Rule 2, before any wave dispatch: "Before assigning tasks to a wave, enumerate all shared artifacts that any task in the wave modifies: interfaces, DTOs, MauiProgram.cs, AppDbContext, migration files, Directory.Build.props, GlobalUsings.cs. If two tasks in the planned wave write to the same artifact, they are not parallel-safe — one must move to the next wave." Include a concrete MyVocaList-specific list of files that are always sequential-only (MauiProgram.cs, AppDbContext, migration files).

---

### OPP-5-4: Wave handoff — inject actual contracts, not references
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2 — Parallel Agent Execution (Wave-Based Execution, Best Practice #2)
**Rationale:** The current briefing protocol says "give file paths, not content." This is correct for avoiding token multiplication of existing code, but it creates a gap for Wave N+1 agents: they receive a file path reference to Wave N's output, which may not yet match what was actually built. The SDD best practice is to inject actual contracts (interface signatures, DTO field lists, migration table names) from Wave N directly into Wave N+1 spawn prompts — not references.
**Suggested content/change:** Add a clarification to the briefing protocol: "For Wave N+1 subagents that depend on Wave N outputs (new interfaces, new DTOs, new migration schemas), paste the actual produced artifact content into the spawn prompt — not a file path reference. The subagent reads existing files independently; it receives new contracts from upstream waves as inline content. This eliminates guesswork about what was built."

---

### OPP-5-5: Spec contracts section required before parallel implementation
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.2.2 — Cross-Agent Spec Conflicts (Strategy 1: Shared Contracts)
**Rationale:** The current spec structure (requirements.md + design.md + tasks.md) does not require a shared contracts definition. When parallel agents implement interdependent services, field naming conflicts, DI lifetime mismatches, and error pattern mismatches are the documented failure modes. The MyVocaList codebase already has confirmed patterns (tuple returns, scoped repositories, no DisplayAlert) but these are in rules files, not in feature specs. Parallel agents don't read rules files deeply enough to avoid spec-level conflicts.
**Suggested content/change:** Add to Rule 1 (Spec-First): "For any feature involving 2+ subagents working on interdependent code, the design.md must include a `## Shared Contracts` section before parallel tasks are assigned. It must define: DTO field names and types, new interface method signatures, service lifetime for any new DI registrations, error return pattern (tuple or exception), and any new configuration keys. Subagents implement against these contracts; they do not infer them."

---

### OPP-5-6: Silent task completion — post-edit re-read requirement
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.3.1 — Silent Task Completion (Mitigation 2: Mandatory Post-Edit Re-read)
**Rationale:** The SDD research documents that the most common form of silent completion in Claude Code is a subagent claiming a file was edited when the edit did not persist. GitHub issues #46755 and #38200 document this pattern specifically. The current workflow exit checklist (build → commit → push) catches build errors but not cases where files were never actually written (the build passes on unchanged files).
**Suggested content/change:** Add to the subagent exit checklist in Rule 2: "After every Edit or Write tool call, re-read the changed lines to confirm the change is present before proceeding. Do not rely on the tool returning success — verify the change appears in the file."

---

### OPP-5-7: Silent task completion — structured task-log entry with evidence
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S5.3.1 — Silent Task Completion (Mitigation 4: Structured Completion Contract)
**Rationale:** The current task-log format records status (To Review, Build failure, etc.) and changed files, but does not require evidence. Research reduces false completion from ~60% to ~10% when agents must explicitly confirm each gate. The current format allows a subagent to write "To Review" after partial work.
**Suggested content/change:** Update the task-log format in Rule 5 to require a `### Verification evidence` block under Build notes when status is `To Review`: "Build: passed (0 errors, 0 warnings) | Tests: N passed, 0 failed | Commit SHA: <sha> | Files written and re-read: list". This makes it harder to claim completion without producing auditable evidence.

---

### OPP-5-8: Main agent must run build after every wave, not trust subagent self-report
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.3.1 — Silent Task Completion (Mitigation: Main Agent Verification)
**Rationale:** The current workflow says the main agent runs shell steps. The SDD research is explicit: the main agent must run `dotnet build` and `dotnet test` independently after each wave completes, not trust the subagent's task-log entry. This is not currently stated as a rule — it is implicit. Making it explicit closes the gap where a subagent's `To Review` entry is accepted without independent verification.
**Suggested content/change:** Add a "Post-wave verification" step to Rule 2: "After every wave completes (all subagents have committed and pushed), the main agent must run `dotnet build` and `dotnet test` before dispatching the next wave, regardless of subagent task-log status. A `To Review` entry from a subagent is a signal to verify, not a verification itself. If build or tests fail, the affected subagent task is retroactively marked `Build failure` and replanned before the next wave."

---

### OPP-5-9: Subagent briefing must state role scope explicitly
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S5.1.1 — Persona/Role Confusion (Mitigation 2: Structured Output and Role Re-Declaration)
**Rationale:** The current briefing protocol specifies "give file paths, not inline content." It does not specify that the subagent's role scope must be declared explicitly in the briefing. The SDD research shows that role scope expansion (Implementor starts reasoning about architecture) is reduced when the briefing explicitly states the boundary: "You are implementing X. You do not redesign Y. Decisions outside your file list go to task-log as blocked: spec gap."
**Suggested content/change:** Add a mandatory "Role scope declaration" line to the subagent briefing template in Rule 2: "Every subagent briefing must include: 'Your role: Implementor. Your file scope: [list]. You do not modify files outside this list. You do not make architectural decisions. If you discover something that requires a decision outside your scope, write it to the task-log as `blocked: spec gap` with a one-line question and stop.'"

---

### OPP-5-10: review.md — add spec vs. code consistency check
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S5.2.2 — Cross-Agent Spec Conflicts (Verifier validates cross-service contracts)
**Rationale:** The current review.md checklist covers build quality, MAUI specifics, architecture, and DevExpress patterns. It does not include a check for spec-drift: cases where code was written that diverges from the feature's design.md. After parallel agent waves, code may be locally correct but misalign with the approved spec (field names, service lifetimes, error patterns). The review step is the natural gate for this check.
**Suggested content/change:** Add a "Spec compliance" section to review.md: "1. For each modified service or DTO, verify field names and types match the spec's Shared Contracts section. 2. Verify new DI registrations use the lifetime defined in the spec (scoped/singleton/transient). 3. Verify error return pattern matches code-principles.md (tuple returns, not exceptions, for expected failures). 4. If the spec has no Shared Contracts section and the feature had parallel implementation tasks, flag it: the spec is incomplete."
