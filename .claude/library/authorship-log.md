# Authorship & Approval Log — `.claude/rules/`

> Provenance record for the project's rules files. Human governance history, not agent guidance —
> routed out of the always-loaded rules files 2026-08-24 (doctor pass) so it costs no session context.
> `CLAUDE.md § Continuous Enhancement — Authorship` is the governing requirement.

---

## `bug-tracking.md`

Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Approval is provisional: these definitions are hooked to the current feature-development/spec-update approach and MUST be revisited when the **Spec Evolution, Versioning & Feature-Folder Organization** feature (BACKLOG 2026-07-09) lands (e.g. bug-fix→spec-version binding). Full content preserved in `library/bug-tracking-reference.md`.

---

## `code-principles.md`

Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Full content preserved in `library/code-style-reference.md`.

---

## `component-change-governance.md`

Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Full content preserved in `library/component-safety-gate.md`.

---

## `constraints-registry.md`

Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Full content preserved in `library/constraints-reference.md`.

---

## `testing.md`

Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Full content preserved in `library/testing-reference.md` (+ Stryker/FsCheck files).

---

## `workflow.md`

Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Approval is provisional where content is hooked to the current spec-update approach (SDD Invariant, Rule 1 spec-gap handling, Rule 3 Session-End Spec Update Ritual): these sections MUST be revisited when the **Spec Evolution, Versioning & Feature-Folder Organization** feature (BACKLOG 2026-07-09) defines the immutable-spec/delta-change pattern.

---

---

## 2026-08-25 amendment — `e6a1463b`

**Human-reviewed and APPROVED by Helder 2026-08-25.** Clears the outstanding review gate that commit
`e6a1463b` recorded against itself ("STILL REQUIRES HELDER'S HUMAN REVIEW per `CLAUDE.md §
Continuous Enhancement — Authorship`").

Scope of the amendment: consolidated the per-file authorship notes into this file; de-pinned exact
stack versions from `CLAUDE.md` in favour of "versions come from the `.csproj`"; routed the
`backlog_gen` verb table to `library/backlog-generator.md` and the ITF `C0–C8` table to
`workflow-rule-2.md` (keeping a never-miss summary inline); added the `devexpress-maui` MCP entry and
the 2026-08-24 `dotnet-skills` scoping (4 enabled / 32 disabled, `csharp-nullable-reference-types`
deliberately off as contrary to the project's lenient-nullable stance).

**Provenance caveat, preserved deliberately:** the change was found uncommitted in the working tree,
carried across sessions unattributed. It was earlier attributed to Helder without evidence — authorship
of uncommitted work is not determinable from git. It was reviewed line-by-line before committing, and
has now been reviewed and approved by Helder. No rule was weakened; every HARD RULE, the four spec
gates, ITF's nine conditions and the regression-test mandate remain inline or explicitly routed.

