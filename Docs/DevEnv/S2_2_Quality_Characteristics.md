# S2.2 — Quality Characteristics

**Status:** Researched
**Predecessor(s) ID:** S2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content researched and written |

---

## Overview

Quality characteristics define what separates a spec that reliably guides AI agents from one that forces them to make assumptions. The four core quality properties are ubiquitous language, Given/When/Then structure, completeness on the critical path, and clarity through determinism. Together they form a practical framework for measuring spec readiness.

This distinction matters because AI agents respond to ambiguity differently than humans do. A human developer encountering a vague requirement asks a clarifying question. An AI agent makes an assumption silently, producing code that is functionally reasonable but architecturally misaligned. Every ambiguity becomes a latent defect. The goal of quality characteristics is to eliminate the gaps that force assumptions.

---

## 1. Ubiquitous Language

**Definition:** Specs must use the project's domain vocabulary consistently, with direct traceability from business terms in the requirements layer to code-level identifiers in the design layer.

### Why It Matters
Domain language is the bridge between product intent and implementation. When requirements use domain terminology (e.g., "venue," "queue entry," "round progression") and design and code use the same terms, the agent's output feels native to the codebase. When terminology shifts or multiplies across layers (venue vs. location, queue entry vs. song submission), agents either invent their own mapping or reuse conflicting names.

Ubiquitous language is a prerequisite for both the requirements and design layers — it must be established before either is written.

### Practice
- **Establish a glossary early.** Define domain terms before writing requirements or design. Each term has a single meaning across all specs and code.
- **Trace terms across layers.** A "venue" in requirements must map directly to a `Venue` entity in code. No synonyms, no drift.
- **Enforce consistency in reviews.** When reviewing a spec, flag any new terminology or deviations from the glossary. This is a gate, not a nice-to-have.

### Example
```
Requirements (ubiquitous language):
- A venue is a named location where karaoke events occur
- A queue entry is a song-singer pairing in the event queue
- A round is a sequential pass through the queue

Design (same language):
- Entity: Venue { Id, Name, ... }
- Entity: QueueEntry { Id, SongId, SingerId, Position, ... }
- Query: GetQueueEntriesByRound(venueId, roundNumber)

Code uses the same terms: VenueService, QueueEntryRepository, Position property
```

---

## 2. Given/When/Then Structure for Scenarios

**Definition:** Acceptance criteria and edge cases written in Given/When/Then format are structured narratives that force specificity while remaining human-readable and directly consumable by agents as test-generation inputs.

### Why It Matters
Given/When/Then is the intersection of three practices:
- **BDD (Behavior-Driven Development):** Forces translation from business language to testable behavior
- **Spec-by-Example:** Concrete examples are more precisely interpreted than prose statements
- **Token efficiency:** Structured scenarios compress intent more efficiently than paragraph descriptions

The format also prevents a common gap: vague acceptance criteria that read clear in English but admit multiple implementations. "Users can reset their password" is ambiguous about token expiry, email delivery, UI flow, and error states. Given/When/Then forces every ambiguity to surface:

```
Given a user with an active account
When they request a password reset
Then a token is sent to their registered email
And the token expires after 24 hours
And a confirmation link is logged
```

Every clause is now testable. Every gap is visible.

### Structure Rules
- **Given** sets up state, permissions, and preconditions. Include the actor, their state, and what is already true. Example: "Given a user with admin role, Given the venue has 5 queued entries"
- **When** describes one and only one user or system action. A single trigger. Example: "When they click Delete"
- **Then** asserts observable outcomes: response status, database state changes, emitted events, UI transitions. Multiple Then clauses are fine if they describe consequences of the same action. Example: "Then the queue is reordered, And analytics are logged, And the user sees a confirmation"

### Pitfalls to Avoid
- **Missing preconditions in Given:** "Given a user" is incomplete. Complete it: "Given a user with edit permissions on this venue."
- **Vague Then assertions:** "works correctly," "behaves as expected," "handles gracefully" are not testable. Replace with measurable outcomes: "returns HTTP 200, updates the database, emits an event."
- **Combining multiple behavior branches:** One scenario, one behavior branch. If the scenario needs "and" in the title ("creates entry AND logs analytics"), split it into two scenarios.
- **Implementation details in Given/When/Then:** Avoid "Given the DOM loads," "When the component mounts." Use: "Given the user navigates to the queue page" (outcome-level, platform-agnostic).

