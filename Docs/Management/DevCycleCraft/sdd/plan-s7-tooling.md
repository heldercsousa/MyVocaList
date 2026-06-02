# S7 — Tooling: Enhancement Opportunities
> Analyzed against current .claude state (see _current_state_summary.md)
> Last reviewed: 2026-05-06

---

## Summary

| Category | Count |
|----------|-------|
| ✅ Validated (previously captured, confirmed still unimplemented) | 15 |
| 🆕 New (not previously captured) | 5 |
| **Total** | **20** |

All 15 previously captured opportunities (OPP-7-1 through OPP-7-15) remain unimplemented — confirmed by inspecting current CLAUDE.md, workflow.md, and settings.json as of 2026-05-06. Five new opportunities identified from a complete re-read of S7.x files: MCP tool batching readiness note, Playwright MCP for UI smoke testing, hooks alignment with Kiro's event-driven model, RTK token-savings hook integration with MCP calls, and Spec Kit slash command adoption path.

---

## Validated Opportunities

### ✅ OPP-7-1: Spec format portability rule — write specs in tool-agnostic markdown
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

### ✅ OPP-7-2: MCP tool availability validation — fail loudly, never skip
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

### ✅ OPP-7-3: MCP server allowlist — approved servers only, pinned, no auto-update
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

### ✅ OPP-7-4: Context window protection — limit MCP server count per agent session
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

### ✅ OPP-7-5: Spec-drift detection in review checklist
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

### ✅ OPP-7-6: Conscious tool lock-in — document ADR for Claude Code selection
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

### ✅ OPP-7-7: Context7 invocation discipline — explicit trigger conditions
**Target:** `CLAUDE.md`
**Action:** Update
**Source topic:** S7.3 — Pattern 1: Context7 for Documentation, S7.2 — Reference Stack
**Rationale:** CLAUDE.md currently says Context7 is "auto-triggered for all .NET MAUI, DevExpress, EF Core, MediatR documentation." S7.3 documents that excessive MCP tool loading degrades context quality (Context Explosion anti-pattern). The current rule is too broad — it triggers on every mention of those frameworks even when the question is architectural (not API-lookup). A tighter trigger condition would preserve context budget while still preventing hallucination on actual API calls.
**Suggested content/change:** Replace the existing Context7 auto-trigger statement in CLAUDE.md with:

```
- Context7: invoke when generating code that uses .NET MAUI, DevExpress, EF Core, or MediatR APIs — not for architectural discussion or planning steps. Trigger: `resolve-library-id` → `query-docs` for the specific class/method needed, not the full library.
```

---

### ✅ OPP-7-8: Subagent MCP isolation — each subagent uses only task-relevant servers
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

---

### ✅ OPP-7-9: Tessl Registry as a versioned skill source for DevExpress and EF Core
**Target:** `CLAUDE.md` (MCP & Skills section)
**Action:** Add
**Source topic:** S7.1 — Tessl (Architectural Fit for MyVocaList), S7.3 — Pattern 2: Tessl Registry
**Gap in current setup:** CLAUDE.md lists Context7 for library docs and `maui-skills` for MAUI patterns, but neither provides version-matched skills for DevExpress MAUI or EF Core 10 at the depth Tessl's registry offers. Context7 fetches current docs; Tessl provides curated, agent-optimized skill packages. S7.1 explicitly identifies Tessl Registry as adding "immediate value" for this project because it offers versioned skills for DevExpress, EF Core, and MediatR — the exact stack used. The 3.3× improvement in correct API usage across OSS libraries is the compelling evidence.
**Suggested content/change:** Add an evaluation note to the MCP & Skills section of CLAUDE.md and a task to SETUP_QUICKSTART.md:

```
## Tessl Registry (Evaluation)
Tessl Spec Registry (tessl.io/registry) provides version-matched library skills for DevExpress, EF Core,
and MediatR — complementing Context7 with higher-level usage patterns and project skill packages.
Evaluate for adoption when:
- Context7 returns incomplete or hallucinated DevExpress MAUI API results
- Project reaches Spec-Anchored maturity (specs link to tests via [@test] syntax)
- Team publishes internal skills (DX patterns, domain rules) for multi-agent reuse

To trial: `tessl install devexpress-maui` and compare generated code quality against Context7-only sessions.
```

---

