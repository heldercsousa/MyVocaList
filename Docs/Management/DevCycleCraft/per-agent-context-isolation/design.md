# Per-Agent Context Isolation — Design (MVP)

**Approved direction (Helder, 2026-07-09):** slim config-only MVP — agent-brief frontmatter changes only. No hooks, no settings.json edits, no agent-body rewrites, no new components.

## Research findings (closes BACKLOG row 174 items a–d)

Source: official docs, code.claude.com/docs/en/sub-agents.md + context-window.md (claude-code-guide agent, 2026-07-09). Measured baseline: `context-baseline.md`.

| Row 174 research item | Answer |
|---|---|
| (a) Per-agent MCP scoping | YES — `mcpServers:` frontmatter exists, but LOW VALUE: schemas are deferred (names only, <0.5k) |
| (b) Worktree-scoped `.mcp.json` overlays | Obsolete — solves the already-solved MCP problem; does nothing for rules/CLAUDE.md inheritance |
| (c) Load/unload hook pattern | Obsolete/meaningless — subagent context dies with the agent; nothing to unload |
| (d) MCP cost model | Answered platform-side — schemas deferred by default (`ENABLE_TOOL_SEARCH=auto`), near-zero cold-start cost |

**Hard limits (non-levers — cited, do not re-research):**
- **CLAUDE.md + `.claude/rules/*.md` injection cannot be scoped per custom agent.** "Explore and Plan are the only subagents that omit CLAUDE.md… There is no frontmatter field or per-agent setting to change which agents skip them." (sub-agents.md § Manage subagent context). This is the ~13–16k/agent floor.
- **Skills-listing block** (~4.5–5.5k in full-tool agents) has no per-agent control; only global per-skill `disable-model-invocation: true` shrinks it. Out of scope.
- **Memory files are not injected into subagents** (probe-verified) — non-issue.
- **Harness/system text** ~3–4k — fixed.
- Practical per-agent floor ≈ **18–23k**. The reachable delta above that floor is what this MVP claims.

**Working levers (all frontmatter):** `tools:` (allowlist), `disallowedTools:` (denylist), `skills:` (each listed skill's FULL body is preloaded; omitting the key does not remove the listing block, only the preload).

> **Addendum (2026-07-09, post-spec-review follow-up research):** per-agent rules scoping does not exist, but **path-scoped rules DO** — `.claude/rules/*.md` files can carry `paths:` frontmatter (glob list) so the rule loads only when matching files are read (memory.md § Path-specific rules). Also confirmed: CLAUDE.md always loads in full (no offset/partial/truncation mechanism — only MEMORY.md truncates at 200 lines/25KB), and `@`-imports are inline-expanded at launch, never lazy. Path-scoping is a *conditional* lever, not a *per-agent* one, and never-miss HARD RULEs must stay unconditional — so it is registered as an out-of-scope follow-up (see requirements.md), not folded into this MVP. Open question for that follow-up: whether path-scoped rules are also excluded from subagent cold-start injection (needs its own probe).

## Changes

One file each, frontmatter only:

| File | Change | Rationale | Est. saving |
|---|---|---|---|
> **Spec updated [2026-07-10]:** Task 2 probe resolved the row-1 uncertainty — `disallowedTools:` does NOT suppress the skills-listing block (~2–2.5k remains in implementor). Measured implementor post-change: 37,370 (formal ≤35,127 line FAILED — comparator mismatch, see `context-baseline.md § Post-change`; like-for-like saving ~4–5k, within the 3–6k estimate below).

| `.claude/agents/implementor.md` | add `disallowedTools: Agent, Artifact, NotebookEdit, PowerShell`; keep `skills: myvocalist-coding` | Never sub-dispatches, publishes artifacts, or edits notebooks; Bash covers `dotnet`/`git`. Denylist (not allowlist) so future harness tools aren't silently lost. Preload kept — it is the coding-rules router used on every task. *Uncertainty: the baseline's ~11k lever came from a `tools:` allowlist, which also dropped the skills-listing block; whether `disallowedTools` alone does the same is unverified — the Task 2 probe decides* | ~3–6k × 4/wave |
| `.claude/agents/orchestrator.md` | add `disallowedTools: Artifact, NotebookEdit`; remove `skills:` block | Must keep Agent (dispatch), Edit/Write (tasks.md markers, BACKLOG, handoffs), Bash (git/dotnet). Never implements → preload is dead weight; inherited rules routing tables already point to the library | ~1–2k/session |
| `.claude/agents/spec-reviewer.md` | remove `skills:` block; `tools: Read, Grep, Glob` unchanged | Report-only; can Read `.claude/library/*.md` directly via routing tables it inherits | ~0.7–1k |
| `.claude/agents/plan-reviewer.md` | remove `skills:` block; `tools:` unchanged | Same | ~0.7–1k |
| `.claude/agents/verifier.md` | remove `skills:` block; `tools: Read, Grep, Glob, Bash` unchanged | Same; compliance checklist sources are the inherited rules + library files it can Read | ~0.7–1k |

## Per-agent tool-need risk assessment (REQ-CTXISO-04)

| Agent | Denied tool | Protocol reference check | Risk |
|---|---|---|---|
| implementor | Agent | Body forbids sub-dispatch already | None |
| implementor | Artifact / NotebookEdit | Never referenced | None |
| implementor | PowerShell | Body's commands are `dotnet`/`git` (shell-agnostic; Bash retained). Watch item: any future task briefing that requires a `.ps1` script must run via `pwsh -File` under Bash or revert this line | Low |
| orchestrator | Artifact / NotebookEdit | Never referenced | None |
| orchestrator | (skills preload removed) | Body references myvocalist-coding as a dispatch-briefing pointer, not a self-use dependency | None |
| reviewers ×3 | (skills preload removed) | Checklists cite rules files (inherited) + library paths (Read-able) | Low — validated live by REQ-CTXISO-06 |

## Verification

1. **Probe (single, token-thrifty):** after the frontmatter edits, dispatch ONE 0-tool implementor probe (same instructions as the 2026-07-09 baseline probes). Record the number in `context-baseline.md § Post-change`. Pass: below 38,127 baseline by ≥3k. *(Note: implementor was not probed pre-change; the general-purpose figure is the comparator — implementor's own definition + skill preload add ~3–4k on top, so any result ≤35,127 confirms net win; expected ~30–33k.)*
2. **Live reviewer validation:** after Task 1 lands, dispatch the `verifier` on Task 1's commit — it runs with the reduced frontmatter (REQ-CTXISO-06). The pre-change spec review of this feature does not count.
3. **No build/test impact:** no source, project, or config-with-runtime-effect files touched — `dotnet` gates not applicable (Docs/`.claude` only).

## Error handling / rollback

Every change is a 1-line frontmatter revert. If any agent fails at dispatch time (unsupported key on installed CC version, missing needed tool), revert that agent's line and record the failure in `task-log.md` — do not work around with body edits.

## Testing

No code → no unit tests (testing.md Level C: plumbing/config — no mandatory test; this design note is the documented no-test decision). Verification is the probe + live dispatches above.
