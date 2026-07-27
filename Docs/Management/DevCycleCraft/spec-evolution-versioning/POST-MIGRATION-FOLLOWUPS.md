# Post-Migration Follow-ups (SPEC-EVO)

Durable reminders for issues deliberately deferred during the T12a folder migration
(Helder 2026-07-25: "leave the folder-collision issue, record a reminder, tackle later").
None of these block T12; they are cleanups to schedule after the migration branch merges.

## FUP-1 — Folder-collision semantic inversion (deferred by Helder 2026-07-25)

**What:** Several archived *Done* rows targeted a feature folder whose `README.md` was already
owned by a **different, still-live** item (`🟡 In Progress` / Pending). To avoid overwriting the
live item's README, the archived Done row was filed as a disambiguated `changes/<slug>/` sub-item
with `pointer:` kept on the existing `task-log.md` (REQ-SEV-27, nothing deleted).

**Cases (all committed on `feature/backlog-migration`):**
- `queue-management/` — live README = "Queue Entry Point Redesign"; archived core "Queue Management"
  filed as `changes/…queue-management-core-product/` (Wave J).
- `backup-restore/` — live README = "Backup Tier 2"; archived "Data Backup & Restore Tier 1+3"
  filed as `changes/…backup-restore-tier1-3/` (Wave J).
- `ui-form-validation-guide/` — live README = in-progress guide; archived "Form validation guide
  shipped" filed as `changes/2026-06-30-form-validation-guide-shipped/` (Wave N).

**Why it's a smell:** the *core/original* feature ends up nested as a `changes/` sub-item **under**
a README describing a later *redesign* of it — semantically inverted (the sub-item is arguably the
parent). It validates cleanly and loses no data, but the hierarchy reads backwards.

**Options to weigh later (not decided):**
1. Leave as-is (accept the inversion; it's only a doc-tree shape, validates fine).
2. Swap: make the archived core the folder `README.md`, refile the live redesign as the `changes/`
   sub-item (touches a live item's frontmatter — do under that item's own task, not here).
3. Introduce a distinct `history/` or `origin` marker so core-vs-redesign is explicit.

**Trigger:** revisit when the three live items above reach closeout, or during the
Spec-Evolution feature's own retrospective — whichever comes first.

## FUP-2 — bug-043 REQ-SEV-01 naming debt (pre-existing, tracked in tasks.md item vi)

`autocomplete-component/bugs/bug-043/` is lowercase, undated, unslugged (pre-scheme). Out of scope
for T12a; only a future `git mv` fixes the name. Recorded here so it isn't lost.

## FUP-3 — `.sln` GUID counter drift in constraints-registry.md (fix under T13)

`constraints-registry.md` still says last-used `.sln` GUID = `0041`; actual high-water after T12a
is `00D0`. Correct it in the T13 rules bundle.