### ✅ OPP-7-10: Cursor as a designated complementary tool for XAML/UI editing
**Target:** `CLAUDE.md`
**Action:** Add
**Source topic:** S7.2 — Integration with MyVocaList Workflow ("When to use Cursor in MyVocaList workflow")
**Gap in current setup:** CLAUDE.md defines Claude Code as the primary tool but gives no guidance on when to use a complementary tool. S7.2 explicitly documents that Cursor excels for "rapid iteration on UI code (XAML, page structure) where inline editing is faster" and provides visual diffing. The MyVocaList workflow delegates UI work to subagents who receive no guidance on what tool to use. The current CLAUDE.md Non-Negotiables rule for "Incremental edits: edit ONE file → build → fix → then next file" would benefit from a companion tool reference for the human reviewer who wants visual diffs.
**Suggested content/change:** Add to the Tool Selection section (OPP-7-6) or as a standalone note:

```
## Complementary Tooling

**Cursor** (optional, for human review sessions):
- When to use: visual diffing of large XAML changes before accepting; rapid UI iteration
- Setup: install `.cursor/rules/` mirroring the key rules from CLAUDE.md and `.claude/rules/`
- Do NOT use Cursor for autonomous task execution — Claude Code's subagent model is the only delegation mechanism
- Do NOT create `.cursorrules` with content that contradicts CLAUDE.md rules
```

---

### ✅ OPP-7-11: GitHub MCP integration for subagent coordination via live PR/issue state
**Target:** `CLAUDE.md` and `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S7.3 — Pattern 3: GitHub MCP for Live Project State, S7.3 — MCP in SDD Workflows §2
**Gap in current setup:** The current workflow.md Rule 2 (Subagent Delegation) describes how subagents coordinate by updating task-log files and committing. It does not leverage GitHub MCP's ability to check live PR and issue state. S7.3 documents a concrete pattern: agents query GitHub MCP to verify "Is there an open PR for feature X?" before starting duplicate work. This is directly relevant to the wave-based parallelism model in Rule 2, where multiple subagents could unknowingly touch overlapping files. GitHub MCP (already listed in SETUP_QUICKSTART.md Step 2, though "disabled by default") would enable pre-task collision detection.
**Suggested content/change:** Add to workflow.md Rule 2 and enable GitHub MCP with read-only scopes:

```
### Pre-task collision check (when GitHub MCP is active)
Before a subagent starts a task that touches shared files (e.g., MauiProgram.cs, AppDbContext.cs):
1. Query GitHub MCP: list open PRs touching those files
2. If a draft PR already modifies the target file, coordinate with the main agent — do not duplicate
3. After task commit, create a draft PR referencing the task-log entry

Note: GitHub MCP is enabled read-only. Write operations (create_pull_request) require explicit user approval.
```

---

### ✅ OPP-7-12: Context7 version-pinning discipline — query with specific version, not latest
**Target:** `CLAUDE.md`
**Action:** Update (extends OPP-7-7)
**Source topic:** S7.3.1 — Tool Versioning Collisions, Risk 2: Version Collisions During Updates
**Gap in current setup:** The current CLAUDE.md instructs Context7 to be invoked for specific libraries but does not specify which version to query. S7.3.1 documents a concrete failure mode: an agent queries Context7 for FluentValidation docs, writes code for v14.2, then the project upgrades to v15.0 (breaking change), and a later agent still has v14.2 cached in session context. The fix is explicit version matching: query Context7 with the exact version from the project's `.csproj`, not "latest." MyVocaList uses EF Core 10, DevExpress v25.2.4, and .NET MAUI 10 — all version-specific.
**Suggested content/change:** Extend the Context7 trigger rule in CLAUDE.md:

```
- Context7: when querying library docs, always specify the exact version from the .csproj (EF Core 10.x,
  DevExpress 25.2.x, MAUI 10.x). Never query "latest" — the installed version is the target version.
  If a version mismatch is detected between Context7's returned spec and the .csproj reference, report
  it to the user before generating code.
