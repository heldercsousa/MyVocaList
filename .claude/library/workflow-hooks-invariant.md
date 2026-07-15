# Development Workflow — Reference — Hook Enforcement Notes + SDD Invariant

> Section file split from `workflow-reference.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `workflow-reference.md`.

## Hook Enforcement Notes

The hooks in `.claude/settings.json` enforce specific rules from this document.

### Hook-enforced rules (automatic warnings or blocks)

| Hook | Trigger | Rule enforced |
|------|---------|---------------|
| `Stop` hook | Session ends with uncommitted changes | Rule 3 — Commit After Every Task; also triggers Verifier dispatch reminder |
| `PostCompact` hook | Context compaction event | Session resume — re-read spec reminder |
| `PostToolUse` hook (Services files) | Edit to a Services/*.cs file | testing.md — TDD reminder for service changes |
| `SessionStart` hook | New session begins | Hook health verification |

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
