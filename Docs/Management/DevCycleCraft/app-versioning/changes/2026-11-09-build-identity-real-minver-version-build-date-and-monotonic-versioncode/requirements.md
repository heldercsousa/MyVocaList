# Build identity — Requirements

> Change to the shipped `app-versioning` item (`../../design.md`, 2026-05-18). That spec stays as
> written; this folder records what changes.

## Problem

An installed build cannot say which build it is. On Helder's S23 the About page shows `v1.0.0`.
That is the SDK default, not the MinVer version the original spec promised. No build date is
shown anywhere. Android `versionCode` is `1` for every build, so Android cannot order builds.

Cause: `MyVocaList.csproj` assigns `ApplicationDisplayVersion` and `ApplicationVersion` from
`$(MinVerVersion)` at evaluation time. MinVer sets that property later, inside its own target, so
both values come out empty and the SDK defaults apply.

## Goal

Answer "which build is on this device?" at a glance. From that answer, find what the build
contains in the history the project already keeps.

## Acceptance criteria

- **REQ-BID-01** — WHEN the About page opens, THE app SHALL show the MinVer version of the build
  (for example `0.1.0-alpha.0.1056`), never the SDK default `1.0.0`.
- **REQ-BID-02** — WHEN the About page opens, THE app SHALL show the first 7 characters of the
  commit SHA the build was produced from.
- **REQ-BID-03** — WHEN the About page opens, THE app SHALL show the date and time the build ran,
  in the device's local time, formatted `yyyy-MM-dd HH:mm`.
- **REQ-BID-04** — THE Android version name (Settings → Apps → MyVocaList) SHALL equal the MinVer
  version shown on the About page.
- **REQ-BID-05** — THE Android `versionCode` SHALL equal the number of first-parent commits
  reachable from the built commit. A build of a later `develop` commit SHALL have a higher
  `versionCode` than a build of an earlier one.
- **REQ-BID-06** — IF git history is unavailable at build time, THEN the build SHALL fail with an
  error naming the missing value. It SHALL NOT fall back to a default. A silent default is how the
  `1.0.0` defect went unnoticed.
- **REQ-BID-07** — WHERE build details are enabled, WHEN the About page opens, THE app SHALL show
  the name of the git branch the build was produced from.
- **REQ-BID-08** — WHERE build details are enabled, WHEN the About page opens, THE app SHALL show
  the subjects of the last 10 first-parent commits of the built commit, newest first.
- **REQ-BID-09** — WHERE build details are enabled, IF the working tree had uncommitted changes
  when the build ran, THEN the About page SHALL show the warning
  `⚠ Built with uncommitted changes` and list the uncommitted paths. Otherwise it SHALL show no
  warning and no path list.
- **REQ-BID-10** — WHERE build details are disabled, THE About page SHALL show none of the content
  of REQ-BID-07, REQ-BID-08 or REQ-BID-09. Build details are enabled by default.

- **REQ-BID-11** — WHERE build details are enabled, WHEN the About page opens, THE app SHALL list
  every item whose frontmatter status was `🟡 In Progress` in the built commit. Each item shows
  its title and its gate text, or no gate line when the item has none, grouped under "In progress"
  with a count.
- **REQ-BID-12** — WHERE build details are enabled, WHEN the About page opens, THE app SHALL list
  every item closed (`✅`) in the calendar month of the build. Each item shows its title, grouped
  under "Done this month" with a count.
- **REQ-BID-13** — IF the status snapshot cannot be produced at build time, THEN the build SHALL
  still succeed, and the About page SHALL omit both status groups.

## Who build details are for

They are for Helder's manual testing, not for end users. They answer four questions from the
device: "does this develop build contain feature X?", "which worktree or task is this build
from?", "is the code in this build committed?" and "what is waiting for me to test, and how?".
The status groups add no maintenance: they read the item frontmatter the process already keeps.
They are only as accurate as that frontmatter. They are plain text, with no styling
beyond the page's existing typography.

## Change tracking — no new artifact

What a build contains comes from data the project already keeps, never from a new document:

- The 7-character SHA on the device (REQ-BID-02) identifies the exact commit.
- `git log <older-sha>..<newer-sha>` lists every change between two installed builds.
- Commit subjects already carry the conventional type and the item ID (`BUG-NNN`, task IDs).
  Those IDs lead to the item's `task-log.md` and to the BACKLOG and LEDGER rows.

## Out of scope

- A per-build changelog file, generated or hand-written.
- The `WhatsNewService` stub and the About page's hidden "What's New" section. They remain the
  future home for user-facing release notes. Those notes are release-level, never per build. Their
  source will be the `✅` items in the backlog archive closed since the previous release tag, so
  no new document is created for them.
- User-facing release notes in the build details. The commit subjects are raw developer text,
  shown only while build details are enabled.
- Hiding build details for store builds. When the first store release is prepared, the
  store-compliance item switches them off with the build property in `design.md` D6.
- The future of `Docs/Changelog/changelog.md`. It now mainly records rule amendments, as
  `CLAUDE.md § Amending These Rules` requires. Changing that is a separate decision.
- The tag discipline for `/sln-release`, including the version-bump prompt at feature start. Tags
  stay as optional milestones; build identity no longer depends on them.
- iOS. Only the Android target is built today.