```

---

### ✅ OPP-7-13: MCP configuration portability — shared .mcp.json as onboarding source of truth
**Target:** `CLAUDE.md` or a new `.claude/rules/dev-environment.md`
**Action:** Add note to `CLAUDE.md`
**Source topic:** S7.3.1 — Cross-Client Configuration Portability (Gap 6)
**Gap in current setup:** S7.3.1 documents that MCP server lists, OAuth credentials, and tool allowlists are client-specific. SETUP_QUICKSTART.md instructs Claude Code to create `.mcp.json` (Step 2), but this file is not committed to version control (it contains API keys). There is no standard for a committed, sanitized `.mcp.json.template` that documents the required server configuration without secrets. New contributors onboarding must reverse-engineer the configuration. This creates reproducibility risk as the project's MCP server list grows.
**Suggested content/change:** Add a committed template file at `.mcp.json.template` and reference it in CLAUDE.md:

```
## MCP Configuration Template
`.mcp.json.template` at the project root documents the expected MCP server configuration
(without secrets). Onboarding: copy to `.mcp.json`, fill in API keys.
The template is committed; `.mcp.json` (with secrets) is gitignored.

When adding a new MCP server:
1. Add it to `.mcp.json` (local, with key)
2. Add the sanitized entry to `.mcp.json.template` (committed, key replaced with placeholder)
3. Update SETUP_QUICKSTART.md Step 2 prompt with any new manual action required
```

---

### ✅ OPP-7-14: sdd-mcp server — evaluate as a workflow-native MCP for spec execution
**Target:** `CLAUDE.md` (MCP & Skills section, evaluation note)
**Action:** Add
**Source topic:** S7.3 — Sources Tier 3: `yi-john-huang/sdd-mcp` (MCP server implementing SDD workflows; agent skills, steering, rules, hooks — v3.3, March 2026)
**Gap in current setup:** MyVocaList's SDD workflow (spec → tasks → subagent delegation → review) is implemented entirely through CLAUDE.md rules and workflow.md. The `sdd-mcp` server (GitHub: yi-john-huang/sdd-mcp) is a purpose-built MCP server that exposes SDD workflow primitives as MCP tools: agent skills, steering rules, spec hooks, task state. This would allow the main agent to query current spec state, check task completion, and invoke SDD-specific skills via the MCP protocol — making the workflow programmatic rather than prompt-driven. Not yet adopted; evaluation opportunity.
**Suggested content/change:** Add evaluation note to CLAUDE.md:

```
## sdd-mcp (Evaluation)
`yi-john-huang/sdd-mcp` (v3.3, March 2026) is an MCP server that exposes SDD workflow primitives
as callable tools: spec state queries, agent skills, steering rules, task hooks.
Evaluate when:
- The main agent repeatedly needs to query "which tasks are complete?" across sessions
- Spec-anchored maturity requires programmatic spec-to-test linkage
- The current prompt-driven task-log workflow shows reproducibility gaps

Trial: install as local stdio server, expose to Claude Code, replace manual task-log queries
with `sdd_mcp.get_task_status()` calls.
```

---

### ✅ OPP-7-15: Spec ceremony calibration — lightweight tasks should skip full spec ritual
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S7.1 — Amazon Kiro Known Limitations: "Spec Overhead at Small Scale"; S7.1.1 — When to Accept Lock-In
**Gap in current setup:** workflow.md Rule 1 (Spec-First) requires reading `design.md` before any implementation, with no exception for lightweight tasks. S7.1 documents that Kiro's spec ritual (requirements + design + tasks) generated 16 acceptance criteria for a simple bug fix — illustrating overhead at the lightweight end. MyVocaList's workflow.md has no calibration guidance: a 2-line bug fix and a new feature both require the same full spec review. The SDD literature recommends right-sizing ceremony to task scope. A lightweight path for bug fixes and minor enhancements would reduce friction without sacrificing spec discipline for complex features.
**Suggested content/change:** Add a task-scope gate to Rule 1 in workflow.md:

```
### Spec Ceremony Calibration

Not all tasks require the full spec-first ritual. Apply the right level of ceremony:

| Task type | Ceremony required |
|-----------|------------------|
| New feature (new page, new service, new entity) | Full spec: requirements.md + design.md + tasks.md |
| Enhancement to existing feature (new field, new filter) | design.md update + tasks.md entry — no new requirements.md |
| Bug fix (code change ≤ 5 lines, no new behavior) | Task-log entry only — no spec files required |
| Refactor (no behavior change) | Task-log entry + update design.md if architecture changes |

