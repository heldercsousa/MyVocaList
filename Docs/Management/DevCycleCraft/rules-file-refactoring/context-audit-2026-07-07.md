# Context & Skill-Configuration Audit — 2026-07-07

**Scope:** Post-implementation evaluation of the *Rules File Refactoring — Unconditional Load* feature (all 12 tasks), plus the follow-up questions Helder raised: skill routing vs enablement, automatic skill loading per agent, library/skill duplication, `Docs/Management` auto-load, Claude's persistent memory, downloaded-but-disabled skill inventory, and user-vs-project tool scoping.
**Method:** Fresh-session `/context` (41.7k/1M, memory 20.8k) as post-state; GATE-A probe (60,492) as pre-state; disk inventory of `~/.claude` and `.claude/`; official docs verified for `skillOverrides` (undocumented) and per-skill invocation control (frontmatter only).
**Session:** main agent direct (docs/config inspection only — no source files read; orchestrator read-scope respected).

---

## Part 1 — Feature evaluation (what the refactoring actually achieved)

### Measured vs claimed

| Metric | Before | After (2026-07-07) | Claimed |
|--------|--------|--------------------|---------|
| Rules files total | ~18–22k (GATE-A) | **11.0k** | target ~2–3k; "16–19k/agent recovered" |
| Memory category | 29.2k (Helder's check) | 20.8k | — |
| Session cold overhead | 60,492 (GATE-A subagent, 0 tools) | ~40.4k (main session, excl. messages) | — |

- **Real, sticky, per-agent win: ~8–11k in rules + CLAUDE.md trims (~20k on total cold start).** Genuine and worth having.
- **The ~2–3k routing-table target was missed ~4×** because every refactor deliberately kept never-miss HARD RULEs inline (`workflow.md` alone still 5.1k / 174 lines). Defensible trade-off — but the promised figures were never reconciled.
- **Per-task BACKLOG savings claims do not sum:** rows record ~28k+ "recovered" vs ~8–11k measured. Per-line token estimates were inflated (actual ≈29 tok/line vs assumed ≈17–20). Rows present estimates as realized measurements.
- **`tasks.md` success criteria #1 ("all rules files 1–2 pages") and #4 ("net 14k savings per skill used") are checked ✅ but false/incoherent** — violates verification-before-completion.
- **No like-for-like post-probe exists** (GATE-A measured a 0-tool subagent; post-state only has main-session `/context`). Budget reset 2026-07-07 → one cheap probe would close the loop honestly.
- **What was done well:** GATE-A before file work; GATE-B decided analytically (no wasted 60k probe); over-fragmentation guard applied consistently; inbound-`§`-anchor grep discipline; Task 11 measurement-driven correction cycle.

### Remaining startup decomposition (~40.4k)
Fixed harness floor **~31.9k** (system prompt 4.3k + tools 12.1k + deferred 15.5k — untouchable) + memory 20.8k + skills 3.3k. Of the controllable part: **project CLAUDE.md 9.3k = 45% of memory** (largest remaining lever), workflow.md 5.1k (second, riskier), everything else lean.

---

## Part 2 — New findings (this audit, 2026-07-07)

### F1 — CRITICAL: `.mcp.json` is git-tracked with live secrets
`git ls-files .mcp.json` confirms it is committed, containing a **GitHub bearer token**, **Context7 API key**, and **Playwright extension token** in plaintext. Action: rotate all three; replace values with `${ENV_VAR}` expansion (supported in `.mcp.json`); keep the file tracked secret-free. Independent of token-budget work; highest priority in this audit.

### F2 — `tooling-evaluations` skill is broken (dead route)
Track A (2026-07-04, `hashed-jingling-ocean.md`) extracted CLAUDE.md's tooling-evaluation sections to `.claude/skills/tooling-evaluations.md` — a **flat file**. Skills require `<name>/SKILL.md` (or `.claude/commands/<name>.md`). It does **not** appear in the session skill list; `CLAUDE.md § Tooling Evaluation & Migration` routes to a skill that cannot be invoked. The content is only reachable as a raw file read. Fix: move to `.claude/skills/tooling-evaluations/SKILL.md` with frontmatter; verify in a fresh session.

### F3 — CLAUDE.md routes to deliberately disabled skills (routing/enablement contradictions)
`CLAUDE.md § Skill & MCP Lookup` (mandatory per task step) references:
- `maui-current-apis` ("always"), `maui-data-binding`, `maui-shell-navigation`, `maui-performance`, `maui-rest-api` — all **project-scoped skills, disabled** via `settings.local.json skillOverrides` (which works for non-plugin skills — only `maui-unit-testing` is on).
- `dotnet-skills:*` (5 refs) — plugin **downloaded but disabled** (`dotnet-skills@dotnet-skills: false`); Task 09/10 explicitly evaluated it as mismatched (Testcontainers/Verify vs this SQLite/Moq project).
- `ddd-dotnet` (§ MCP & Skills + § Methodology Layering) — plugin **disabled**.
- `superpowers:test-driven-development` — **disabled by design** (testing.md authority note), yet still listed as the go-to for "Tests".

Every agent obeying the mandatory lookup hits dead or contradictory routes. Per-skill disposition proposal in Part 4 / R3.

### F4 — Downloaded-but-disabled inventory (disk + settings)
- **Project skills** (`.claude/skills/`): 21 `maui-*` skills; 19–20 off, `maui-unit-testing` on. Plus broken `tooling-evaluations.md` (F2).
- **User skill:** `myvocalist-coding` at `~/.claude/skills/` — project-specific content at user scope (leaks into every project; already a BACKLOG row 2026-07-05 — reaffirmed, still pending).
- **Plugins downloaded, disabled (project):** `dotnet-skills`, `ddd-dotnet`, `data-dotnet`, `bdd-dotnet` (dotnet-claude-code-skills), `ux@teslasoft-skills`, `frontend-design@claude-plugins-official`. Enabled: `superpowers`, `claude-code-setup`, `context-budget`.
- **Plugins downloaded, disabled (user):** `claude-md-management@claude-plugins-official` — the CLAUDE.md-specialist tooling Helder recalled. Candidate to enable temporarily for the CLAUDE.md restructure task (R5), then re-disable.
- Note: sandboxed `ls`/`Glob` cannot enumerate `~/.claude/plugins/cache` contents (returns empty); inventory above derives from `enabledPlugins` keys + explicit-path reads.

### F5 — MCP scope/governance drift
`settings.local.json` enables `context7`, `sequential-thinking`, `github`, `devexpress-maui` (sqlite disabled). But `CLAUDE.md § MCP Security Stance` approved list has **no `sequential-thinking`** (unapproved-but-enabled), and **GitHub MCP is marked "evaluation — re-evaluate before enabling"** yet is enabled. Playwright/mudblazor defined in `.mcp.json` but not in the local enabled list (OK). Action: either add sequential-thinking to the approved list with justification, or disable it; make the GitHub MCP go/no-go explicit. Verify actual tool-token cost via `/mcp` against the ~5k MCP budget rule.

### F6 — `Docs/Management` is NOT auto-loaded (Helder's concern — already answered by prior research)
`sprightly-launching-corbato.md` (in this repo, Management root) already established: only `CLAUDE.md` + `.claude/rules/*` load unconditionally; `Docs/**` costs tokens **only on explicit Read/non-ignored Glob**; the 43 MB scare is 86% one 38 MB debug log. Its four housekeeping recommendations are **still unexecuted**: move the 38 MB log out, add `.claudeignore` rule for debug captures, archive ~8 scratch-named root files, delete empty `ManagementByPass`. Additionally, the two substantive research docs themselves sit under scratch names at Management root (`hashed-jingling-ocean.md` = context-efficiency research incl. `paths:` frontmatter risk analysis; `sprightly-launching-corbato.md` = Docs footprint risk analysis) — rename/move into proper feature folders + `.sln` (R7).

### F7 — Claude's persistent memory: empty, and correctly so
The advertised memory directory does not exist on disk — nothing loads from it (the `/context` "Memory files 20.8k" = CLAUDE.md + rules only). Policy going forward: **project facts belong in the repo's documented pattern** (BACKLOG, task-logs, findings — versioned, shared, visible to subagents); Claude's memory is local-only, unversioned, invisible to subagents/other machines — use it only for cross-project user preferences, never as a second project memory. No action needed beyond this recorded policy.

### F8 — Agent briefs are not registered agent definitions
`.claude/agents/*.md` (orchestrator, implementor, spec/plan-reviewer, verifier) have **no YAML frontmatter** → they are prose briefings pasted into `general-purpose` dispatches, not agent types; nothing auto-loads skills into them. Fix (R4): add frontmatter (`name`, `description`, `tools`) + **preload `myvocalist-coding`** (docs: sub-agents § preload skills — injects full body at startup; safe here because the body is a ~400-tok routing table; do NOT preload big-bodied skills). **Not wheel-reinvention:** orchestrator.md already *extends* `superpowers:subagent-driven-development` (authoritative for the base loop); built-ins Explore/Plan cover generic exploration/planning; the project-specific reviewer/implementor protocols exist in no plugin. The change is registration + preload of existing content, not new machinery.

### F9 — Library/skill duplication (bounded)
`.claude/library/*` (19 files, 5,610 lines) is **on-demand only — zero startup cost**; duplication there is hygiene, not budget. Real overlap: `testing-reference.md` §Test Project Structure + generic Moq/ViewModel scaffolding duplicates the enabled `maui-unit-testing` skill (trim to pointers; keep real-SQLite/TestDbContextFactory/Tester-Builder/AC-traceability — no skill covers those). `mediatr-reference.md` (119 lines, generic) → delete; re-derive via Context7 when MediatR ships. All DevExpress/CRUD/MD3 library content is project-confirmed knowledge with no 3rd-party equivalent — keep.

---

## Part 3 — Answers to Helder's direct questions

1. **Evaluate routed skills rather than just deleting refs? Yes — disposition per skill, not blanket removal** (R3). Where a skill earns its place, enable + (for agents) preload; where the project already covers it better, remove the route. Blanket deletion would hide the one high-value candidate (`maui-current-apis`).
2. **Built-in agents / plugins instead of custom definitions?** Partially exist (superpowers SDD loop, built-in Explore/Plan) and are already the declared base. Adding frontmatter to the existing briefs is configuration on top of them — not reinvention (F8).
3. **CLAUDE.md one-pager?** Official guidance is **<200 lines** (verified 2026-07-04 research). Current: 300 lines / 9.3k tokens. A <200-line pass is feasible (MCP narrative → Claude-only skill; SDD-applicability essay, Tool Selection rationale, Methodology Layering → library/skill). The specialized tooling Helder recalls = `claude-md-management` plugin (downloaded, disabled) and built-in `/init`; the plugin is generic — useful as an audit assistant for the trim task, not as the author of record (Authorship rule still applies).
4. **Library duplication via myvocalist-coding?** Bounded — two files (F9); the rest is project-unique. And the skill map itself stays: it is the discovery path that keeps on-demand files findable.

---

## Part 4 — Consolidated final recommendations (priority order)

| # | Action | Why | Effort/Risk |
|---|--------|-----|-------------|
| **R1** | **Rotate GitHub/Context7/Playwright secrets; strip from `.mcp.json` via `${ENV}` expansion** (F1) | Committed live credentials | Small / none |
| **R2** | Fix `tooling-evaluations` → `.claude/skills/tooling-evaluations/SKILL.md`; restart-verify (F2) | Dead route from CLAUDE.md | Trivial / none |
| **R3** | Reconcile skill routing vs enablement (F3): **re-enable `maui-current-apis`** (guards against stale MAUI APIs; complements Context7); **remove** `dotnet-skills:*` + `superpowers:test-driven-development` rows; **decide** ddd-dotnet (enable at spec-time or drop the two refs); keep other `maui-*` off (Context7 covers, version-pinned) and drop their rows | Mandatory lookup table must not lie | Small / low — CLAUDE.md `amend:` |
| **R4** | Agent frontmatter + `myvocalist-coding` preload on the 5 briefs (F8) | Deterministic skill loading at agent start | Small / low; restart-verify |
| **R5** | CLAUDE.md <200-line restructure (deferred Task 12 item): narrative → Claude-only skill(s) + library; optionally enable `claude-md-management` temporarily as audit assistant | Largest remaining lever (~9.3k → ~4–5k, ×every agent) | Medium / medium — needs Helder review, `amend:` |
| **R6** | Record-correction pass: annotate BACKLOG rows + `findings-measurement.md` with measured (~8–11k) vs claimed; fix tasks.md criteria #1/#4; optional GATE-A-style post-probe (budget reset 07-07) | Docs currently overstate results | Small / none |
| **R7** | Docs housekeeping (from `sprightly-launching-corbato.md`, still pending): move 38 MB log, `.claudeignore` debug-capture rule, archive scratch root files, delete `ManagementByPass`, rename the 2 research docs into feature folders (+`.sln`) | Prevents accidental token sweeps; findability | Small / low |
| **R8** | MCP governance sync (F5): approve-or-disable `sequential-thinking`; explicit GitHub-MCP go/no-go; `/mcp` cost check. Plus, when convenient: move `myvocalist-coding` to project scope (existing BACKLOG row); trim `testing-reference.md` overlap; delete `mediatr-reference.md` (F9) | Governance consistency; hygiene | Small / low |

**Explicitly rejected:** `paths:` frontmatter scoping of rules files (worktree-silent-failure risk, issues #23569 et al. — per 2026-07-04 research, unchanged); bypass-folder scheme (rejected 2026-07-05 research); further micro-splitting of the small rules files (diminishing returns at 4% window usage — remaining value is per-subagent cost/latency, concentrated in R4/R5).
