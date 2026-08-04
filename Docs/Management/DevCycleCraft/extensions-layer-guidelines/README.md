---
id: extensions-layer-guidelines
title: "**`MyVocaList.Extensions` layer guidelines — placement criteria + rules-file promotion**"
status: "💡 Pending"
target: 2026-07-19
section: DevCycleCraft
kind: feature
order: 110
goal: "formalize when a helper belongs in the new dependency-free `MyVocaList.Extensions` project (created by D4 above) vs. Services/Domain, beyond the one worked example."
gate: "`MyVocaList.Extensions` must exist first (Task 6a)."
pointer: DevCycleCraft/extensions-layer-guidelines/
---

**Notes overflow (transcribed from the pre-migration BACKLOG row):** Promote into `code-principles.md` once stable.

# `MyVocaList.Extensions` — Placement Guidelines

> Status: 💡 Pending — not yet written as a formal rules-file amendment; this document captures the
> working criteria until it graduates into `.claude/rules/code-principles.md` or its own
> `.claude/library/` reference file (per `CLAUDE.md § Continuous Enhancement`).
> Origin: `DevCycleCraft/persisted-string-trimming/` Task 6a (D4, 2026-07-19) created the first
> occupant of this layer — `StringNormalization`'s extension methods, relocated out of `Services`
> after a verifier flagged an `Infra→Services` layering violation. See
> `persisted-string-trimming/design.md § Decision points → D4` for the full incident and rationale.

## Why this layer exists

`MyVocaList.Extensions` is a **dependency-free leaf project** — zero `ProjectReference` to
`Domain`, `Infra`, `Services`, or `Contracts`. It sits structurally *below* the onion (parallel to
or under `Domain`), so **every** layer, including `Infra`, may reference it without inverting the
DRY Onion order (`Domain → Infra → Services → UI`, `workflow.md` Rule 4). Before this project
existed, a pure, stateless string utility had nowhere correct to live: placing it in `Services`
(where it was originally written, Task 1) forced `Infra` to add a reference *into* `Services` just
to reach it — directionally backwards, even though not circular.

## Placement criteria — when a helper belongs here

A method is a `MyVocaList.Extensions` candidate when **all** of the following hold:

1. **Pure and stateless** — no I/O, no configuration, no `DateTime.Now`/randomness, no shared
   mutable state, no DI dependency. Same inputs always produce the same outputs.
2. **Operates on a type this solution doesn't own** — a BCL type (`string`, `IEnumerable<T>`,
   `DateTime`, etc.) or a third-party library type, not a MyVocaList `Domain` entity/value object.
   (Extension methods on your own owned types are usually better as regular instance methods —
   Microsoft's Framework Design Guidelines: prefer extension methods specifically to avoid
   dependencies the owning type shouldn't have, not as a general C# style choice.)
3. **Not business logic** — no domain rule, no validation with exceptions, no anything that could
   plausibly need to differ by feature/entity/user-role. Whitespace collapsing, for example, is a
   universal data-integrity operation with no MyVocaList-specific variability — that is what makes
   it eligible; a method like `Song.IsEligibleForQueue()` is not, no matter how "utility"-shaped it
   looks, because eligibility rules are domain-specific and could change.
4. **Reusable beyond this solution's domain** — if you'd expect the same method to be useful in an
   unrelated .NET solution with no karaoke/queue domain knowledge, it's a good fit. If understanding
   it requires knowing what a "singer" or "queue round" is, it belongs elsewhere.

If a helper meets 1–2 but fails 3 or 4 (pure, BCL-typed, but domain-specific), it likely belongs as
a `private static` local helper inside the Service that uses it, or a `Domain`-layer value object
method — not this project. Anything failing 1 (has side effects/dependencies) **never** belongs
here — see the Services-injection guidance in `code-style-reference.md § DI Registration
Conventions` instead.

## Organization inside the project

One project, organized **by namespace per concern area**, to avoid both extremes: a proliferation
of near-empty micro-projects, and an unfocused "Common"/"Utils" dumping ground with no organizing
principle. Current and anticipated namespaces:

- `MyVocaList.Extensions.Strings` — `StringExtensions` (`NormalizeSearchQuery`, `TrimForStorage`,
  `TrimForStorageOrNull` — the D4 precedent).
- Future namespaces get added the same way, one per concern, only when a second candidate actually
  qualifies (see the companion audit task, `audit-candidates.md`) — do not pre-create empty
  namespace folders speculatively.

## Naming

Project: `MyVocaList.Extensions` (mirrors the recognized `Microsoft.Extensions.*` ecosystem
convention — immediately legible to any .NET developer, no jargon). Class names: `{Type}Extensions`
(e.g. `StringExtensions`), one class per extended type per namespace, matching the BCL's own
`Enumerable`/`String` extension-class conventions.

## Extraction path (not yet triggered)

Because the project has zero MyVocaList-specific dependencies by construction, promoting it to a
standalone repo + NuGet package later — if a second, unrelated solution needs it — is near-zero
rework: copy the project folder, add a `.sln`/CI, publish. Not done proactively; only one solution
currently consumes it, and standing up a separate repo/feed/CI pipeline for a handful of methods
would be premature relative to actual present need. Revisit this if/when a second solution needs
any of these extensions.

## Open item

This document is the *working* version of the guideline. Promoting it into
`.claude/rules/code-principles.md` (a new `§ Extension/Utility Layer Placement` section, following
the existing routing-table pattern) is tracked as a follow-up in
`Docs/Management/BACKLOG.md` — see the Dev Cycle Craft row for this feature.
