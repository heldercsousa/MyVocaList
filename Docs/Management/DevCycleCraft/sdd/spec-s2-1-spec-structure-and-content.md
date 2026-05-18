# S2.1 — Spec Structure & Content

**Status:** Researched
**Predecessor(s) ID:** S2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content researched and written by research agent |

---

## Overview

A complete specification for AI code generation must contain a set of structural elements that collectively define what the system does, what it accepts as input, what it produces as output, what constraints bound its execution, and how its state changes over time. In SDD, specifications are not descriptive narratives — they are executable contracts. Each element of the spec serves a distinct purpose in guiding AI agents toward correct implementation.

This document covers the structural elements that distinguish a complete, AI-ready spec from an incomplete one. A spec that omits any of these elements leaves gaps that agents fill with assumptions — and assumptions become latent defects.

---

## The Seven Core Structural Elements

Industry practice as of 2025–2026 has converged on seven structural elements that must be present in every complete spec. These elements map to formal software verification concepts (preconditions, postconditions, invariants) and BDD practice (Given/When/Then):

### 1. Inputs (Data and Constraints)

**What it is:** The specification of all data that flows into the system or component being specified.

**What it must contain:**
- **Source:** Where the input originates (user, API, file, message queue, database)
- **Format:** Schema, type, or structure (JSON shape, proto schema, SQL result set, object model)
- **Validation rules:** What constitutes a valid input; what must be rejected
- **Rate limits / throughput:** How many inputs per time period the system must handle
- **Constraints on values:** Ranges, lengths, enum values, forbidden combinations

**Example from spec research (2026):**
```
| Name | Source | Format | Validation | Rate Limit |
|------|--------|--------|------------|------------|
| Stripe webhook | Stripe (HTTPS POST) | StripeEvent JSON | HMAC-SHA256 signature, timestamp < 5min | 10K/min |
| Payment request | Client app (REST) | { orderId: UUID, amount: number } | JWT auth, orderId exists, amount > 0 | 100/min per client |
| Payment form | User input (web) | Form POST data | orderId required, amount 1–99,999 | 1 per user per 5s |
```

**Why agents need this:** Without explicit input schemas and validation rules, agents default to minimal validation and generic error responses. Explicit input specs prevent oversights and ensure agents implement security-critical validation.

### 2. Outputs (Deliverables and Guarantees)

**What it is:** The specification of all data that flows out of the system or component.

**What it must contain:**
- **Destination:** Where the output goes (response body, event stream, database write, log file)
- **Format:** Response shape, event schema, database row structure
- **SLA / guarantees:** Latency, availability, ordering, duplication semantics
- **Error outcomes:** What the output contains when the operation fails

**Example from spec research (2026):**
```
| Name | Destination | Format | SLA |
|------|-------------|--------|-----|
| Webhook ack | Stripe (HTTP) | 200 empty / 400 error code | < 100ms p95 |
| Payment notification | RabbitMQ (AMQP) | { event_type, payment_id, amount, timestamp } | at-least-once, < 500ms |
| Payment response | Client (HTTP) | { paymentId, status, created_at } | < 200ms p95 |
```

**Why agents need this:** Agents that do not know what to return often invent response shapes inconsistent with the rest of the API or with the caller's expectations. Output specs force consistency and precision.

### 3. Preconditions (Static and Stateful)

**What it is:** The conditions that must be true before the operation can execute.

**What it must contain (two types):**
- **Static preconditions:** Configuration, feature flags, or environmental conditions that do not change during the operation. Example: "Debug mode is enabled" or "API version is 2.1+"
- **Stateful preconditions:** The state of the system that must hold for the operation to proceed. Example: "User is authenticated and has role 'admin'" or "Order status is 'pending payment'"

**Both are expressed in the GEARS syntax (2026 standard):**
```
[Where <static-precondition>]
[While <stateful-precondition>]
When <trigger>
The <subject> shall <behavior>
```

**Example:**
```
Where API version is 2.1 or greater
While user is authenticated
When the user submits the payment form
The payment processor shall validate the amount
```

**Why agents need this:** Preconditions define the assumptions under which the spec applies. An agent that does not know whether a state precondition is met may generate incorrect code paths or fail to check required conditions before proceeding.

### 4. Postconditions and Invariants (State Guarantees)

**What it is:** The conditions that must be true after the operation completes (postconditions) and the conditions that must remain true throughout the system's lifetime (invariants).

