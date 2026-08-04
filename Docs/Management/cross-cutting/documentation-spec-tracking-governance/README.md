---
id: documentation-spec-tracking-governance
title: **Documentation & spec-tracking governance — where docs live**
status: 🔵 Superseded
target: 2026-07-11
section: DevCycleCraft
goal: "standing rule/mechanism so docs never strand on feature branches (interim rule: docs commit to `develop`)."
pointer: cross-cutting/documentation-spec-tracking-governance/
closed: 2026-08
order: 10
kind: feature
---

# Documentation & spec-tracking governance — where docs live

Migrated from the pre-migration BACKLOG.md row (no prior spec folder).

Back-reference: `Docs/Management/cross-cutting-log.md` (retained; not migrated).

> **Closed 🔵 Superseded [2026-08-04] -- delivered elsewhere (Helder).** The standing rule this
> item asked for now exists, so the item has no remaining scope:
>
> - **Where docs live** -- `CLAUDE.md` § Docs/ Folder Layout defines the canonical per-item
>   folder layout, and item frontmatter now generates the BACKLOG rows.
> - **Docs never strand on feature branches** -- `workflow.md` Rule 2 carries *"Docs land on
>   develop"* as a HARD RULE, with `/sln-docs-sync` as the flush mechanism.
> - **Spec tracking** -- the shipped **Spec Evolution, Versioning & Feature-Folder
>   Organization** feature (`DevCycleCraft/spec-evolution-versioning/`) made shipped specs
>   immutable and gave changes and bugs their own nested folders.
>
> What was an interim rule when this row was written is now enforced, including by a
> pre-commit gate. Nothing actionable remains.
