# Spec-Writing Guide — MyVocaList

> Loaded on-demand. Reference when writing `requirements.md`, `design.md`, or `tasks.md`.
> For the spec decision table (when to write a spec at all), see `.claude/rules/workflow.md § Rule 1`.

---

## Spec language — determinism

In spec files (`requirements.md`, `design.md`) and in task descriptions, vague quality adjectives are **prohibited**. They force agents to invent their own thresholds, producing code that is technically compliant but misaligned with intent.

**Prohibited terms:** fast, slow, quick, responsive, robust, secure, user-friendly, intuitive, handles gracefully, works correctly, performs well, reasonable, appropriate, suitable, adequate.

**Replace with measurable thresholds:**

| Instead of | Write |
|------------|-------|
| "the list loads quickly" | "the list renders within 300 ms on a mid-range Android device" |
| "handles errors gracefully" | "returns `(false, "message")` on failure; no exception escapes the service boundary" |
| "the form validates correctly" | "name ≤ 30 chars; empty name returns `(false, "Name is required")`" |
| "secure storage" | "stored via `SecureStorage.SetAsync`; never in `Preferences` or plain SQLite" |

If the threshold is not yet known, write: `[threshold TBD — establish before implementation starts]`. This is valid in a draft spec; it is **not** valid when a task is dispatched to a subagent.

**Rule:** Any acceptance criterion containing a prohibited term is not ready for implementation. The Tester cannot write a deterministic test from it; the Builder cannot implement it without guessing.

---

## Acceptance criteria format

All acceptance criteria in `requirements.md` must use one of these two formats:

**Given/When/Then (for behavior-driven scenarios):**
```
Given [precondition]
When [action]
Then [expected outcome]
And [additional outcome if needed]
```

**EARS (Easy Approach to Requirements Syntax — for system-level rules):**
```
WHEN [trigger], the system SHALL [response]
WHILE [state], the system SHALL [behavior]
IF [condition], THEN the system SHALL [action]
The system SHALL [always-on constraint]
```

Use Given/When/Then for user-facing flows. Use EARS for background rules, constraints, and invariants.

**Never write vague acceptance criteria** such as "the system should work correctly" or "users can manage X." Every criterion must be falsifiable — a test must be writable from it.

---

## requirements.md — mandatory sections

- **User stories** — "As a [role], I want [action] so that [value]"
- **Acceptance criteria** — one per user story (see Given/When/Then format above)
- **Validation rules** — field constraints, business invariants
- **Out of Scope** — explicit list of what this feature does NOT do; prevents scope creep during implementation
- **Domain Vocabulary** — define every domain term used in the spec (e.g. "Round", "Queue Entry", "Absence"). All stakeholders and agents must use these exact terms — no synonyms.

---

## design.md — mandatory sections

- **Architecture** — which layers are affected, how they interact
- **Interfaces** — new or modified service/repository interfaces with signatures
- **Page structure** — screens, navigation flows
- **Interaction flows** — sequence of user actions and system responses
- **Invariants & Postconditions** — system invariants that must hold after every operation (e.g. "Queue always has at least one active singer", "Round number is monotonically increasing")
- **Key Decisions** — one entry per significant design choice, using this format:

  ```
  ### Decision: [short title]
  **Chosen approach:** [what was decided]
  **Alternatives considered:** [what was rejected and why]
  **Reversibility:** [Easily reversible | Reversible with effort | Hard to reverse | Irreversible]
  **Rationale:** [why this approach was chosen]
  ```

### design.md — optional but recommended sections (for complex features)

- **State machine** — if the feature introduces entity state transitions, document the full state diagram: states, transitions, triggering events, guards. Example: `QueueEntry` states: `Waiting → Singing → Done | Absent`. Without this, subagents invent their own state models.
- **Integration contracts** — if the feature calls external systems (APIs, MCPs, platform services), document the request/response contracts, error modes, and retry behavior. Never leave integration assumptions implicit.

---

## Functional vs technical separation

`requirements.md` and `design.md` serve different audiences and must not be mixed.

| Belongs in `requirements.md` | Belongs in `design.md` |
|------------------------------|------------------------|
| User goals and intent | Interface signatures |
| What the system must do | How the system does it |
| Acceptance criteria (Given/When/Then) | Layer responsibilities |
| Validation rules from user perspective | EF Core entity configuration |
| Business invariants | Navigation stack design |
| Out of scope statements | Repository method signatures |
| Domain vocabulary | ViewModel state machine |

**Rule:** If you cannot decide which file a piece of information belongs in, ask: "Is this about what the user needs, or how the system is built?" User need → `requirements.md`. System construction → `design.md`.

---

## Architecture reversibility documentation

For every significant architectural decision in `design.md`, document its **reversibility**:

