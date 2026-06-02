# S8.2 — Parallel Work Coordination

**Status:** Researched  
**Predecessor(s) ID:** S8

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; git worktrees, branch isolation, merge sequencing |

---

## Overview

Parallel work coordination is the discipline of running multiple AI agents simultaneously on the same repository without creating filesystem conflicts, semantic contradictions, or merge chaos. The infrastructure that makes this safe is **git worktrees** — a feature that has moved from advanced technique (2015–2024) to industry default for agent-driven development (2025–2026).

The critical insight: AI agents working on the same repository in the same directory will overwrite each other's files and produce silent data loss. A single working tree cannot support parallel execution. Git worktrees solve this by creating multiple working directories linked to the same repository — each agent gets its own checked-out branch, its own file system state, but all share a single `.git` object database. Multiple agents can work simultaneously without stepping on each other.

The scope of parallel coordination extends beyond filesystem isolation to include branch strategy, merge sequencing, semantic conflict detection, and the human review bottleneck that becomes the limiting factor as concurrency scales.

---

## S8.2.1 — Git Worktrees as the Isolation Primitive

### What is a git worktree?

A git worktree is a separate working directory linked to an existing Git repository. It is created with:

```bash
git worktree add ../agent-api -b feat/api-validation
```

This command creates a new directory (`../agent-api`) with its own checked-out branch (`feat/api-validation`), its own staged changes, its own working files — but all worktrees share the same `.git` object database. No duplication of history, no wasted disk on cloned repositories.

### Why worktrees, not clones?

An alternative approach — cloning the repository multiple times — works but carries significant overhead:

| Aspect | Clones | Worktrees |
|--------|--------|-----------|
| Object database | Duplicated per clone | Shared across all worktrees |
| Disk usage | 5 clones of a 2GB repo = 10GB | 5 worktrees = ~working dir size × 5 |
| Branch visibility | Pull/fetch required to see other clones' branches | Immediate; same `.git` |
| Merge simplicity | Standard git merge across clones | Standard git merge; branches are on same repo |

Worktrees eliminate duplication without sacrificing the isolation that agents need.

### The one-worktree-per-agent rule

The industry pattern established in 2025–2026 is simple: **one git worktree per agent task, one branch per worktree**. This is enforced in practice by:

- **Cursor 2.0** (October 2025) — Ships a "Parallel Agents" feature built directly on worktrees, supporting up to 8 simultaneous agents.
- **VS Code 1.107** (July 2025) — Added automatic worktree isolation for Copilot background agents.
- **Claude Code** — Supports `isolation: worktree` in subagent configuration; agents spawned with this flag receive their own worktree automatically.

The practical setup for three parallel agents:

```bash
git worktree add ../agent-api -b feat/api-validation
git worktree add ../agent-ui -b feat/checkout-component
git worktree add ../agent-tests -b fix/test-suite
```

Each directory is a complete, independent workspace. Agent A compiles in `../agent-api`, Agent B builds in `../agent-ui`, Agent C runs tests in `../agent-tests` — simultaneously, with no interference.

### Constraints and limitations

**Cannot check out the same branch in two worktrees:** Git enforces this by design — it prevents two directories from modifying the same HEAD and corrupting the index. If two agents must work on the same branch, one must use a detached HEAD state or a temporary branch.

**Build directories are per-worktree:** `node_modules`, `target/`, `venv/` are unique to each worktree. The first compilation in a new worktree is slow. Subsequent builds use the worktree's independent cache — a significant advantage over `git checkout`, which invalidates the cache every time you switch branches.

**Shared resources need coordination:** If the application writes to `~/.config/myapp/` or similar, two worktree instances running simultaneously will compete for the same file. Solutions:
- Parameterize the data directory by build: `APP_DATA_DIR=$PWD/.data npm start`
- Use separate databases per worktree (Docker Compose profiles work cleanly: `docker compose -p agent-api up` and `docker compose -p agent-ui up`)

**Git hooks must be robust:** Hooks in `.git/hooks/` are shared. If a hook assumes repo root via a hardcoded path, it breaks in linked worktrees. Use `git rev-parse --show-toplevel` to get the current worktree's root.

---

