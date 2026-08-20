# About Page License Text — MIT · Design

> **Change to shipped behavior.** Supersedes the License block of `../../design.md § layout tree`.

## Approach

Two literal string swaps inside the existing License block of
`MyVocaList/UI/Pages/About/AboutPage.xaml`. No new bindings, no ViewModel change, no
service, no DI registration, no resource keys, no layout change.

```
├── "License"  (Label.Large, section header)
│   ├── "MIT License"  (Body.Medium)                    ← AC-AB-05a  (was "CC BY-NC-ND 4.0")
│   ├── "Free to use, modify, and distribute."
│   │    (Body.Small, muted)                            ← AC-AB-05a  (was "Free for personal
│   │                                                       and non-commercial use. No derivatives.")
│   └── "© 2025 Helder Sousa"  (Body.Small)             ← unchanged
```

## Decisions

- **Literal, not bound.** The license changes roughly once in a project's life; a
  `IWhatsNewService`-style indirection would add a layer with no second consumer.
- **"MIT License", not "MIT".** Matches the `LICENSE` file's own title line.

## Files owned

- `MyVocaList/UI/Pages/About/AboutPage.xaml` — License block only.

## Risk

Low — presentation-only string change on one shipped page, no logic path touched.

## Verification

- Build 0 errors.
- Manual E2E: open flyout → About; License section reads "MIT License" /
  "Free to use, modify, and distribute." / "© 2025 Helder Sousa".
- `grep -rn "CC BY-NC-ND\|NonCommercial" --include=*.xaml --include=*.cs` returns nothing.
