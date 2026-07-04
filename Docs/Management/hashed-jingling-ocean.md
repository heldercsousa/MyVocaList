# Context Efficiency: Path-Scoped Rules + CLAUDE.md Trim

## Context

A fresh session (immediately after `/context`, no prior work) showed 55.8k tokens
consumed, with ~34k of that from "Memory files": `CLAUDE.md` (6.8k) plus all seven
`.claude/rules/*.md` files (`workflow.md` 9.9k, `testing.md` 8.3k, plus five smaller
files) loaded in full, unconditionally, every session. Since this project's workflow
relies heavily on subagent dispatch (each subagent is a fresh session that re-pays this
34k cost), the user asked whether Claude Code supports loading this content only when
actually relevant, rather than always.

I verified against the official docs (`code.claude.com/docs/en/memory`) and the
`anthropics/claude-code` GitHub issue tracker — not just the first research pass —
because the fix hinges on a specific, falsifiable claim (a `paths:` frontmatter
feature) that turned out to have real reliability problems worth knowing about
before committing to it.

## What's confirmed

1. **Root cause**: `.claude/rules/*.md` files without `paths:` frontmatter load
   unconditionally "with the same priority as `.claude/CLAUDE.md`" — this is
   documented, intended behavior, not a bug or a hook side-effect. No hook or
   `@import` is involved (verified: none of the project's rules use `@import`, and
   `SessionStart` hook only does health verification).
2. **The lazy-loading primitive exists**: `paths:` YAML frontmatter scopes a rule
   file so it only enters context "when Claude works with files matching the
   specified patterns" (glob syntax, brace expansion supported).
3. **CLAUDE.md itself is already oversized** per official guidance ("target under
   200 lines... longer files consume more context and reduce adherence"). This
   project's `CLAUDE.md` is well past that.
4. **Skills are a separate, more reliable on-demand mechanism** — they load only
   when invoked or when Claude judges them relevant to the prompt, independent of
   file-path matching.

## What's risky — found via GitHub issues, not the first research pass

Multiple **open** issues against `paths:` frontmatter specifically:
- [#23478](https://github.com/anthropics/claude-code/issues/23478): path rules
  trigger on Read, not on Write/Create.
- [#22170](https://github.com/anthropics/claude-code/issues/22170) /
  [#21858](https://github.com/anthropics/claude-code/issues/21858) /
  [#16853](https://github.com/anthropics/claude-code/issues/16853): `paths:`
  frontmatter silently not loading at all in various configs.
- [#17204](https://github.com/anthropics/claude-code/issues/17204): documented
  `paths:` YAML format doesn't work in some cases; undocumented `globs:` does.
- **[#23569](https://github.com/anthropics/claude-code/issues/23569): path-conditional
  rules are ignored when loaded via git worktree resolution** — this is the one
  that matters most here, because `workflow.md` mandates git worktrees for every
  parallel subagent wave. If we scope `workflow.md` or `testing.md` by path and
  the mechanism silently fails inside a worktree, the TDD/spec-first discipline
  those files encode would lapse **with no error, no warning** — worse than the
  status quo of just paying the token cost.

Given this project's own governance culture (validate before applying broadly,
document rationale before amending rules — see `CLAUDE.md § Amending These Rules`),
rolling this out to the safety-critical files without validation would violate the
project's own stated principles.

## Recommended approach — two independent tracks

### Track A — Move rarely-used CLAUDE.md content into a Skill (low risk, do first)

Skills don't have the worktree/Read-vs-Write bugs above — different trigger
mechanism (explicit invocation or prompt-relevance judgment, not file-path
matching). Candidates to extract from `CLAUDE.md` (all reference/evaluation
material consulted rarely, not "always apply" rules):
- `### Tessl Registry (Evaluation)`
- `### sdd-mcp (Evaluation)`
- `### Migration Path (if Spec Kit adoption becomes warranted)`
- `### Complementary Tooling — Cursor (optional, for human review sessions)`

Fold these into a new on-demand skill (e.g. `tooling-evaluations`) or an existing
one, invoked when the user asks about tool migration/evaluation. Leaves `CLAUDE.md`
closer to the official <200-line guidance without touching any behavior-critical
rule.

### Track B — Pilot `paths:` frontmatter on low-consequence rule files only

Do **not** path-scope `workflow.md` or `testing.md` yet — they are the two largest
files but also the most safety-critical, and the exact failure modes reported
(#23478, #23569) hit this project's dominant execution pattern (worktree-based
subagents) directly.

Pilot on files where a silent misfire is low-consequence:
- `mediatr-patterns.md` → `paths: ["MyVocaList.Services/**/*.cs"]` (currently
  reference-only, not yet in active use)
- `component-change-governance.md` → `paths: ["**/UI/Components/**"]`
- `bug-tracking.md` → `paths: ["Docs/Management/**/*.md"]`

**Validation before trusting the pilot:**
1. Add frontmatter to the three pilot files.
2. Fresh session, run `/context` — confirm these three files are *not* counted.
3. Have Claude `Read` a matching file, then run `/memory` — confirm the rule now
   shows as loaded.
4. Repeat step 3 inside an actual worktree-dispatched subagent (the project's
   normal execution mode) — this is the specific case #23569 reports as broken,
   so it must be checked here, not assumed from the docs.
5. Optionally wire the `InstructionsLoaded` hook (documented specifically for
   this kind of debugging) for a session or two to get ground truth instead of
   relying on `/memory` snapshots.

Only if step 4 passes cleanly should `code-principles.md` and
`constraints-registry.md` be considered for broader path-scoping
(`**/*.cs`, `**/*.xaml`). `workflow.md` and `testing.md` stay unconditional until
#23569 is confirmed fixed in a released version, or the `InstructionsLoaded` hook
shows reliable loading across several real worktree sessions.

## Process requirement

Per `CLAUDE.md § Amending These Rules`, any change to `.claude/rules/*.md`
requires: (1) documenting what's wrong with the current state and why (this doc),
(2) noting backward-compatibility impact (none — frontmatter addition is additive),
(3) commit with `amend:` prefix and rationale, (4) a `Docs/Changelog/changelog.md`
entry with old rule / new rule / effective date.

## Verification

- Token comparison: run `/context` on a fresh session before and after Track A/B
  changes; expect CLAUDE.md down by ~1.5–2k (Track A) and 3 pilot rule files (~4k
  combined) off the unconditional load (Track B), pending validation passing.
- Functional check: confirm the extracted CLAUDE.md sections are still reachable
  by asking Claude the relevant question and confirming the new skill fires.
- Worktree check (critical, see Track B step 4): must be done before trusting any
  pilot file's path-scoping in a real subagent wave.