**What it must contain:**
- **Postconditions:** What changed in the system state as a result of the operation. Example: "The order status transitions from 'pending' to 'processing'" or "A record is inserted into the payments table"
- **Invariants:** Rules that apply across all states and must never be violated. Example: "A user account balance never goes negative" or "Event IDs are globally unique"

**How to express them:**
- As assertions or conditions, ideally with formal notation if precision is critical
- Mapped back to acceptance criteria (see S2.2 for the test mapping)

**Example:**
```
Postcondition: Order record updated with payment_id and timestamp
Postcondition: Payment event published to queue with order_id, amount, status
Invariant: Order.total_amount == sum(all order line items)
Invariant: Payment events are immutable once published (no updates allowed)
```

**Why agents need this:** Invariants define the "rules of the road" — facts about the system that must never become false. Agents that do not know the invariants may generate code that violates them under edge cases.

### 5. Integration Contracts (Service Boundaries and Dependencies)

**What it is:** The explicit interfaces and guarantees at the boundary between this component and other systems it depends on or serves.

**What it must contain:**
- **External service calls:** Which services are called, what is sent, what is expected back
- **Failure scenarios:** What to do if the external service is down, slow, or returns an error
- **Retry semantics:** Whether calls are idempotent, how many retries, backoff strategy
- **Message formats:** If events are published or consumed, the full schema and versioning policy

**Example:**
```
Calls: Stripe /payment_intents POST
  Input: { amount, currency, metadata }
  Output: { id, status, created }
  Failure: If status 4xx, return 400 to client with Stripe error message
           If status 5xx, retry up to 3 times with exponential backoff (1s, 2s, 4s)
  Idempotency: Use Stripe-Idempotency-Key header with order_id as value

Publishes: order.payment.completed event to RabbitMQ
  Schema: { event_type, order_id, payment_id, amount, timestamp, user_id }
  Retention: 7 days
  Retry: at-least-once delivery (RabbitMQ ensures redelivery on nack)
```

**Why agents need this:** Integration contracts define how this component speaks to the outside world. Without them, agents may make unsafe assumptions (e.g., assuming all calls succeed, or not handling eventual consistency in distributed systems).

### 6. State Machines (Behavioral Protocol)

**What it is:** The ordered sequence of states that an entity or operation can enter, the valid transitions between states, and the conditions that trigger each transition.

**What it must contain:**
- **States:** All possible states the entity can be in
- **Transitions:** Which state to state is valid, under what conditions
- **Terminal states:** States from which no further transitions occur
- **Rejected transitions:** Attempts to move to invalid states (and what happens when that is attempted)

**Example (order lifecycle):**
```
States: CREATED → PENDING_PAYMENT → PROCESSING → FULFILLED / CANCELLED / REFUNDED

Valid transitions:
  CREATED → PENDING_PAYMENT (when user starts checkout)
  PENDING_PAYMENT → PROCESSING (when payment succeeds)
  PROCESSING → FULFILLED (when items are shipped)
  PENDING_PAYMENT → CANCELLED (if user cancels or payment expires)
  PROCESSING → REFUNDED (if user requests refund within 30 days)

Invalid transitions (and error):
  FULFILLED → PENDING_PAYMENT: 409 Conflict — order already completed
  REFUNDED → PROCESSING: 400 Bad Request — cannot restart refunded order
```

**Why agents need this:** State machines prevent agents from generating unreachable code paths or allowing invalid state transitions. They are especially critical in multi-stage workflows.

### 7. Edge Cases and Failure Modes (Complete Path Coverage)

**What it is:** The explicit enumeration of cases where the happy path does not apply, and what the system must do in each case.

**Categories (systematic checklist from 2026 research):**
- **Null / Empty:** What happens if an input is empty, null, or missing a required field?
- **Duplicates / Idempotency:** What happens if the same request is submitted twice?
- **Concurrency / Race Conditions:** What happens if two requests modify the same resource simultaneously?
- **Permissions / Visibility:** What if the caller does not have permission?
- **Temporal / Expiry:** What if a token has expired, or a time window has closed?
- **Resource Exhaustion:** What if queues are full, or rate limits are exceeded?
- **External Failures:** What if a dependency is down or slow?

