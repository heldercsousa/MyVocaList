# Audit — Existing Code Suitable for Relocation to `MyVocaList.Extensions`

> Status: 💡 Pending — not yet executed. Scope defined here; findings get appended when the audit
> task runs. See `README.md` (same folder) for the placement criteria this audit applies.

## Goal

`StringNormalization` (now `StringExtensions` in `MyVocaList.Extensions.Strings`, per Task 6a / D4)
was written directly in `Services` and only relocated after the fact, once a verifier caught the
resulting layering violation. This audit looks for other existing helpers already living in the
wrong place — Services, Domain, or Infra — that have the same shape: pure, stateless, operating on
a BCL/third-party type, with no MyVocaList-specific business meaning.

## Scope

Sweep for candidates in, at minimum:
- `Services/**/*.cs` — static/private-static helper methods operating on `string`, collection
  types (`IEnumerable<T>`, `List<T>`), `DateTime`/`DateOnly`, or similar BCL types, with no call
  into a repository, no DI dependency, no domain-entity parameter.
- `Domain/**/*.cs` — any static utility class (not a value object, not an entity) that could be
  BCL-typed rather than domain-typed.
- `Infra/**/*.cs` — static helpers unrelated to EF Core configuration itself (e.g. generic parsing/
  formatting helpers that happen to live near a repository).

## Method

For each candidate found:
1. Apply the four criteria in `README.md § Placement criteria` — record which criteria it passes/
   fails.
2. If it passes all four: log it as a relocation candidate (file, method signature, current
   namespace, one-line description).
3. If it passes some but not all: note why it's disqualified (e.g. "touches `IPersonRepository` —
   not pure" or "operates on `Domain.Entities.Song` — not BCL-typed") so the exclusion reasoning is
   preserved, not just the inclusion list.

## Findings

*(To be filled in when the audit task executes — do not pre-populate with guesses.)*

## Out of scope

- Anything already flagged as business logic under `code-principles.md § Architecture Constraints`
  (constitutional, unamendable) is excluded regardless of how "utility"-shaped it looks.
- UI-layer (ViewModel/Page/XAML-code-behind) helpers are out of scope for this pass — this audit is
  Services/Domain/Infra only. A follow-up UI-layer pass can be scoped separately if this one finds
  the pattern is common enough to warrant it.