For bug fixes: create a task-log entry with "root cause" and "regression prevention" fields instead
of a full spec. This mirrors Kiro's Bugfix Spec pattern without the overhead of a full feature spec.
```

---

## New Opportunities

### 🆕 OPP-7-16: MCP tool batching readiness — annotate long-running tasks for streaming
**Target:** `CLAUDE.md` (MCP & Skills section)
**Action:** Add
**Source topic:** S7.3 — MCP Specification Evolution: "Streaming tool outputs" (Proposed), "Tool batching" (In development)
**Gap in current setup:** S7.3 documents two upcoming MCP features: (1) tool batching (agent sends 10 tool calls in one request, reducing round-trips) and (2) streaming tool outputs (long-running tools like migrations or builds stream results instead of waiting for completion). The current workflow uses dotnet build and dotnet ef migrations as shell commands outside MCP, but as the workflow matures and these tools gain MCP-native equivalents, the existing approach of waiting for full output before proceeding may become a bottleneck. There is no awareness of these emerging patterns in CLAUDE.md. A readiness note would prime adoption when these features reach Claude Code.
**Suggested content/change:** Add a forward-looking note to the MCP & Skills section of CLAUDE.md:

```
### MCP Emerging Patterns (adopt when available in Claude Code)
- **Tool batching:** When Claude Code supports sending multiple MCP tool calls in a single request,
  batch related Context7 lookups (e.g., fetch EF Core + MAUI docs in one round-trip instead of two).
  This will reduce per-task latency for multi-library implementation work.
- **Streaming tool outputs:** When streaming MCP tools land, prefer them for dotnet build equivalent
  MCP tools — avoids timeout risk on first-run builds (>30s). Until then, shell commands are preferred.
```

---

### 🆕 OPP-7-17: Playwright MCP for automated UI smoke testing of MAUI pages
**Target:** `CLAUDE.md` (MCP & Skills section, evaluation note)
**Action:** Add
**Source topic:** S7.3 — Notable MCP Server Categories: "Browser Automation: Playwright MCP (accessibility-snapshot-based, fast and deterministic)"
**Gap in current setup:** CLAUDE.md defines the testing strategy (unit + integration tests via xUnit/Moq) but has no mention of UI-level smoke testing for MAUI pages. S7.3 lists Playwright MCP as a notable server in the SDD context, described as "accessibility-snapshot-based, fast and deterministic." While Playwright MCP is browser-oriented, MAUI's Blazor Hybrid option and the DevExpress MAUI web components create a potential integration surface. More immediately, Playwright MCP could be used to validate web-based spec artifacts (MAUI component previews, design system previews) and test DevExpress's web documentation against implementation. Not yet adopted; evaluation opportunity.
**Suggested content/change:** Add an evaluation note to CLAUDE.md MCP & Skills:

```
## Playwright MCP (Evaluation)
Playwright MCP provides accessibility-snapshot-based UI testing — deterministic DOM queries
without full browser rendering overhead. Relevant evaluation triggers:
- Project adopts Blazor Hybrid pages (Playwright MCP enables automated page testing)
- DevExpress web component previews need regression testing
- Acceptance criteria in requirements.md need automated verification against a rendered UI