| Reversibility level | Description | Example |
|--------------------|-------------|---------|
| **Easily reversible** | Change with no data migration, no interface change | Switching a sort algorithm |
| **Reversible with effort** | Requires migration or interface update | Adding a required column to an existing table |
| **Hard to reverse** | Requires data migration + downstream consumers to change | Changing an entity's primary key type |
| **Irreversible** | Cannot be undone without data loss or breaking changes | Removing a feature that users rely on |

**Rule:** Any decision rated "Hard to reverse" or "Irreversible" must be explicitly flagged in `design.md` under Key Decisions, and must be approved by Helder before implementation begins. Subagents must not make hard-to-reverse decisions unilaterally — they must escalate to the main agent.

---

## Spec format portability rule

Spec files must be written in **plain Markdown** with no tool-specific formatting, no embedded code that requires execution, and no links to external services that may become unavailable.

**Portability requirements:**
- All spec files must be readable by any Markdown viewer (GitHub, VS Code, plain text editor)
- No embedded Mermaid diagrams that require a specific renderer to be meaningful — if you use Mermaid, also include a plain-text description of the diagram's content
- No links to Confluence, Notion, Jira, Linear, or other external tools — if a decision is important, it must be in the spec file itself
- No placeholders that require another tool to fill in (e.g., `{JIRA-123}`, `{{TICKET}}`)
- No relative path assumptions — all file references should be absolute from the repository root

**Why portability matters:** Specs are the long-term memory of this project. A spec that only works in one specific tool is a spec that will become unreadable when the tool changes. Claude Code reads specs from the filesystem — it cannot authenticate to external services or render tool-specific formats.

**For diagrams:** Prefer ASCII state machines and ASCII tables over Mermaid when the diagram is simple. For complex diagrams where Mermaid adds real value, include both the Mermaid block AND a prose description below it that captures the same information in plain text.

**Example — portable state machine:**
```
QueueEntry states:
  Waiting → Singing  (triggered by: admin taps "Start singing")
  Singing → Done     (triggered by: admin taps "Done")
  Singing → Absent   (triggered by: admin taps "Mark absent")
  Absent  → Waiting  (triggered by: admin taps "Return to queue")
```

---

## Failure-mode analysis

Before finalizing a spec, perform a brief failure-mode analysis:

1. **For each acceptance criterion:** What happens if the operation fails? Is the failure mode documented in the spec?
2. **For each integration point:** What happens if the external system is unavailable or returns an error?
3. **For each state transition:** What happens if the transition is attempted from an invalid state?

Failure modes that are not in the spec will be handled inconsistently by subagents. Document them explicitly.

---

## Demo statement requirement

Every task in `tasks.md` that touches user-facing behavior must include a **demo statement**: a one-sentence description of what a human observer would see when the task is complete.

Format: `Demo: [actor] can [observable action] and sees [observable result].`

Examples:
- `Demo: Admin taps "Add Singer" and sees the new singer appear at the bottom of the queue list immediately.`
- `Demo: The queue page loads within 500ms with all singers in their correct round-based order.`
- `Demo: Tapping an absent singer shows a bottom sheet with "Mark as Returned" and "Remove" options.`

**Purpose:** Demo statements prevent tasks from being marked "done" when the code compiles but the feature doesn't work as intended. A subagent that cannot write a demo statement does not understand the task.

---

## Spec ownership constraint

**Specs are written by Helder (Architect) — not by subagents.**

Subagents implement what the spec says. They do not write, rewrite, or significantly alter specs.

| Allowed for subagents | Not allowed for subagents |
|-----------------------|--------------------------|
| Read the spec | Create `requirements.md` or `design.md` from scratch |
| Note a spec gap in the task-log (status: `blocked: spec gap`) | Fill in the spec gap unilaterally |
| Add a change note to a spec when implementation reveals a discrepancy | Rewrite acceptance criteria to match their implementation |
| Flag an ambiguous requirement with options + recommendation | Choose between ambiguous interpretations without escalating |

**Why:** Specs written by subagents reflect what the subagent found convenient to implement, not what the user actually needs. The spec is Helder's voice — it must come from Helder.

**Exception:** A subagent may add a `> **Spec updated [YYYY-MM-DD]:** one-line note` to an existing spec file when updating it per the spec versioning discipline — but only to reflect a decision that was explicitly authorized by the main agent.

---

## Tacit knowledge capture

Specs only capture what people consciously describe. Tacit knowledge — "of course it works that way" assumptions — is the primary source of spec gaps.

**Protocol:** When writing a spec, explicitly ask these questions before finalizing:
1. What would break if a new developer implemented this from scratch using only the spec?
2. What do I know about this feature that isn't written down yet?
3. What edge cases have I seen in similar features in this codebase?
4. What integrations or dependencies are assumed but not stated?

