# Sync plan.md / tasks.md / BACKLOG.md to the D3 decision (ValueConverter for persistence)

## Context

D3 was just recorded (2026-07-19) in `design.md`/`requirements.md`: persisted-string trimming
(REQ-TRIM-05/06/07) moves from per-Service-method `TrimForStorage`/`TrimForStorageOrNull` calls to
EF Core `ValueConverter<string,string>`/`ValueConverter<string?,string?>` instances configured per
name-like property in each `EntityTypeConfiguration` (Infra), still delegating to the same
`StringNormalization` helper. Search-query normalization (REQ-TRIM-01–04) is unchanged.

`plan.md`, `tasks.md`, and the BACKLOG.md row for this item were all written before D3 and still
describe the old "every Service Create/Update calls `TrimForStorage` directly" mechanism for the
persistence half. They need to be brought in line so a fresh implementor reading them doesn't build
the superseded design. This is documentation-only — no code changes, no plan/spec-reviewer subagent
needed (both target files are execution-detail docs restating an already-decided requirements/design
change, not a new spec).

## Files to update

### 1. `Docs/Management/DevCycleCraft/persisted-string-trimming/plan.md`
- Header note: mention D3 (2026-07-19) alongside the existing D1/D2 note.
- **Goal** line: currently says the helper is "wired into every search and Create/Update method" —
  narrow this to search only; persistence goal becomes "wired via EF Core ValueConverters in
  EntityTypeConfiguration."
- **Architecture** paragraph: remove "Create/Update methods replace ad-hoc `Trim()` with
  `TrimForStorage`/`TrimForStorageOrNull`" — replace with a note that persistence normalization is
  configured once per property via `ValueConverter`, not per call site.
- **Global Constraints**: REQ-TRIM-09 bullet needs the same search-only carve-out already added to
  requirements.md.
- **Task 2 (PersonService)**, **Task 3 (ArtistService)**, **Task 4 (VenueService/EventService)**,
  **Task 5 (SongService)**: each currently has explicit "Fix storage sites" steps calling
  `TrimForStorage`/`TrimForStorageOrNull` inline in Service methods, plus storage-focused Moq
  assertions. These need replacing/trimming down to search-only steps for these Service files, since
  storage normalization no longer happens there.
- **New Task** (insert before the renumbered Task 6, i.e. becomes Task 6, integration becomes Task 7):
  "Persistence: `ValueConverter`s in `EntityTypeConfiguration`" — one task covering all name-like
  properties (Person.Name, Artist.Name, Venue.Name, Event.Name, Song.Title, + optional fields like
  Person.Email) since these are Infra-layer config changes, not disjoint per-service work the way
  the search fixes are. Include: converter definitions delegating to `StringNormalization`, real-
  SQLite round-trip tests (per `testing.md` — no in-memory provider) proving normalized values are
  read back, and the D3 rationale as a code comment pointer to design.md § D3.
- **Task 6 → Task 7 (Integration verification + docs close-out)**: renumber; keep merge order (Task
  1 → search tasks 2–5 [P] → persistence task 6 → integration task 7); AC traceability matrix note
  for REQ-TRIM-05/06/07 now maps to the `ValueConverter` task/tests, not Tasks 2–5.
- **Self-review notes**: update the REQ-TRIM-05/06/07 coverage line to point at the new persistence
  task instead of "T2–T5 storage tests."

### 2. `Docs/Management/DevCycleCraft/persisted-string-trimming/tasks.md`
- Header note: add D3 pointer alongside existing D1/D2 reference.
- **Task 1**: unchanged (`StringNormalization` helper itself is still needed — the converter
  delegates to it).
- **Tasks 2–5**: trim `Produces`/`Demo` lines to search-only (drop "storage trimming" from Task 2's
  title/produces, drop `TrimForStorage`/`OrNull` mentions from Tasks 3–5's produces/demo lines).
- **New Task 6** — "Persistence: EF Core `ValueConverter`s for name-like properties" *(depends: Task
  1)*: Produces (converters in `EntityTypeConfiguration` for Person/Artist/Venue/Event/Song +
  optional fields, real-SQLite round-trip tests), Consumes (Task 1 helper), Risk (Medium — touches
  shared `AppDbContext`/`EntityTypeConfiguration` files, which are on the sequential-only file
  registry per `workflow.md § Sequential-only file registry` — must not run in the same wave as
  another task touching `AppDbContext.cs`), Files owned, Demo, Review lane (verifier subagent, D3
  rationale check).
- Renumber current Task 6 (Integration merge + docs close-out) to Task 7, depends on Tasks 2–6.
- Note in the header: Task 6 is **not** `[P]` with Tasks 2–5 if any of them also touch
  `EntityTypeConfiguration`/`AppDbContext.cs` (they shouldn't, per the current file lists — confirm
  and state this explicitly to avoid a wave collision).

### 3. `Docs/Management/BACKLOG.md` (row 88)
- Update the Gate text: currently "D1 (internal collapse: YES) + D2 (folder routing: approved)
  recorded 2026-07-15; plan.md/tasks.md written — awaiting Helder plan approval." Add: "D3
  (persistence mechanism: EF Core ValueConverter, not per-Service-method calls) recorded 2026-07-19"
  and note plan.md/tasks.md are being re-synced to D3 before Helder plan approval.

## Verification

- Re-read all three edited files after changes and confirm: no remaining reference to Service
  Create/Update methods calling `TrimForStorage`/`TrimForStorageOrNull` directly for persistence;
  Task numbering is consistent across plan.md and tasks.md; BACKLOG row reflects D3.
- No code/tests are touched — nothing to build or run. This stays within the "docs land on develop,
  spec-first" discipline; implementation still requires Helder's plan approval per the BACKLOG gate
  before any worktree/subagent work begins.