### Practice
- **One scenario = one test case.** A scenario should be implementable as a single unit test without hidden assumptions or conditional branches.
- **Ask reviewers to challenge Given state.** "What if the user didn't have edit permissions?" surfaces missing scenarios.
- **Link each scenario to a test before implementation begins.** Traceability established upfront prevents spec/test drift.

---

## 3. Completeness on the Critical Path, Conciseness Everywhere Else

**Definition:** A spec must cover every branch with a different expected outcome; it must not enumerate exhaustive input combinations that differ only in scale.

### Why It Matters
Over-specification leads to maintenance burden and false precision. Under-specification forces agents to invent behavior. The balance is pragmatic: cover the critical path; omit the obvious variations.

The Thoughtworks analysis (Dec 2025) notes: "experienced programmers may find that over-formalized specs can cause unnecessary trouble, and slow down change and feedback cycles." The goal is precision where it prevents structural confusion, relaxation where the agent's default is acceptable.

### Examples of Complete vs. Over-Specified

**Complete (good):**
```
Create a venue:
  Given a valid venue name (1–30 chars)
  When the user submits the form
  Then the venue is persisted, assigned an ID, and displayed in the list

  Scenario: Duplicate name
    Given a venue named "Jazz Club" exists
    When the user submits a venue named "Jazz Club"
    Then an error is returned: "A venue with this name already exists"

  Scenario: Name too long
    Given the user enters a name longer than 30 characters
    When they submit
    Then an error is returned: "Name must not exceed 30 characters"
```

**Over-Specified (avoid):**
```
Create a venue:
  Scenario: Valid 1-character name → persisted
  Scenario: Valid 2-character name → persisted
  Scenario: Valid 15-character name → persisted
  Scenario: Valid 30-character name → persisted
  Scenario: Name with spaces → persisted
  Scenario: Name with hyphens → persisted
  Scenario: Name with apostrophes → persisted
  [... dozens more input variations]
```

The over-specified version adds no behavioral clarity — it enumerates input variations without testing different outcomes. The complete version tests the essential branches: valid creation, duplicate error, validation error.

### Decision Rule
If a branch changes the return value, error message, or system state, it needs a scenario. If it doesn't, it probably doesn't need its own entry.

### Practice
- **Identify critical paths first.** What are the happy path and the error paths that have materially different outcomes?
- **Use data-driven approaches for input validation.** Rather than listing 20 valid name formats, write: "A venue name is valid if it contains 1–30 characters and matches the pattern `[A-Za-z0-9 \-'.]`" Then test the boundary cases (empty, 1 char, 30 chars, 31 chars) not every valid combination.
- **Trust the agent's defaults for non-critical variation.** If the spec says "return an error message," the agent will. You don't need to specify every possible error message wording.

---

## 4. Clarity and Determinism

**Definition:** Every requirement must be expressed as an observable, testable outcome. Vague language (fast, robust, handles gracefully) is not a requirement — it is a wish. Deterministic language removes hallucination and makes output reviewable against a known standard.

### Why It Matters
Vague language produces non-deterministic agent behavior. When an agent encounters "the list shall load without visible delay," it makes an assumption about what "visible delay" means. On a fast device, it might assume 500ms is acceptable. On a slow device, 2 seconds. The two implementations contradict each other, and both are wrong because the spec was ambiguous.

Deterministic language provides an oracle that the agent can check against: "the list shall render within 300ms of navigation on a mid-range Android device."

### Examples of Vague vs. Deterministic

| Vague | Deterministic |
|-------|---------------|
| The system handles errors gracefully | On validation failure, return HTTP 400 with a JSON body: `{ "error": "name_too_long", "message": "..." }` |
| The feature is performant | Database queries must complete in under 100ms on the test server (3GHz CPU, 4GB RAM) |
| Users can intuitively navigate the interface | The primary action (delete queue) is a red button with a confirmation dialog; users can undo accidental deletion within 30 seconds |
| The system is secure | Passwords are hashed with bcrypt (12 rounds); session tokens expire after 1 hour of inactivity |
| The feature scales well | Support at least 1,000 concurrent users without degrading response time below 500ms p95 |

