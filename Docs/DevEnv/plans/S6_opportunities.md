# S6 — Governance & Enforcement: Enhancement Opportunities
> Analyzed against current .claude state (see _current_state_summary.md)

---

### OPP-6-01: Distinguish constitutional constraints from guidelines in CLAUDE.md
**Target:** CLAUDE.md
**Action:** Update
**Source topic:** S6.1 — Constitutional Constraints
**Rationale:** CLAUDE.md currently mixes mechanically-enforced rules with advisory guidance without distinguishing between them. The SDD constitutional model requires that every rule either has a named enforcement mechanism or is explicitly labeled as a guideline. AI agents comply more reliably when the enforcement boundary is explicit: rules without a stated mechanism degrade to suggestions under context pressure.
**Suggested content/change:** Add a new section header "Constitutional Constraints (Mechanically Enforced)" above the Non-Negotiables block and annotate each rule with its enforcement mechanism in parentheses: e.g., "Never use `DisplayAlert`" → "(enforced: review.md checklist + hook)". Separate from these a "Guidelines (Advisory)" section covering stylistic or lower-stakes preferences. This costs nothing in token budget but signals clearly to agents which rules are non-negotiable.

---

### OPP-6-02: Add rationale to every Non-Negotiable rule in CLAUDE.md
**Target:** CLAUDE.md
**Action:** Update
**Source topic:** S6.1.1 — Constitutional Rigidity and Staleness
**Rationale:** Rules without rationale are ignored under time pressure and cannot be correctly amended because no one knows why they exist. S6.1.1 documents that rationale capture is the primary countermeasure against stale rules. Currently CLAUDE.md states rules but not the problem each rule solves or the conditions that make it relevant.
**Suggested content/change:** After each Non-Negotiable, add a one-line rationale in italics or as a `> ` blockquote. Example: "Never use `DisplayAlert`, `DisplayActionSheet`, `DisplayPromptAsync`. *Reason: these dialogs bypass the app's theme, violate MD3 interaction patterns, and on Android are not dismissible via back gesture.*" Apply the same treatment to the architecture constraints in CLAUDE.md and the key rules in code-principles.md. The format from S6.1.1: **Constraint:** [rule] / **Rationale:** [problem it solves] / **Amendment trigger:** [what circumstance would warrant re-evaluation].

---

### OPP-6-03: Add an amendment governance process for CLAUDE.md and rules files
**Target:** CLAUDE.md
**Action:** Add
**Source topic:** S6.1.2 — Amendment Governance
**Rationale:** CLAUDE.md states "Continuous Enhancement — after every task, always ask what was learned that should improve CLAUDE.md." This is correct but incomplete: it describes when to consider amendments, not how to make them safely. Without a process, rules either accumulate silently (stale rules never removed) or drift informally (edits made without recording rationale). The SDD amendment model requires documented rationale, a record of what changed and why, and a note on backward compatibility.
**Suggested content/change:** Add a short "Amending These Rules" section to CLAUDE.md (3–5 lines):

```
## Amending These Rules
Before changing CLAUDE.md or any .claude/rules/ file:
1. Document what is wrong with the current rule and why (one sentence minimum).
2. Note whether existing code needs to be updated (backward compatibility).
3. Commit the change with message prefix `amend:` and rationale in the commit body.
4. Update CHANGELOG.md with the old rule, new rule, and effective date.
Security requirements and the "Business logic only in Services" constraint are not relaxable without explicit architecture review.
```

This adds ~8 lines to CLAUDE.md and prevents silent rule drift.

---

### OPP-6-04: Document hook enforcement boundaries and known gaps
**Target:** .claude/rules/workflow.md
**Action:** Add
**Source topic:** S6.2 / S6.2.1 — Automated Hooks + Enforcement Cost Overhead
**Rationale:** workflow.md documents that hooks exist (`UserPromptSubmit`, `Stop`) but does not explain what they enforce, where they can fail silently, or how subagents interact with them. Subagents spawned from the main agent do NOT automatically inherit user-scope hooks — only project-scope `.claude/settings.json` hooks are inherited. This is a documented Claude Code limitation (S6.2.1 Risk 3) that affects MyVocaList's multi-agent wave model. If a subagent bypasses the `Stop` hook because hooks are user-scoped, the commit-discipline rule is silently unenforced.
**Suggested content/change:** Add a new sub-section to workflow.md after Rule 2:

```
## Hook Enforcement Notes
- The Stop hook warns on uncommitted changes — this is a hard gate, not advisory.
- Hooks must be in `.claude/settings.json` (project scope) to be inherited by subagents.
  Hooks in `~/.claude/settings.json` (user scope) are NOT inherited by subagents.
- Known gap: PreToolUse exit code 2 does not reliably block `Write`/`Edit` in all harness
  versions (only `Bash` is reliable). Security-critical rules must also be in review.md.
- PreToolUse hooks should complete in <50 ms. Stop hooks may run up to 120 sec.
- If a hook fails silently (missing dependency, non-zero non-2 exit code), enforcement
  does not happen. Check hook health at session start.
```

---

### OPP-6-05: Add a review gate severity classification to review.md
**Target:** .claude/commands/review.md
**Action:** Update
**Source topic:** S6.3 — Review Gates
**Rationale:** review.md currently lists a flat checklist of items to check. The SDD review gate model distinguishes Blocker (blocks advancement), Warning (negotiable), and Suggestion (informational). Without this classification, reviewers do not know which findings must be fixed before proceeding and which are optional. This makes review outputs harder to act on and increases the risk of "rubber stamp approval" where all findings are noted but none are blocking.
**Suggested content/change:** Add a severity legend to review.md:

