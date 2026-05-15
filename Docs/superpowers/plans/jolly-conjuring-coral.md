# Plan: Claude Managed Agents — Applicability to MyVocaList Dev Cycle

**Type:** Research / Evaluation (no code changes)
**Date:** 2026-05-15

---

## Context

Anthropic released **Claude Managed Agents** in April/May 2026 — a hosted service for running long-lived, persistent autonomous agents in production. The question is whether it can accelerate the MyVocaList development cycle (spec-writing → subagent delegation → build/test/commit).

---

## What Claude Managed Agents Is

Managed Agents is a **hosted infrastructure service** — Anthropic runs the compute. It decouples:

- **Brain** — Claude model + harness (reasoning)
- **Hands** — sandboxes + tools (bash, file I/O, code execution, MCP, web search)
- **Session** — append-only event log (persistent state across restarts)

Key SDK: Python (`anthropics/claude-agent-sdk-python`) and TypeScript (`@anthropic-ai/claude-agent-sdk`).

---

## How It Compares to the Current Dev Model

| Dimension | Current (Claude Code subagents) | Managed Agents |
|---|---|---|
| Lifetime | Session-scoped, ephemeral | Long-running, persistent |
| Infrastructure | Runs on Helder's machine | Anthropic-hosted cloud |
| File access | Local repo via tool calls | Sandboxed environment |
| State | Context-window bound | Persistent event log + memory stores |
| Interface | Interactive CLI | REST API + SDK |
| Cost unit | Claude Code subscription | Token cost + $0.08/session-hour |
| Context | Full CLAUDE.md + rules | Must be injected via system prompt |

---

## Pricing (Managed Agents)

- **Claude Sonnet 4.6:** $3/MTok input · $15/MTok output
- **Session runtime:** $0.08 per active session-hour
- Example: 1-hour coding session with 200k tokens ≈ ~$0.68 + $0.08 = **~$0.76/session**

Current Claude Code subscription is flat-rate — Managed Agents would add per-use cost.

---

## Where It Could Help MyVocaList

### ✅ High Fit — Long-Running Parallel Subagent Waves

The most valuable use case: dispatching **multiple independent build/test/commit agents in parallel** that run for 30–90 minutes without tying up the local Claude Code session.

Currently, Claude Code runs subagents sequentially or in limited parallel within one session. Managed Agents could run Wave 1 agents (Domain layer) while Wave 2 agents (Services layer) are already being prepared — true asynchronous wave execution.

**Concrete example:** Phase 11 (Artists & Songs) had Agent A, B, C needing `/clear` between each. With Managed Agents, all three could run simultaneously against isolated worktrees, each with their own sandbox.

### ✅ High Fit — Persistence Across `/clear`

Current pain: context compaction (`/clear`) loses subagent memory, requiring re-briefing. Managed Agent sessions persist the full event log — a re-started harness `wake(sessionId)` resumes exactly where it left off.

### ✅ Medium Fit — Offloading Build/Test Loops

`dotnet build` + `dotnet test` loops are mechanical and slow (30–90s). Managed Agents could run these loops asynchronously while Helder continues spec work in Claude Code.

### ❌ Low Fit — Local MAUI Emulator Testing

Managed Agents run in Anthropic-hosted sandboxes — they cannot launch the Android emulator, run MAUI on-device, or test DevExpress rendering. All UI validation still requires Claude Code on the local machine.

### ❌ Low Fit — Replacing Claude Code

Claude Code has the full project context (CLAUDE.md, rules, skills, hooks). Managed Agents would require injecting all of that via system prompt — significant overhead and fragility risk. This is not a replacement; it's a complement.

---

## Recommended Adoption Pattern (if any)

**Do not replace Claude Code with Managed Agents.** Use a hybrid:

1. **Claude Code** (main agent) — orchestration, spec-writing, plan creation, rule enforcement, final review
2. **Managed Agents** (parallel workers) — long-running mechanical tasks: build loops, test execution, repository-only file edits without UI validation

**Gate:** Managed Agents only make sense when:
- A wave has ≥ 3 independent tasks taking > 30 min each
- Tasks don't require local emulator testing
- The cost per session (~$0.76) is justified vs. waiting sequentially

**For MyVocaList at current pace:** The project has ~1–2 feature sessions/week. Managed Agents would add complexity (API setup, sandbox configuration, credential injection) for marginal gain on a project of this size. The break-even point is when multi-session, multi-day feature waves are the norm.

---

## Verdict

| Verdict | Reason |
|---|---|
| **Not yet — revisit at Phase 15+** | Current session volume doesn't justify the setup cost. The existing Claude Code subagent model (with worktrees) handles MyVocaList's concurrency needs adequately. |
| **Watch for:** | MAUI sandbox support, local file system mounting, or Claude Code native integration with Managed Agents (planned) |
| **Trigger to adopt:** | When a single feature requires >4 parallel agents running >1 hour each, or when sessions regularly hit context limits mid-wave |

---

## No Code Changes Required

This is a pure evaluation. No files in the project are modified. The findings are recorded here and optionally in project memory.
