# S7 — Tooling: Enhancement Opportunities
> Analyzed against current .claude state (see _current_state_summary.md)

---

### OPP-7-1: Spec format portability rule — write specs in tool-agnostic markdown
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S7.1.2 — Tool-Switching Friction
**Rationale:** S7.1.2 documents concrete cases where EARS notation and tool-specific syntax embedded in specs locked teams into a single SDD tool. MyVocaList's specs (requirements.md, design.md, tasks.md) are already close to portable markdown, but there is no explicit rule to keep them that way. As the spec corpus grows, accidental tool coupling can creep in. A rule would prevent this structurally.
**Suggested content/change:** Add a new rule (e.g., Rule 7) to workflow.md:

```
## Rule 7 — Spec Format Portability

Spec files (`requirements.md`, `design.md`, `tasks.md`) must be written in plain markdown only.

- Do NOT use EARS notation, Mermaid-required directives, or tool-specific linking syntax
- Do NOT embed Claude-Code-specific hook syntax in spec content
- Acceptance criteria: plain bulleted lists or checkboxes — not structured natural language parsers
- Architecture diagrams in design.md: ASCII or fenced code blocks only (not rendered-Mermaid-required)

Reason: specs are long-lived codebase artifacts. If the project moves to a different AI assistant (Cursor, Copilot), specs must remain readable and consumable without reformatting.
```

---

### OPP-7-2: MCP tool availability validation — fail loudly, never skip
**Target:** `CLAUDE.md`
**Action:** Add
**Source topic:** S7.3.1 — MCP Protocol Immaturity (Risk 1: Silent Tool Unavailability)
**Rationale:** S7.3.1 documents a specific failure mode: when an MCP server is unavailable, agents may silently skip the step rather than failing. For MyVocaList, Context7 (library docs) and SQLite MCP (live db inspection) are used in critical workflows. If Context7 is unreachable during a MAUI implementation task, the agent may generate hallucinated API calls silently. There is currently no rule requiring explicit failure.
**Suggested content/change:** Add to the MCP & Skills section in CLAUDE.md:

```
### MCP Availability Gate
If a required MCP server (Context7, SQLite) is unavailable at task start:
- Do NOT silently skip the lookup and proceed
- Fail with an explicit message: "Context7 MCP unavailable — cannot proceed without library documentation"
- Wait for user to restore the connection or explicitly authorize proceeding without docs
Never assume a missing tool response means the tool found nothing — distinguish "tool returned empty" from "tool unavailable"
```

---

### OPP-7-3: MCP server allowlist — approved servers only, pinned, no auto-update
**Target:** `CLAUDE.md`
**Action:** Add
**Source topic:** S7.3 — MCP Servers (Security), S7.3.1 — MCP Protocol Immaturity
**Rationale:** S7.3 documents that 9 of 11 MCP registries were successfully poisoned in April 2026, and 41% of registry servers have no authentication. The practical mitigation is an explicit allowlist of approved servers with pinned versions. CLAUDE.md currently documents which MCPs exist but has no security stance on how they must be configured.
**Suggested content/change:** Add to the MCP & Skills section in CLAUDE.md:

```
### MCP Security Stance
Approved MCP servers for this project (local-first only):
- Context7 (library docs) — official server only; never install `context7-docs` or similarly named variants
- SQLite MCP — local stdio only; db at `.claude/MyVocaList.db`
- DevExpress MAUI MCP — project-installed only

Rules:
- Never add an MCP server discovered from a public registry without explicit review
- Pinned versions in `.claude/settings.json` — no auto-update from registries
- If a new MCP server is needed, add it to this list first with justification
```

---

### OPP-7-4: Context window protection — limit MCP server count per agent session
**Target:** `CLAUDE.md`
**Action:** Add
**Source topic:** S7.3 — Anti-Pattern 1: Too Many Servers, S7.3.1 — Context Explosion
**Rationale:** S7.3 documents that Google dropped MCP from its Workspace CLI after tool definitions from multiple servers inflated context windows to 40,000–100,000 tokens, degrading reasoning quality. MyVocaList currently lists Context7, SQLite, and DevExpress MCPs in CLAUDE.md. There is no guidance on avoiding context bloat when multiple MCPs are active simultaneously.
**Suggested content/change:** Add to the MCP & Skills section in CLAUDE.md:

```
### MCP Context Budget
Do not activate all MCP servers in every session. Load only what the current task requires:
- MAUI/DevExpress implementation: Context7 + DevExpress MCP only
- Database schema work: SQLite MCP only
- Tasks that don't touch MAUI APIs: disable Context7 to reduce context overhead

If tool definitions from all active MCPs exceed ~5,000 tokens combined, deactivate the least-relevant server for that session.
```

