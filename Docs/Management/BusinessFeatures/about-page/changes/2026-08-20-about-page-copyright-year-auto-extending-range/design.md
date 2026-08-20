# About Page Copyright Year — auto-extending range · Design

> **Change to shipped behavior.** Supersedes the copyright literal in `../../design.md § Page Structure`
> and extends `§ ViewModel — AboutViewModel`.

## Approach

The About page already has the exact pattern this needs: `Since` is a computed ViewModel property over
`AppConstants.FoundedYear`. The copyright line follows it — a second computed property replacing the
XAML literal.

```csharp
// AboutViewModel
public string Copyright => FormatCopyright(AppConstants.FoundedYear, DateTime.Now.Year);

internal static string FormatCopyright(int foundedYear, int currentYear) =>
    currentYear > foundedYear
        ? $"© {foundedYear}–{currentYear} Helder Sousa"
        : $"© {foundedYear} Helder Sousa";
```

`AboutPage.xaml` binds the copyright Label's `Text` to `Copyright` instead of the literal
`"© 2025 Helder Sousa"`. Typography, colors, ordering and every other Label are untouched.

## Decisions

- **Pure static formatter, not an injected clock.** `DateTime.Now.Year` is read in the property; the
  branching logic lives in `FormatCopyright`, which takes both years as parameters and is therefore
  fully unit-testable without `TimeProvider`, DI changes, or a fake clock. Injecting `TimeProvider`
  for one display string would add a registration and a constructor parameter with no second consumer
  — rejected as over-engineering for a Level-B concern.
- **`internal` + `InternalsVisibleTo`, not `public`.** The formatter is an implementation detail; the
  test project already sees `internal` members of the MAUI head project. If it does not, make the
  method `public static` rather than weakening the test.
- **En dash, not hyphen.** `2025–2026` is the correct typographic form for a year range and matches
  MD3 typography conventions. The file is UTF-8; the literal is safe.
- **`currentYear > foundedYear`, not `!=`.** Guards the skewed-clock edge case (AC-AB-05d validation)
  — a past year collapses to the single founding year instead of rendering an inverted range.
- **`Since` is untouched.** It is a separate property with separate meaning (AC-AB-05f).

## Files owned

- `MyVocaList/UI/ViewModels/AboutViewModel.cs` — add `Copyright` + `FormatCopyright`.
- `MyVocaList/UI/Pages/About/AboutPage.xaml` — bind the copyright Label.
- `MyVocaList.Tests/Unit/ViewModels/AboutViewModelTests.cs` — new or extended.

## Risk

Low — one display string on a read-only page. No navigation, persistence, or business rule touched.
The formatter is pure, so its behavior is fully pinned by tests.

## Testing

TDD Level **B** (`testing.md`) — pure formatting logic with clear edge cases. Write the tests first.

| Case | Input | Expected |
|------|-------|----------|
| Later year | (2025, 2026) | `© 2025–2026 Helder Sousa` |
| Much later year | (2025, 2099) | `© 2025–2099 Helder Sousa` |
| Same year | (2025, 2025) | `© 2025 Helder Sousa` |
| Skewed clock | (2025, 2024) | `© 2025 Helder Sousa` |

Also assert the `Copyright` property itself is non-empty and starts with `© 2025`, binding the
property to the constant without re-asserting the machine's clock.

## Verification

- Build 0 errors; full suite green.
- Manual E2E: About page License section shows `© 2025–2026 Helder Sousa`; `Since 2025` unchanged.
