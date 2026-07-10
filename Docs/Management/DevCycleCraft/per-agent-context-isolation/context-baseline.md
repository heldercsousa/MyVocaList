# Per-Agent Context Isolation — Fresh Baseline (2026-07-09)

**Method:** Two parallel throwaway 0-tool probe subagents (GATE-A method, `rules-file-refactoring/findings-measurement.md`), each instructed to introspect its injected context only. Token totals from Agent-tool usage metadata; category shares are the probes' self-estimates (qualitative, proof-quoted).

## Headline numbers

| Probe | Cold-start tokens | Notes |
|-------|-------------------|-------|
| `general-purpose` (full tools) | **38,127** | vs 60,492 GATE-A probe 2026-07-04 (pre rules-refactoring + pre MCP-schema deferral) |
| `spec-reviewer` (`tools: Read, Grep, Glob` frontmatter) | **27,090** | Task 15 frontmatter groundwork measured live |

**Frontmatter tool restriction alone is worth ~11k tokens per agent** (38.1k → 27.1k).

## Category decomposition

### general-purpose (38.1k)
| Category | Share (probe estimate) | ~Tokens | Scopeable per-agent? |
|----------|------------------------|---------|----------------------|
| Tool schemas (11 full schemas incl. Bash + PowerShell) | ~35–40% | ~13–15k | YES — `tools:` frontmatter (proven by spec-reviewer delta) |
| Project CLAUDE.md + 6 rules routing tables + RTK.md | ~35–40% | ~13–15k | UNKNOWN — no known per-agent memory/rules exclusion mechanism (research item) |
| Skills listing block (40 skill descriptions) | ~12–15% | ~4.5–5.5k | Partially — absent entirely in restricted-frontmatter agents (spec-reviewer had NO skills listing) |
| Harness/system text (env, git status, scratchpad) | ~8–10% | ~3–4k | NO — fixed overhead |
| MCP schemas/instructions | ~0% (deferred, names only) | <0.5k | Already solved by Claude Code schema deferral — confirms 2026-07-09 scope update |
| Memory files | 0% | 0 | N/A — memory not injected into subagents at all |

### spec-reviewer (27.1k)
| Category | Share | Notes |
|----------|-------|-------|
| CLAUDE.md + rules inheritance | ~55–60% | Now the DOMINANT cost in restricted agents (~14–16k) — the remaining lever |
| Agent definition (spec-reviewer.md body) | ~15% | ~4–5k — self-inflicted, tunable per agent file |
| Tool schemas (3 only) | ~10% | Minimal |
| Harness text | ~10% | Fixed |
| `myvocalist-coding` skill force-preloaded | ~8% (~2.5–3k chars) | **WASTE for a read-only reviewer** — remove from reviewer `skills:` frontmatter |
| Skills listing / MCP / memory | 0% | Absent |

## Implications for scope (go/no-go input)

1. **MCP is a non-lever** — schema deferral already landed platform-side. Confirms BACKLOG row 174's 2026-07-09 scope update.
2. **`tools:` frontmatter is the biggest proven lever (~11k/agent)** — already applied to spec-reviewer/plan-reviewer (and verifier partially). NOT applied to implementor/orchestrator (no `tools:` key → full schemas + full skills listing).
3. **`skills:` preload hygiene is a small quick win (~0.7–1k/agent)** — `myvocalist-coding` is preloaded into ALL 5 agent briefs including read-only reviewers.
4. **CLAUDE.md + rules inheritance (~13–16k/agent) is the remaining large chunk** — whether it can be scoped per-agent is the single open research question for this feature. If Claude Code offers no mechanism, achievable additional savings beyond frontmatter hygiene are limited and the time-box rule applies (jump to BUG-027).
5. **Unscopeable floor** ≈ harness text + minimal tool schemas ≈ 5–7k/agent.

## Probe incidental findings

- spec-reviewer probe noted the harness prose mentions Bash but no Bash schema is provided — cosmetic inconsistency, no action.
- Neither probe saw memory-file content: per-agent memory inheritance is a non-issue for subagents today.
