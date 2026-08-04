---
id: db-side-collation
title: "**DB-Side Collation — Remove All Normalized Columns**"
status: "✅ Done"
target: 2026-06
section: DevCycleCraft
kind: feature
closed: 2026-06
order: 100
goal: "all accent+case normalization handled by the database, never by C# code — normalized shadow columns dropped, collation-based unique indexes, no C#-side string normalization."
pointer: cross-cutting/db-side-collation/
---

# DB-Side Collation — Remove All Normalized Columns

Decided direction (Helder, 2026-06-01): all accent+case normalization must be handled by the
database, never by C# code. Dropped all `*Normalized` shadow columns (`Artist.NameNormalized`,
`Song.TitleNormalized`, `Person.FullNameNormalized`) — only the original display field survives.
UNIQUE indexes are defined on the original column with collation applied, so the DB enforces
uniqueness accent+case insensitively. All queries (search, duplicate checks, autocomplete) rely on
the DB collation — no C#-side normalization in service or repository code. Collation registration
is abstracted via EF Core configuration so a second DB provider requires only a provider-specific
collation name. `NOCASE_NOACCENT` is a custom SQLite collation registered via
`CollationInterceptor`.

> Migrated from the 2026-06 archive row (T12a Wave M, F-1a batch 3). Goal text is reworded from
> the archived Notes cell (verbatim text tripped the file-path-beyond-pointer banned pattern via
> `workflow.md`); meaning preserved.
