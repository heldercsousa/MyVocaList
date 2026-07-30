# Handoff — `.sln` package + registration audit (2026-07-30)

> Session artifact. The parent feature row stays ✅ Done — this handoff records a maintenance pass
> against that rule, not a reopening of it. Nothing here blocks other work.

## Status: complete and committed. Two follow-ups open, both optional.

## Context manifest (read these, in order — nothing else)

| File | Why |
|------|-----|
| `.claude/library/constraints-reference.md § Visual Studio Solution (.sln)` | The amended rule — GUID counter single source, pattern, trap note, derive-next one-liner |
| `.claude/rules/constraints-registry.md § Visual Studio Solution (.sln)` | The routing entry that now forbids restating the counter |
| `Docs/Changelog/changelog.md` → entry `07/30/2026 - amend` | Full narrative of what was wrong and what changed |
| `Docs/Management/DevCycleCraft/spec-evolution-versioning/T13-proposed-diffs.md` → block 16 | Marked SUPERSEDED by this pass — read before applying the T13 bundle |
| `MyVocaList.sln` | The artifact that was repaired |

## What was done

Three commits on `develop`:

| Commit | Content |
|--------|---------|
| `4283a84` | `System.Security.Cryptography.Xml` 10.0.8 → **10.0.10** (five High-severity advisories); `.sln` structural repair — two folder blocks had been spliced onto the `EndGlobal` line, leaving `Global` unterminated, so **every `dotnet` CLI command on the solution failed**; six duplicate `bugs` folders renamed to clear MSB5004 |
| `62a670b` | 6 stale `SolutionItems` entries (files deleted from disk) removed; empty root folder `Contracts` dropped; 3 malformed GUIDs normalized |
| `5bf497e` | The `amend:` — GUID counter single-sourced; `Docs/` layout cleanup; five of the six `bugs` renames reverted |

Layout changes in `5bf497e`:
- `2026-07-21-inline-artist-create` and `backlog-scripts` were floating at **solution root** for want of a `NestedProjects` entry → nested under `artists-songs/changes` and `DevCycleCraft/backlog-first-registration`.
- `Docs/claude-rules` removed — it registered 2 of 6 `.claude/rules/*` files, contradicting the 2026-07-04 note that those are not `.sln`-registered.
- `Docs/Management/` root reduced to `BACKLOG.md` + `LEDGER.md`. `cross-cutting-log.md` → `cross-cutting/`; `EMULATOR_TEST_MASTER_LIST.md` + the frozen-UI chat dump → new `cross-cutting/emulator-testing/` (GUID `00D4`).
- Three agent-scratch files deleted per Helder (`binary-wibbling-jellyfish.md`, `hashed-jingling-ocean.md`, `sprightly-launching-corbato.md`).
- Remaining 10 unregistered `Docs/` files registered.

## Verification (re-run to confirm nothing regressed)

```
dotnet build MyVocaList.sln -t:restore --nologo -v:q        # expect 0 errors
dotnet list package --vulnerable --include-transitive        # expect 0 vulnerable, all 7 projects
```

Audit script (**not committed** — Helder declined the validator; recreate in the scratchpad if needed).
It reports four things: stale `SolutionItems`, empty solution folders, `Docs/` files on disk that are
unregistered, and registered items outside `Docs/`. At handoff time: **0 stale, 0 unregistered**.

Next free solution-folder GUID — derive it, never hand-count:

```
python -c "import re;print(max(re.findall(r'FA1234BC-0001-4000-8000-0000000000(..)',open('MyVocaList.sln',encoding='utf-8-sig').read())))"
```

## Open items

1. **MSB5004 root cause never isolated.** An earlier claim in this session — that MSBuild requires
   globally unique solution-folder names — is **wrong**: 14 folders named `changes` coexist fine.
   Pairwise and single-folder probes showed only `DevCycleCraft/autocomplete-component/bugs` trips
   MSB5004, and it does so **alone**. Ruled out: missing `ProjectSection`, malformed GUIDs, broken
   parent chain, duplicate GUIDs, duplicate nesting entries. That folder therefore still carries the
   qualified name `bugs (autocomplete-component)` as a workaround. If someone wants the plain name
   back, the real trigger has to be found first.
2. **`Docs/Management/DevCycleCraft/enforcement-automations/` is still an empty folder.** The
   registration HARD GATE remains prose-only, which is why this drift accumulated silently in the
   first place. Promoting the audit script to a committed validator is a one-file job and would
   close the loop. Helder chose "amend the rule files only" this pass — this is a deliberate
   deferral, not an oversight.

## Not done deliberately

- **`Docs/Management/LEDGER.md` was not touched.** It held uncommitted edits from a concurrent
  session (BUG-065 trace dispatch, 2026-07-30). Per the single-writer rule, no LEDGER row was added
  for this pass. Add one only if that session's edits have landed.
- **No BACKLOG row registered.** Rows are generated (`backlog_gen.py`), and regenerating would have
  rewritten `BACKLOG.md` while another session was mid-flight. This handoff is the record instead.
