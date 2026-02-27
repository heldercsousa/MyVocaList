# Changelog Command

Update the project changelog after every commit.

## File
`Docs/Changelog/changelog.md`

## Entry Format
```
- **MM/dd/yyyy** - <type> - <description>
```

## Types (must match commit types exactly)
| Type | Use for |
|------|---------|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `refactor` | Code restructure with no behavior change |
| `docs` | Documentation only |
| `perf` | Performance improvement |
| `test` | Test additions or changes |

## Steps

1. Open `Docs/Changelog/changelog.md`.
2. Add a new entry at the **top** of the list (most recent first).
3. Use today's date in `MM/dd/yyyy` format.
4. Match the type to the commit type used.
5. Keep the description concise (one sentence, what changed, not how).

## Example
```
- **02/26/2026** - feat - Add venue search with debounce and paged loading
- **02/25/2026** - fix - Prevent duplicate venue names on update
```

## Notes
- One entry per commit (not per file changed).
- If a commit covers multiple types, use the dominant type.
- Never remove existing entries.
