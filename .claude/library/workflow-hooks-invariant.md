# Development Workflow — Reference — Hook Enforcement Notes + SDD Invariant

> Section file split from `workflow-reference.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `workflow-reference.md`.

## Hook Enforcement Notes

The hooks in `.claude/settings.json` enforce specific rules from this document.

### Hook-enforced rules (automatic warnings or blocks)

| Hook | Trigger | Rule enforced |
|------|---------|---------------|
| `Stop` hook (`uncommitted-warning.py --stop`) | Turn ends with uncommitted changes | Rule 3 — Commit After Every Task. Shown to Helder only (`systemMessage`) |
| `PostToolUse` `Agent` (`uncommitted-warning.py --after-agent`) | A subagent returned and the main checkout is dirty | Rule 3 — the model is told to `/sln-commit` |
| `PreToolUse` `Read` (`read-slice-guard.py`) | Read of a file over 200 lines or 12 KB without a `limit` <= 250 | Token budget — read in slices; `BACKLOG.md` → `backlog_gen.py query` |
| `PreToolUse` `Bash\|PowerShell` (`bash-guard.py`) | `git worktree add` outside `.claude/worktrees/` (blocked); `cat`-like read of an over-budget file (note only) | Rule 2 — one worktree location; token budget |
| `PreToolUse` `Write\|Edit` (`delegate-reminder.py`) | Main agent edits a code file with no ITF declaration | Rule 2 — ITF or dispatch. Silent for subagents |
| `PostToolUse` (`worktree-base-check.py`) | Worktree created | Rule 2 — reports only a branch forked from `main` |
| `SessionStart` hook | New session begins | Hook health verification |
| `pre-commit` (`process_checks.py`) | Staged `Docs/`, `MyVocaList.sln` or `.xaml` | `.sln` registration incl. moves/deletes; `SafeAreaEdges="Container"` (or `"All"` on pages with text input) on ContentPages; `To Review` task-log entries need `### Changed files` |
| `commit-msg` (`process_checks.py`) | Every commit | `feat:`/`refactor:`/`perf:`/`amend:` need an `Item: <backlog-id>` trailer naming a registered item; docs/chore/merge/fix exempt |
| `pre-commit` (`ledger-row-guard.py`) | Staged `LEDGER.md` | Ledger row length (max 450 chars) |

**Writing a hook message (delivery rule, Claude Code hooks reference, checked 2026-11-15):** plain `print` output reaches the model only from `UserPromptSubmit` and `SessionStart` hooks. From any tool hook (`PreToolUse`, `PostToolUse`) or `Stop` hook it reaches nobody. Use `.claude/scripts/hook_out.py`: `additional_context` (the model sees it, costs tokens each time), `system_message` (Helder sees it), `deny` (blocks the tool call). The former TDD reminder on Services edits was removed on 2026-11-15: it never fired in a worktree, and `testing.md § TDD within SDD` carries the rule.

### Self-enforced rules (no hook — agent must apply consciously)

- Pre-dispatch validation checklist (Rule 2 / `agents/orchestrator.md`)
- DRY Onion task ordering (Rule 4)
- Single-writer rule for hotspot files (Rule 2)
- Spec freshness gate before dispatching a wave (`agents/orchestrator.md`)
- Multi-wave checkpoint every second wave (`agents/orchestrator.md`)
- Session-end spec update ritual (Rule 3 subsection)
- AC traceability matrix in task-log (Rule 5)
- E2E emulator gate before To Review (`agents/implementor.md`)

### Hook health verification

At the start of each session, verify that hooks are operational:
1. Check that `.claude/settings.json` exists and is valid JSON
2. Confirm the `Stop` hook is present and references the correct script
3. If a hook is misconfigured: fix it before dispatching any subagent

---

## SDD Invariant

> **Spec changes before code changes.**

- If a new requirement arises during implementation, update the spec first — then update the code.
- If code contradicts the spec, the code is wrong — the spec is not wrong.
- If the spec is incomplete, stop and clarify with Helder — do not improvise.
- A subagent that modifies behavior not described in the spec has violated this invariant.

This invariant applies to all agents (main and sub) at all times.

---