Not applicable to pure MAUI native pages. Evaluate at Blazor Hybrid adoption milestone.
```

---

### 🆕 OPP-7-18: Align existing hooks with Kiro's event-driven model — document hook coverage gaps
**Target:** `.claude/settings.json` (hooks section documentation) and `CLAUDE.md`
**Action:** Add
**Source topic:** S7.1 — Amazon Kiro: "Agent Hooks: Automated triggers on file-save and other events — agents run in the background to generate tests, documentation, or optimized code without user intervention"; S7.2 — SDD Workflow Integration Patterns
**Gap in current setup:** The `.claude/settings.json` already has hooks (PreToolUse, PostToolUse, PostCompact, TaskCreated, TaskCompleted, Stop). These cover workflow lifecycle events. However, Kiro's hook model includes code-quality hooks that run automatically when files are saved: generate or update unit tests on save, update documentation automatically, validate UI design compliance. The current hooks focus on task tracking and the coding-delegation gate — they do not cover test generation prompts or doc-sync reminders triggered by file edits. S7.1 shows Kiro's hooks as a differentiating SDD feature. Adding a PostToolUse hook that prompts "was a test written for this change?" on `.cs` service file edits would approximate this discipline within Claude Code's hook system.
**Suggested content/change:** Add a new PostToolUse hook entry to `.claude/settings.json` and document in CLAUDE.md:

```json
// In .claude/settings.json hooks.PostToolUse (new entry alongside existing Edit|Write matcher):
{
  "matcher": "Edit|Write",
  "hooks": [{
    "type": "command",
    "command": "python -c \"import json,sys,os; d=json.load(sys.stdin); fp=d.get('tool_input',{}).get('file_path',''); is_service=fp.endswith('.cs') and '/Services/' in fp; print('{\\\"systemMessage\\\": \\\"Reminder: does this Services change have a corresponding unit test? See testing.md TDD Workflow.\\\"}') if is_service else None\" 2>/dev/null || true"
  }]
}
```

And add to CLAUDE.md Non-Negotiables:
```
- **Test reminder hook:** A PostToolUse hook fires when editing Services/*.cs files, reminding that a unit test is required per testing.md. This is a reminder, not a gate — but treat it as one.
```

---

### 🆕 OPP-7-19: Token-efficiency rule for MCP queries — RTK integration awareness
**Target:** `CLAUDE.md` (MCP & Skills section)
**Action:** Add
**Source topic:** S7.3 — Anti-Pattern 2: Blocking on Slow Remote MCP Servers; global settings show RTK (Rust Token Killer) is active via PreToolUse Bash hook
**Gap in current setup:** The global `~/.claude/settings.json` uses RTK (Rust Token Killer) via a PreToolUse hook that rewrites commands through `rtk hook claude` for token savings. However, MCP tool calls bypass the RTK Bash hook — they are not shell commands. S7.3's Anti-Pattern 2 (blocking on slow remote MCP servers) recommends parallel queries and caching. RTK's token-saving approach (filtering verbose output) has no equivalent for MCP responses, which can be large (Context7 returning full library docs). The gap: there is no guidance in CLAUDE.md on how to limit the verbosity of MCP responses the way RTK limits shell command output.
**Suggested content/change:** Add to CLAUDE.md MCP section:

```
### MCP Response Token Discipline
MCP tool responses are not filtered by RTK (which only applies to Bash commands).
To control response size:
- Context7 `query-docs`: use targeted topic queries ("EF Core DbContext configuration") rather than
  broad library queries ("EF Core"). Broad queries return 5,000–20,000 tokens of irrelevant docs.
- SQLite MCP: limit query results — use WHERE clauses and LIMIT; never SELECT * on large tables.
- DevExpress MCP: query for specific component names, not full component libraries.
Treat MCP response tokens as session budget — each large MCP response reduces available context
for reasoning and code generation in the same session.
```

---

### 🆕 OPP-7-20: Spec Kit slash command adoption path — document migration path if needed
**Target:** `CLAUDE.md` (Tool Selection section)
**Action:** Add (extends OPP-7-6)
**Source topic:** S7.1 — GitHub Spec Kit: CLI-first workflow, 92,000+ stars, agent-agnostic; S7.1.2 — Tool-Switching Friction: Spec Kit → other tools is 1–2 weeks retraining
**Gap in current setup:** OPP-7-6 captures the ADR for Claude Code selection and acknowledges migration cost. S7.1 provides additional detail: if the team ever needed to adopt Spec Kit (the most widely adopted SDD tool, 92,000+ stars), the migration from the current custom workflow (CLAUDE.md + workflow.md) to Spec Kit's `/specify`, `/plan`, `/tasks` slash commands is materially lower friction than migrating to Kiro — because Spec Kit supports Claude Code natively and the current spec format (requirements.md / design.md / tasks.md) maps almost directly to Spec Kit's artifact set. This should be documented as a known low-friction migration path, giving the team confidence that the current setup is not a dead end.
**Suggested content/change:** Add to the Tool Selection section in CLAUDE.md (alongside OPP-7-6):

```
### Migration Path (if Spec Kit adoption becomes warranted)
The current spec format (requirements.md, design.md, tasks.md in Docs/specs/) maps directly to
GitHub Spec Kit's artifact set. Adopting Spec Kit would require:
1. `speckit init` — creates `.specify/` directory with templates
2. Moving specs from `Docs/specs/[feature]/` to `.specify/specs/[feature]/`
3. Merging CLAUDE.md + rules/*.md into `.specify/constitution.md`
4. Replacing workflow.md Rule 1-4 trigger patterns with `/specify`, `/plan`, `/tasks` slash commands

Estimated migration effort: 1–2 weeks (per S7.1.2 research). Claude Code is explicitly supported
by Spec Kit — no agent switch required. This migration path is low-friction if needed.
```