**Example:**
```
Edge case: Null/empty
  Input: { orderId: null, amount: 100 }
  Expected: Return 400 with message "orderId is required"

Edge case: Duplicate (idempotency)
  Request 1: POST /payment { orderId: 123, idempotencyKey: 'abc' } → returns { paymentId: 'pay_x' }
  Request 2: POST /payment { orderId: 123, idempotencyKey: 'abc' } → returns same { paymentId: 'pay_x' }
  Expected: Exact same response, no double charge

Edge case: Expired token
  Request: POST /confirm-payment with 2-day-old confirmation token
  Expected: Return 401 with message "Token has expired"

Edge case: Concurrent updates
  Thread A: PATCH /order/123 { status: 'processing' }
  Thread B: PATCH /order/123 { status: 'cancelled' } (race with A)
  Expected: One succeeds, one returns 409 Conflict "Order was modified, read current state and retry"
```

**Why agents need this:** Edge cases are where most bugs live. Agents left to infer edge case behavior generate code that passes happy-path tests but fails in production. Explicit edge case specs eliminate this class of defect.

---

## Spec Format and Syntax Conventions

The seven structural elements can be expressed in different formats, depending on the layer and audience:

### GEARS Syntax (for behavioral requirements)

GEARS (Generalized Expression for AI-Ready Specs, 2026) extends the EARS notation to unify requirements and test scenarios:

```
[Where <static-precondition>]
[While <stateful-precondition>]
When <trigger>
The <subject> shall <behavior>
```

Maps directly to Given-When-Then:
- **Where + While** → **Given** (setup and state)
- **When** → **When** (trigger event)
- **Shall** → **Then** (required behavior)

### Markdown Tables (for contracts and interfaces)

Tabular format is preferred for inputs, outputs, constraints, and integration contracts because it is scannable and maps directly to code review:

```markdown
| Input | Source | Format | Validation | Rate Limit |
|-------|--------|--------|------------|------------|
| Payment request | Client API | { orderId, amount } | orderId exists, 0 < amount < 100K | 100/min per user |
```

### State Machine Diagrams

State transitions can be expressed as:
- Text-based ASCII state diagrams (scannable in code review)
- Formal transition tables (explicit, parseable)
- Graphical diagrams (visual, but less diff-friendly)

### YAML for Structured Specs

Some teams (especially those using Kiro or spec-kit) express inputs, outputs, and constraints in YAML for machine-readability:

```yaml
feature: Payment Processing
inputs:
  - name: payment_request
    type: object
    required:
      - order_id
      - amount
    properties:
      order_id:
        type: string
        format: uuid
      amount:
        type: number
        minimum: 0.01
        maximum: 99999
outputs:
  - name: payment_response
    type: object
    properties:
      payment_id:
        type: string
      status:
        type: string
        enum: [success, pending, failed]
```

---

## Completeness Criteria: When a Spec Is Ready

A spec is ready for agent execution when it satisfies **all seven structural elements** and each element is **specific enough to test**. Use this checklist:

| Element | Ready When |
|---------|-----------|
| **Inputs** | Every input source is named, schema is explicit (JSON shape or type), validation rules are specific (not "validate user input" but "name must be 1–50 chars, non-empty, no special characters") |
| **Outputs** | Format is explicit, error outcomes are named, SLAs are measurable (not "fast" but "< 100ms p95") |
| **Preconditions** | Static preconditions (flags, config) are enumerated; stateful preconditions (auth, resource state) are explicit |
| **Postconditions** | What changes in the system is described; what stays the same is implied |
| **Invariants** | Rules that must never be broken are stated as assertions (e.g., "balance ≥ 0", "id is globally unique") |
| **Integration Contracts** | External calls are explicit; failure modes are named; idempotency is stated |
| **State Machines** | All reachable states are named; invalid transitions are listed; terminal states are marked |
| **Edge Cases** | At least one case from each category (null, duplicate, concurrent, permission, temporal, resource, external) is enumerated |

If any of the seven elements is missing or vague, the spec is not ready.

---

## Common Pitfalls and Antipatterns

**1. Omitting Preconditions**
- **Pitfall:** Spec says "validate the order exists" but does not say "where is this order looked up?" (user account? global? after auth?)"
- **Fix:** Explicitly state "While user is authenticated and order.user_id == request.user_id"

**2. Conflating Input Format with Validation**
- **Pitfall:** "Input is a JSON payment request" (format) without stating "amount must be > 0" (validation)
- **Fix:** Separate the two: format row says `{ amount: number }`, validation row says "amount > 0 and < 100,000"

**3. Vague Postconditions**
- **Pitfall:** "The order is updated" without saying what fields changed or when
- **Fix:** "Order.status transitions from 'pending' to 'processing' AND a payment_id is recorded AND row.updated_at is set to now()"