## S8.2.2 — Branch Isolation and Scoping

### Feature-based scoping

The most durable task scoping strategy is **feature-based**, not file-based. Instead of assigning "files in the auth module," assign "the authentication feature — schema, API endpoints, tests, and UI." The agent may touch many files, but it owns a coherent domain. Other agents have no reason to enter that domain.

This approach works because:
1. The agent understands the problem space holistically.
2. File-level coordination overhead is minimized.
3. When merge conflicts occur, they tend to be semantic (both agents solved the same problem differently) rather than purely syntactic.

### Additive vs. edit task distinction

Not all tasks parallelize equally:

**Additive tasks** (new features, new test files, new modules) run safely in parallel. Agents are building new things, not modifying existing work.

**Edit tasks** (refactoring shared code, migrating global patterns, updating shared configuration) must be sequenced. One agent finishes and merges before the next begins. If two agents both refactor the same utility function, merge conflicts are inevitable — and resolving them requires human judgment.

Confusing these two categories is the most common parallelism mistake. A team that tries to run refactoring tasks in parallel without sequencing will face quadratic merge complexity.

### Establishing interfaces as shared contracts

Before launching parallel agents, write down the interfaces they will share: API shapes, TypeScript types, function signatures, database schema changes. These contracts live in a single file on the main branch that all worktrees inherit. By establishing these before implementation begins, agents can work independently against stable interfaces.

Example: If multiple agents need to consume an API endpoint, define the endpoint schema in a shared `api-contracts.ts` file on `main` before agents branch off. Each agent sees that contract and implements against it without coordination.

---

## S8.2.3 — Merge Sequencing and Conflict Resolution

### The dependency-first merge strategy

Parallel worktrees eliminate filesystem conflicts during execution but do not eliminate semantic conflicts at merge time. Two agents that implement incompatible assumptions about a shared interface will produce branches that merge cleanly at the file level but break at runtime.

The established pattern is:

1. **Identify dependency order:** Which branches must be merged before others depend on their output?
   - If Agent A built a shared API client that Agents B, C, and D consume, merge A first.
   - If Agent B depends on Agent A's schema change, merge A before B.

2. **Merge dependency-first:** Start with the branches that have no downstream dependencies.

3. **Rebase downstream branches:** After merging a dependency, rebase downstream branches on the updated `main` before merging them.
   ```bash
   git checkout agent/feat-b
   git rebase main
   ```

4. **Verify semantic compatibility:** If semantic conflicts are found after merge (code compiles but behavior is wrong), use an orchestrator agent or human review to reconcile the incompatible assumptions.

### Orchestrator agent pattern

For teams running 5+ parallel agents, a designated orchestrator agent (not assigned to any feature) reviews all agent branches before merge, specifically looking for semantic contradictions. The orchestrator:
- Reads both branches
- Identifies where they encode incompatible assumptions
- Suggests reconciliation
- Acts faster and more thoroughly than manual review

This is distinct from per-branch review; its job is cross-branch consistency, not per-branch correctness.

---

## S8.2.4 — The 4-Agent Wave Cap and Review Bottleneck

### Wave-based parallelism

The MyVocaList workflow enforces a **4-agent wave cap** — never spawn more than 4 parallel subagents at any one time. This is not a technical limit of worktrees but a human-review constraint.

Why 4 agents?
- **Review capacity:** A human (or orchestrator agent) can meaningfully review 4 concurrent outputs before coordination overhead exceeds the productivity gain.
- **Rate limits:** Claude Code capacity scales with API tier:
  - Claude Pro: 2–3 agents comfortably before hitting rate limits
  - Claude Max: 4–5 agents without interruptions
  - API (direct): 5–7 agents before review bandwidth becomes the constraint
  - Beyond 7: You need an orchestrator agent managing the agents, not just yourself

- **Merge complexity:** With 4 agents, the number of possible merge orderings is manageable. With 10+, conflict resolution becomes a full-time job.

Work is dispatched in waves: spawn up to 4 agents, wait for all to complete or reach a checkpoint, then start the next wave.

### Review workflow

When 3–4 parallel agents finish:

1. **Review each branch independently.** Use a multi-file diff editor to inspect changes.
2. **Run the test suite against each branch.** Verify isolated correctness.
3. **Check for scope creep.** Did Agent A touch files it wasn't assigned? This is a sign that scoping was vague.
4. **Identify merge dependencies.** If Agent B's branch depends on Agent A's output, merge A first.
5. **Merge in dependency order.** Rebase downstream branches after each merge.
6. **Run full suite on the merged result.** Verify that the combination works.

### Kill criteria

Define kill criteria upfront — conditions under which an agent's task is aborted and reassigned:

- **Stuck for 3+ iterations on the same error without progress** → Abort, decompose the task further, reassign.
- **Touches files outside its assigned scope** → Abort immediately, review what changed, re-scope.
- **Output fails tests for 2+ cycles** → Abort, add failing test cases to the prompt, relaunch.

---

## S8.2.5 — Cross-Team Spec Consistency (S8.2.1 topic)

When multiple agents or teams work on interdependent specs simultaneously, the risk is that they encode contradictory decisions that only surface when implementation artifacts are combined. No current tooling fully resolves this — it remains a known tension in SDD.

Mitigations in active use:

**Shared project memory:** Tools like Colign maintain a "Project Memory" that stores domain rules, constraints, and technical decisions that are automatically injected into every spec change. This gives all working agents a common factual baseline.

**Single-writer rules for hotspot files:** Routes registries, shared configuration, global interfaces — files that every feature touches — must be designated as single-writer at any one time. If multiple specs need to modify the same hotspot, they are sequenced, not parallelized.

**Spec delta checkpoints:** Platforms like Specledger track every spec change with its intent. Checkpoints create alignment points where human review confirms that parallel spec changes are not contradictory before any implementation begins.

**Coordinator-reviewer pattern:** A designated orchestrator reviews all agent branches before merge, specifically looking for semantic contradictions. This is distinct from per-branch review.

---

## S8.2.6 — Practical Tooling and Orchestration

### Native agent support in IDEs and tools

The ecosystem now provides native worktree support:

| Tool | Native Support | Notes |
|------|---|---|
| Claude Code | `claude --worktree feature-auth` flag; subagent `isolation: worktree` | Auto-creates worktree + branch; cleanup automatic |
| Cursor 2.0+ | Built-in "Parallel Agents" feature | Up to 8 simultaneous agents |
| VS Code 1.107+ | Automatic worktree isolation for Copilot background agents | Transparent to the user |
| GitHub Copilot CLI | Developer-managed worktrees | You create; Copilot uses them |

### Orchestration frameworks

Several open-source and commercial platforms manage multi-agent worktree workflows:

**Shep** (open source, MIT) — Manages multiple agent sessions from a dashboard. Creates worktrees, handles commits, watches CI, retries on failure, opens PRs. Supports Claude Code, Cursor, Gemini CLI.

**Agent Orchestrator (ComposioHQ)** — Spawns parallel agents in worktrees, autonomously fixes CI failures and addresses review comments. Agent-agnostic and runtime-agnostic.

**Parallel Code** — Open-source Electron app. Dispatches agents, manages isolation, displays diffs for review, merges with one click. Keeps VS Code, Cursor, JetBrains as your IDE.

**Paragent** — Commercial SaaS. You describe features, it creates branches, runs agents in parallel, opens PRs. Cloud-based isolation without local worktree overhead.

**git-stint** — Manages session branches and worktrees automatically. Hooks track changes, auto-commits WIP checkpoints, detects file conflicts across sessions.

**Agent Teams (Claude Code)** — Experimental feature (as of 2026). Agents claim tasks from a shared task list, each in its own worktree, merge continuously to an integration branch. Lateral coordination, not hierarchical.

---

## S8.2.7 — Best Practices for Multi-Agent Workflows

### Before parallelizing, write contracts

Establish interfaces as shared contracts before agents branch:
- API endpoint schemas
- Database schema changes
- Shared TypeScript interfaces
- Function signatures

These live on `main` and all worktrees inherit them. Agents implement against stable contracts without needing to coordinate changes.

### Scope tasks to disjoint file sets

Every agent's task must own a non-overlapping set of files. If two agents both need to modify `config.ts`, that is a coordination problem to solve in task decomposition, not at merge time.

