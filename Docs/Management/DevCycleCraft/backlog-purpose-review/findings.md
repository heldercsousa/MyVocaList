# BACKLOG.md Purpose Review — Measurement Findings (2026-07-15)

## Before

- `Docs/Management/BACKLOG.md`: **250 lines / 17,019 words** (`wc -l -w`).
- Token cost: **~56k tokens** per the Read tool's truncation report — the file exceeded the 25k single-Read cap and could NOT be read in one call (it took 8 paged reads during this restructure).

## After

- `Docs/Management/BACKLOG.md`: **123 lines / 2,840 words / 22,248 bytes** (~6–7k tokens).
- **Primary mechanical check PASSED:** a single `Read` of the slimmed BACKLOG.md returns the full file (123 lines) with no truncation.
- Reduction: **-83% words** (17,019 → 2,840), -51% lines (250 → 123).

## Where the mass went (nothing deleted — all moved)

| Artifact | Size |
|----------|------|
| `backlog-archive/BACKLOG-ARCHIVE-2026-03.md` | 19 lines / 210 words |
| `backlog-archive/BACKLOG-ARCHIVE-2026-04.md` | 17 lines / 145 words |
| `backlog-archive/BACKLOG-ARCHIVE-2026-05.md` | 24 lines / 358 words |
| `backlog-archive/BACKLOG-ARCHIVE-2026-06.md` | 60 lines / 1,320 words |
| `backlog-archive/BACKLOG-ARCHIVE-2026-07.md` | 46 lines / 1,069 words |
| `cross-cutting-log.md` (verbatim narratives of folder-less items) | 426 lines / 6,312 words |
| 26 feature-doc relocation targets (verbatim `## Moved from BACKLOG.md (2026-07-15)` blocks) | remainder |

Each archive file stays small (all well under the single-Read cap) and is append-only monthly history. Verbatim narratives (full original table rows) were relocated to 26 feature `task-log.md`/`findings.md` files plus the shared `cross-cutting-log.md`, preserving provenance under dated headings.