**4. Forgetting Failure Modes in Integration Contracts**
- **Pitfall:** Spec says "call Stripe API" without saying "what if Stripe times out?"
- **Fix:** Explicitly state "If Stripe returns 5xx, retry up to 3 times with 1s/2s/4s backoff; if all retries fail, return 503 to client"

**5. Not Enumerating Edge Cases**
- **Pitfall:** Spec covers the happy path; agent must infer what to do for null, permissions, concurrency
- **Fix:** Add a section "Edge Cases" with at least one explicit scenario per category

**6. Over-Specifying Implementation Details**
- **Pitfall:** "The code shall use bcrypt with 12 salt rounds" (implementation) instead of specifying the contract ("passwords shall not be stored in plaintext; verification shall be timing-safe")
- **Fix:** Specify the contract; let the agent choose the implementation (unless the choice is constrained by policy)

---

## Relationship to Other SDD Layers

The seven structural elements appear across multiple layers of a complete SDD spec:

| Layer | Where These Elements Appear | Example |
|-------|----------------------------|---------|
| **Requirements layer** | GEARS statements; acceptance criteria; edge case scenarios | "When user submits payment form, the system shall validate amount > 0" |
| **Design layer** | API contracts; database schema; integration contracts; state machines | "POST /payments input: { orderId, amount }; output: { paymentId, status }" |
| **Task layer** | Each task implicitly tests one or more elements; tasks are ordered to build elements incrementally | Task 1: "Create Payment entity"; Task 2: "Add validation"; Task 3: "Add state machine" |

The three layers are not independent: a design decision (e.g., "orders use UUID not autoincrement ID") constrains the requirements layer (e.g., "orderId input is a UUID"), which in turn constrains tasks (e.g., "Generate UUID on order creation").

---

## Token and Cognitive Load

Specifying all seven elements takes effort, but the cost is front-loaded and recovers in reduced agent hallucination and rework.

**Why comprehensive specs reduce token cost:**
- Agents given complete specs make fewer wrong assumptions and require fewer clarification sessions
- Explicit edge case specs prevent agents from generating untested code paths
- Integration contracts prevent agents from mishandling failures, reducing debugging cycles
- State machines prevent agents from generating unreachable code

**Research finding (2025-2026):** Teams using complete specs report 60–80% fewer AI-generated regressions and 40–50% reduction in review iterations compared to teams using vague specs or prose descriptions.

---

## Sources

- [From Spec to Production: A Practical Guide to Spec-Driven Development with Claude Code — Claude Lab (2026)](https://claudelab.net/en/articles/claude-code/claude-code-spec-driven-development-workflow-complete-guide)
- [Feature Spec Template — Spec Coding (2026)](https://spec-coding.dev/templates/feature-spec)
- [Spec-Driven Development: Write the Spec First, Let AI Build It — Blink Blog (2026)](https://blink.new/blog/spec-driven-development-ai)
- [GEARS: the AI-Ready Spec Syntax — SubLang (2026)](https://sublang.xyz/ref/gears-ai-ready-spec-syntax/)
- [Phase 2: Writing Effective Specifications — The AI Agent Factory (2026)](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development/writing-effective-specs)
- [Spec Template Examples for Dev Teams — Spec Coding (2025)](https://spec-coding.dev/guides/spec-template-examples)
- [Systems Thinking Specs: Architecture, Interfaces, and State — Agenticoding (2026)](https://github.com/agenticoding/agenticoding.github.io/blob/main/website/docs/practical-techniques/lesson-13-systems-thinking-specs.md)
- [Specification-Driven Development: How to Stop Vibe Coding — Pockit Blog (2026)](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [Spec-Driven AI Coding: Writing Specs Agents Execute Well — SurePrompts (2026)](https://sureprompts.com/blog/spec-driven-ai-coding)
- [Diving Into Spec-Driven Development With GitHub Spec Kit — Microsoft for Developers (2025)](https://developer.microsoft.com/blog/spec-driven-development-spec-kit)
- [OSSA Specification: The Normative Contract for AI Agents — Open Standard Agents (2026)](https://openstandardagents.org/specification)
- [Agent Contracts: A Formal Framework for Resource-Bounded Autonomous AI Systems — arXiv (2026)](https://arxiv.org/html/2601.08815v1)
- [Open Agent Specification (Agent Spec) Technical Report — arXiv (2025)](https://arxiv.org/html/2510.04173v2)