Check file overlap before launching: `git stint conflicts` or a custom script that compares file sets across branches.

### Start with 2 agents, move to 3

Developers and orchestrators who have run parallel workflows report this rhythm:
1. Run two agents on a single well-decomposed task.
2. Get your merge workflow right on that cycle.
3. Move to three agents when comfortable.
4. Rarely exceed four agents running in parallel.

Running ten agents is technically possible but operationally expensive — review capacity and merge complexity grow faster than agent count.

### Parameterize environment resources

Agents running in parallel may need separate:
- Databases (use Docker Compose profiles: `docker compose -p agent-api up`)
- Configuration files (set `APP_DATA_DIR=$PWD/.data` per worktree)
- Port assignments (configure dev server to use a port from an env var)

Shared resources (read-only seed databases, static file stores) can remain shared.

### Establish kill criteria and enforce them

Define conditions under which you abort an agent's task:
- Stuck on the same error for 3+ iterations
- Touching files outside its assigned scope
- Failing tests for 2+ retries

This prevents "zombie agents" that consume resources without making progress.

### Use an orchestrator for 5+ concurrent agents

Below 5 agents, manual monitoring and merging is feasible. Beyond 7, an orchestrator agent (or a tool like Shep/Agent Orchestrator) becomes necessary to avoid review bottlenecks.

---

## Sources

- [Parallel AI Coding Agents: The Git Worktrees Workflow Guide — The Agentic Blog](https://blog.appxlab.io/parallel-ai-coding-agents-git-worktrees/) (2026-04-01)
- [Worktree Isolation: Git Sandboxes for Parallel Agents — Agent Patterns](https://agentpatterns.ai/workflows/worktree-isolation/)
- [Git Worktree for Multi-Agent Dev: Setup Guide — Termdock](https://termdock.com/en/blog/git-worktree-multi-agent-setup) (2024-01-01)
- [Git Worktrees: How to Have Multiple AI Agents Working Simultaneously Without Stepping on Each Other — frr.dev](https://www.frr.dev/posts/git-worktrees-coding-agents-parallel/) (2026-02-16)
- [Running Parallel AI Agents on Isolated Git Worktrees for Small, Reviewable PRs — Fazm Blog](https://fazm.ai/blog/parallel-agents-isolated-worktrees-small-prs) (2025-12-01)
- [Git Worktree: The Infrastructure That Unlocks Agentic Development — htek.dev](https://htek.dev/articles/git-worktree-unlocks-agentic-development/) (2026-03-19)
- [git worktree: Multiple Working Directories Per Repo, and the Key to Parallel AI Agents — recca0120](https://recca0120.github.io/en/2026/04/14/git-worktree-parallel-work/) (2026-04-14)
- [Parallel AI Agents with Git Worktree - Multi-Session Guide — GitWorktree.org](https://www.gitworktree.org/ai-tools/parallel-agents) (2025-07-01)
- [Multi-Agent AI Coding Workflow: The Complete Guide — The Agentic Blog](https://blog.appxlab.io/2026/04/06/multi-agent-ai-coding-workflow/) (2026-04-06)
- [ComposioHQ/agent-orchestrator — GitHub](https://github.com/ComposioHQ/agent-orchestrator) (2026-02-13)
- [Agent Orchestrator: Managing Parallel AI Coding Agents with Git Worktrees — Starlog](https://starlog.is/articles/ai-agents/composiohq-agent-orchestrator) (2026-02-24)
- [Parallel Code — The open-source workspace for parallel AI coding](https://parallelcode.app/)
- [Paragent — AI Coding Agents That Ship Features in Parallel](https://paragent.app/)
- [git-stint — GitHub](https://github.com/rchaz/git-stint) (2026-02-27)
- [Claude Code Agent Teams: The Practical Guide to Multi-Agent Parallel Development — The Prompt Shelf](https://thepromptshelf.dev/blog/claude-code-agent-teams-guide-2026/) (2026-04-06)
- [Shep — Run multiple AI agents in parallel. Each in its own worktree.](https://shep.bot/)
- [shep-ai/shep — GitHub](https://github.com/shep-ai/shep) (2026-02-01)
