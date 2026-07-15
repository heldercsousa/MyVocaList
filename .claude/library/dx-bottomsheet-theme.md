# DevExpress MAUI Component Patterns — BottomSheet + theme token usage

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## BottomSheet — confirmed in codebase

See `.claude/rules/dialogs-validation.md` for full patterns.

Key properties:
- `AllowedState="HalfExpanded"` — locks to half expanded
- `HalfExpandedRatio="0.4"` — 40% of screen height (adjust per content, 0.28 for confirm sheets)
- `IsModal="True"` — dims background
- `ShowGrabber="True"` — shows drag handle
- `AllowDismiss="True"` — user can swipe down to dismiss
- `CornerRadius="28"` — rounded top corners
- `StateChanged` event — sync state back to ViewModel

## Theme Token Usage

Two ways to reference color tokens in XAML:

| Method | When to use |
|--------|-------------|
| `{StaticResource Primary}` | Standard layout properties (BackgroundColor, TextColor, etc.) |
| `{dx:ThemeColor Primary}` | DevExpress-specific properties (CheckedCheckBoxColor, BorderColor on DXBorder) |

Token names are identical — only the binding syntax differs.