### Practice
- **Replace adjectives with measures.** "Fast" → "under 300ms"; "robust" → "handles 10,000 concurrent requests"; "user-friendly" → "5 taps to complete the task" or "documented with a 2-minute tutorial."
- **Specify observable outcomes, not internal mechanisms.** Rather than "the event is published via MediatR," say "after creation, other users see the new entry in their queue within 2 seconds."
- **Define failure contracts explicitly.** What is the error code? The message? Is it a JSON object or plain text? Can the user retry? Is the state partially committed?
- **Use examples as oracles.** "When the user adds a song, the queue position is incremented by 1 and the UI updates immediately" is testable by running the action and checking the position.

---

## Good Specs Share Four Properties

Synthesizing the academic work on SDD (arXiv:2602.00180 and related papers) and industry practice (Thoughtworks, GitHub, Kiro):

1. **Behavior-focused:** Specs describe what happens, not how the system is built. They avoid prescribing algorithms, data structures, or technology choices.

2. **Testable:** Each requirement is verifiable. A test can fail (or pass) based on the spec. This is the definition of testability.

3. **Unambiguous:** Different readers (human or AI) reach the same interpretation. Vague words like "suitable," "reasonable," "typical" are eliminated.

4. **Complete enough to cover essential cases without over-specifying.** Every branch with a different outcome is covered. Input variations that produce the same outcome are grouped and described abstractly.

---

## Quality Gates: How to Review for Quality

Before handing a spec to an agent:

1. **Domain language check.** Every business term in requirements maps to a code-level identifier. No synonyms. No new terms.

2. **Given/When/Then audit.** Every acceptance criterion follows the structure. No vague Then assertions. One scenario per behavior branch.

3. **Completeness audit.** For each design decision, ask: "Is the critical path covered? Are error cases explicit?" If a question would require a clarifying call, the spec is incomplete.

4. **Determinism audit.** Read every quality attribute. If it contains "should," "might," "typically," "reasonable," flag it. Replace with a measurable property.

5. **Traceability.** Each scenario in the spec has a corresponding test ID or test file path. Traceability is established before implementation, not after.

---

## Integration With the SDD Workflow

Quality characteristics are checked during the specification phase (S3.1) before the plan phase begins. A spec that fails any of the four quality checks should be revised, not handed to an agent for implementation. This gate prevents the common failure mode: an agent implementing an under-specified feature and producing code that is functionally correct but architecturally misaligned with the team's intent.

The four characteristics also inform the review process described in S3.3 (Verification / Review Gates).

---

## Sources

- [Spec-driven development: Unpacking one of 2025's key new AI-assisted engineering practices — Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180](https://arxiv.org/html/2602.00180v1)
- [Given When Then Template for Product Specs — Spec Coding](https://spec-coding.dev/guides/given-when-then-template)
- [Gherkin Tests: An Insider's Guide to Effective Specification and Validation — HoBSoft](https://hobsoft.com/guides/gherkin-tests-an-insiders-guide-to-effective-specification-and-validation)
- [Notes on Spec-Driven Development — Antonis Pantelides](https://apantelides.com/notes/2025-09-23-notes-on-spec-driven-development/)
- [Agile Speccing: Writing Feature Specs That Actually Work — Mostly Lucid](https://www.mostlylucid.net/blog/writingfeaturespecs)
- [How to Write Cucumber Specifications the Right Way — Jakub Sobolewski](https://jakubsobolewski.com/blog/ai-assisted-specifications/)
- [Diving Into Spec-Driven Development With GitHub Spec Kit — Microsoft for Developers](https://developer.microsoft.com/blog/spec-driven-development-spec-kit)
- [How to write a good spec for AI agents — Addy Osmani](https://addyo.substack.com/p/how-to-write-a-good-spec-for-ai-agents)
- [Simplex — Specification Language for Agentic Coding and Autonomous AI Agents](https://simplex-spec.org/)
- [Open Agent Specification (Agent Spec) — arXiv:2510.04173](https://arxiv.org/html/2510.04173v4)
- [Prism: A Minimal Compositional Metalanguage for Specifying Agent Behaviour — arXiv:2512.00611](https://arxiv.org/html/2512.00611v1)