**LLM-assisted extraction technique:** After drafting a spec, prompt Claude with:
> "What assumptions are implicit in this spec that a developer would need to know but aren't written here? What edge cases are unaddressed?"

Review Claude's output and add any valid tacit knowledge to the spec before implementation starts. This technique surfaces hidden constraints before they become bugs.

---

## Over-specification guard

A spec that is too long is as harmful as one that is too short. Over-specified specs:
- Take longer to maintain than to implement
- Constrain implementation details that should be left to the developer's judgment
- Become stale faster because they describe the how, not the what

**Thin spec standard:** A good spec specifies outcomes, not implementations.

| Over-specified (avoid) | Thin (prefer) |
|------------------------|---------------|
| "The VenueRepository will use a LEFT JOIN with parameterized WHERE clause" | "Venues are searchable by name (case-insensitive)" |
| "The ViewModel will call `ReplaceRange` in a `RunOnUiThread` block" | "The list updates immediately after a venue is added" |
| "The button will have a 48dp minimum touch target" | "All touch targets meet platform UX standards" |

**Spec length guideline:** `requirements.md` should not exceed 2 pages. `design.md` should not exceed 3 pages. If you find yourself writing more, split the feature into sub-features.

---

## Decision log — fourth optional spec file

For features with many architectural trade-offs, a fourth spec file `decisions.md` may be created alongside the three standard files. This file is a chronological log of decisions made during the feature's lifetime.

**Format: `Docs/specs/[feature]/decisions.md`**

```markdown
# Decision Log — [Feature Name]

## [YYYY-MM-DD] [Short title]
**Context:** [what situation required a decision]
**Decision:** [what was decided]
**Consequences:** [what this enables or constrains going forward]
```

**When to use it:** Create `decisions.md` when:
- A feature has more than 3 Key Decisions in `design.md`
- Decisions evolve over multiple sessions
- The feature is a long-lived component likely to be revisited

**When not to use it:** Do not create `decisions.md` for simple CRUD features. Architectural overhead must be proportional to complexity.

---

## Spec versioning discipline

When a spec is updated after implementation has started:

1. **Add a change note at the top of the updated file** using this format:
   ```
   > **Spec updated [YYYY-MM-DD]:** [one sentence describing what changed and why]
   ```
2. **Do not delete old content** — mark superseded sections with `~~strikethrough~~` and add a note explaining what replaced them.
3. **Update `tasks.md`** to reflect the change: add new tasks, mark any tasks that are now irrelevant as `[CANCELLED: reason]`.
4. **Notify the main agent** by setting the task-log status to `Spec updated — re-planning required` before stopping.

Rationale: Versioned specs allow the main agent to understand what changed mid-flight and assess the impact on in-progress or pending tasks.

---

## Spec-update gate — after implementation

When a subagent's work reveals a discrepancy between the spec and the delivered code (even a "minor" one), the following must happen before the task is marked `To Review`:

1. Update `requirements.md` or `design.md` to reflect what was actually built.
2. Note the change in the task-log as `Spec updated — re-planning required` if it affects subsequent tasks.
3. Never leave the spec stale at the end of a task. A stale spec is technical debt that compounds with every subsequent wave.

> **Staleness prevention:** Every implementation task must end with a brief spec-review question: "Does the spec still accurately describe what was built?" If the answer is no, fix the spec before committing.

---

## Rebuild test — feature close-out spec quality check

When a feature is considered complete (all tasks checked in `tasks.md`, final review passed), perform the **rebuild test** as a spec quality diagnostic before closing the feature.

**Rebuild test protocol:**
1. Take the completed feature's spec (`requirements.md` + `design.md`) and the test suite — without the existing implementation
2. In a fresh Claude session (empty context), provide only the spec and the test suite and ask: "Implement this feature"
3. Compare the generated output against the actual implementation
4. Count the number of places where the generated output contradicts the delivered implementation

**Interpretation:**
| Divergences | Meaning | Action |
|-------------|---------|--------|
| 0–1 | Spec accurately describes the implementation | Close the feature |
| 2–3 | Spec has minor gaps or imprecisions | Update the spec before closing |
| 4+ | Spec has significant gaps — a new developer would implement it differently | Revise the spec substantially; consider whether the implementation itself is correct |

**This is a diagnostic, not a requirement.** Apply it:
- For complex features (Large or Epic on the spec size calibration table)
- When the implementation diverged significantly from the original spec
- When Helder suspects the spec does not reflect the delivered behavior
- As a periodic quality audit (e.g., once per quarter for active features)

**What the rebuild test reveals:**
- Missing acceptance criteria (the generator produces different behavior because there was no criterion forbidding it)
- Over-constrained specs (the generator ignores constraints that are too detailed to be meaningful)
- Tacit knowledge gaps (the generator makes a "reasonable" choice that contradicts the actual behavior because the constraint was never written)