```
## Severity Levels
- 🔴 Blocker — Must be fixed before any further work. Examples: build failure, DisplayAlert usage,
  cross-layer dependency violation, hardcoded color/string, missing SafeAreaEdges.
- 🟡 Warning — Should be fixed; may proceed with documented justification. Examples: missing
  XML doc on a public interface method, single ReplaceRange violation with no ANR risk.
- 🟢 Suggestion — Optional improvement. Examples: naming refinement, comment clarity.

A task is only "To Review" status when there are zero Blockers.
```

Then prefix each existing checklist item with the appropriate severity marker.

---

### OPP-6-06: Add a spec-vs-code consistency check to review.md
**Target:** .claude/commands/review.md
**Action:** Add
**Source topic:** S6.3 / S6.4.1 (Category 1) — Review Gates / Behavioral Contract Violations
**Rationale:** review.md checks code quality, MAUI specifics, and architecture, but does not instruct the reviewer to check whether the implementation matches the approved spec. Behavioral contract violations — the most common drift category at 60% of incidents (S6.4.1) — are invisible to a code-only review. A validation rule silently dropped from a service, or an acceptance criterion not implemented, passes all code checks but violates the spec.
**Suggested content/change:** Add a "Spec Alignment" section to review.md's checklist:

```
## Spec Alignment (run for every implementation task)
- [ ] Read `Docs/specs/[feature]/requirements.md` acceptance criteria — each criterion
      has a corresponding test or implementation path.
- [ ] Read `Docs/specs/[feature]/design.md` — service interface signatures match design;
      no behaviors present in design are absent from implementation.
- [ ] Validation rules documented in the spec (name length, required fields, uniqueness)
      are enforced in the Service layer with a corresponding service unit test.
- [ ] If implementation differs from spec: spec is updated through change control
      (documented in commit message), NOT silently changed in code.
```

---

### OPP-6-07: Add a "spec is source of truth" rule to workflow.md
**Target:** .claude/rules/workflow.md
**Action:** Add
**Source topic:** S6.4.2 — Continuous Conformance Requirement
**Rationale:** workflow.md Rule 1 says "read the spec before coding" but does not state what happens when code diverges from the spec. The SDD principle is asymmetric: the spec is the source of truth; code that diverges from the spec must be corrected, NOT the other way around — unless the spec is formally updated. Without this rule, agents facing a conflict between spec intent and implementation convenience will resolve it by matching the code to what they have already written, silently drifting from the spec.
**Suggested content/change:** Append to Rule 1 (Spec-First):

```
### Spec is Source of Truth
When implementation diverges from the spec:
- **Default:** Fix the code to match the spec. The spec was reviewed and approved; the code was not.
- **Exception:** If the spec is demonstrably wrong (blocks a valid use case, contains an error),
  update the spec through change control (update requirements.md + design.md, commit with rationale,
  update tasks.md) — then re-implement.
- **Never:** silently make code match what is convenient, leaving the spec behind.
  This is how behavioral contract violations accumulate.
```

---

### OPP-6-08: Add a multi-agent scope conflict rule to workflow.md
**Target:** .claude/rules/workflow.md
**Action:** Add
**Source topic:** S6.4.1 (Category 6) — Multi-Agent Scope Conflicts
**Rationale:** workflow.md Rule 2 documents wave-based parallelism (max 4 subagents) but has no rule for when two subagents touch overlapping domain territory — the same entity, interface, or database table. This is the most likely source of silent incompatibility: two subagents edit different files that share a contract (e.g., a service interface + a consuming ViewModel) without coordination, and both pass their own tests but fail integration.
**Suggested content/change:** Add to Rule 2 (Subagent Delegation), under "Wave-based parallelism":

```
### Scope Isolation (Mandatory Before Dispatching a Wave)
Before dispatching parallel subagents, the main agent must verify:
- No two subagents in the same wave modify the same entity, interface, or database table.
- If two tasks touch a shared contract (e.g., one adds a service method, another adds a
  ViewModel that calls it), they are NOT parallel — mark the second `[SEQUENTIAL]` in tasks.md
  and run it after the first wave completes and is committed.
- After all subagents in a wave complete, run `dotnet build` before starting the next wave.
  A failing build from wave N is always fixed before wave N+1 begins.
```

---

### OPP-6-09: Add six spec-code drift categories as a review checklist to review.md
**Target:** .claude/commands/review.md
**Action:** Add
**Source topic:** S6.4.1 — Six Drift Categories
**Rationale:** review.md does not currently check for the six silent divergence categories identified by SDD research. Four of the six are actionable at review time without external tooling: behavioral contract violations (check spec vs service impl), validation/permission rule drops (audit service methods), static analysis suppressions (check for // ReSharper disable, #pragma warning disable without justification), and multi-agent scope conflicts (check if two recent tasks touched the same entity). These are lightweight manual checks that catch the most common (60%) drift category.
**Suggested content/change:** Add to the "Spec Alignment" section from OPP-6-06, or as a separate sub-section:

```
## Drift Detection (silent divergence checks)
- [ ] 🔴 Behavioral contracts: every validation rule in the spec (name length, uniqueness,
      required fields) has a corresponding branch in the service + a unit test for the failure path.
- [ ] 🟡 Permission/security rules: access gates documented in design.md exist in the service.
- [ ] 🟡 Suppression audit: any `#pragma warning disable`, `// ReSharper disable`, or
      `[SuppressMessage]` added in this task has a comment explaining why and when it expires.
- [ ] 🟡 Scope conflict check: if this task and another recent task both touch the same
      domain entity (e.g., Venue, Singer, Queue), verify the changes compose without silent
      incompatibility (interface change + caller change in sync).
```