---

### OPP-7-5: Spec-drift detection in review checklist
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S7.1 — Kiro: "Specs as Living Artifacts", S7.2 — SDD Workflow Integration Patterns
**Rationale:** S7.1 highlights that one of Kiro's explicit features is keeping specs synced with code — solving the common problem where specs become stale during implementation. MyVocaList's `review.md` checklist focuses on code quality but does not include a spec-drift check. After a task completes, the reviewer should verify that `design.md` and `tasks.md` still accurately reflect what was built.
**Suggested content/change:** Add a new checklist section to `.claude/commands/review.md`:

```
## Spec Drift Check
After every task completion review:
- [ ] `tasks.md` checkbox is checked off for this task
- [ ] `design.md` still accurately describes what was built (no undocumented architectural decisions)
- [ ] `requirements.md` acceptance criteria are still valid (no scope changes that weren't reflected)
- If any spec file is out of sync with the implementation, update the spec BEFORE merging the code change
- Document any design decisions that weren't anticipated in the spec as a `### Decision:` entry in `design.md`
```

---

### OPP-7-6: Conscious tool lock-in — document ADR for Claude Code selection
**Target:** `CLAUDE.md`
**Action:** Add
**Source topic:** S7.1.1 — Vendor Lock-In (Accept Lock-In Consciously, Mitigation Strategy 1)
**Rationale:** S7.1.1 establishes that the best practice when accepting tool lock-in is to document the decision with explicit trade-offs and a re-evaluation horizon. CLAUDE.md currently names Claude Code as the tool but provides no lock-in rationale. Adding a brief ADR-style note signals to future contributors why the tool was chosen and when to reconsider, without requiring a separate ADR file.
**Suggested content/change:** Add a new subsection under the Roles section of CLAUDE.md:

```
## Tool Selection

**Primary AI assistant:** Claude Code (Anthropic CLI)
**Decision rationale:** Spec-first discipline (CLAUDE.md + rules files), subagent delegation support, 1M-token context window, terminal-native workflow, MCP client built-in.
**Lock-in accepted:** Spec format and rules files are Claude Code-specific; migrating to Cursor or Copilot would require translating CLAUDE.md to `.cursorrules` or `copilot-instructions.md`.
**Re-evaluation trigger:** If Anthropic discontinues Claude Code, pricing exceeds $200/month, or a competing tool delivers >2x productivity improvement on SDD tasks.
```

---

### OPP-7-7: Context7 invocation discipline — explicit trigger conditions
**Target:** `CLAUDE.md`
**Action:** Update
**Source topic:** S7.3 — Pattern 1: Context7 for Documentation, S7.2 — Reference Stack
**Rationale:** CLAUDE.md currently says Context7 is "auto-triggered for all .NET MAUI, DevExpress, EF Core, MediatR documentation." S7.3 documents that excessive MCP tool loading degrades context quality (Context Explosion anti-pattern). The current rule is too broad — it triggers on every mention of those frameworks even when the question is architectural (not API-lookup). A tighter trigger condition would preserve context budget while still preventing hallucination on actual API calls.
**Suggested content/change:** Replace the existing Context7 auto-trigger statement in CLAUDE.md with:

```
- Context7: invoke when generating code that uses .NET MAUI, DevExpress, EF Core, or MediatR APIs — not for architectural discussion or planning steps. Trigger: `resolve-library-id` → `query-docs` for the specific class/method needed, not the full library.
```

---

### OPP-7-8: Subagent MCP isolation — each subagent uses only task-relevant servers
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S7.3 — Anti-Pattern 1, S7.3.1 — Immaturity, S7.2 — Subagent Delegation
**Rationale:** workflow.md documents how subagents are briefed but does not specify which MCP servers subagents should activate. If a subagent for "add database index" activates DevExpress MCP + Context7 + SQLite simultaneously, it wastes 15–30K tokens on irrelevant tool definitions. The briefing protocol should include an explicit MCP scope.
**Suggested content/change:** Add to the briefing protocol in Rule 2 of workflow.md:

```
### MCP scope in subagent briefings
Include in every subagent briefing which MCP servers the subagent should activate:
- Implementation task (Services/Domain): Context7 for EF Core/MediatR only — no DevExpress MCP
- UI task (XAML/pages): Context7 for MAUI/DevExpress + DevExpress MCP — no SQLite MCP
- Database/migration task: SQLite MCP only — no DevExpress MCP
- Explicitly state: "Activate only [list] MCP servers for this task"
```
