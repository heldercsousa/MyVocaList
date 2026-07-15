# M3 Components — AppBar Patterns

---

## ⚠️ PRE-IMPLEMENTATION AUDIT CHECKLIST (REFER TO `devexpress-patterns.md`)

**This file documents custom components (AppBar, ListItem, EmptyState, FloatingToolbar) that were implemented because DevExpress had no equivalent at the time.**

**Before implementing a new component OR adding styles for a component here:**

1. **Check `devexpress-patterns.md § PRE-IMPLEMENTATION AUDIT CHECKLIST` first** — it has the complete pre-implementation workflow.
2. **Ask:** Does DevExpress now have a newer equivalent? If yes, prefer it over custom code.
3. **Example:** Before the 2026 DevExpress release, there was no `FilterChipGroup` → we would have built a custom chip component. Now DX has it → use `dxe:FilterChipGroup`, not custom chips.

**This file is the registry of "what we built custom" — not the first place to check before coding.** The first place is always `devexpress-patterns.md`.

---

## ⚠️ Styles Must Exist Before Use

Before referencing any style key in XAML (e.g. `Style="{StaticResource BottomSheetTitle}"`):

1. **Verify** the key exists in `MaterialStyles.xaml` or `MaterialColors.xaml` — grep for it.
2. **If missing:** add the style definition before (or in the same commit as) the XAML reference.
3. **Never** leave a `StaticResource` reference with no corresponding definition — it is a runtime crash, not a build error.
4. **Ownership:** BottomSheet-specific Label/Button styles belong in `MaterialStyles.xaml` near the `BottomSheetDestructiveAction` / `BottomSheetCancelAction` block.

**Previously missing style now defined:**
- `BottomSheetTitle` — `TargetType="Label"`, titleLarge: 22sp RobotoRegular, `OnSurface` color, `Padding="24,16,24,8"`. Used as the title label inside `dx:BottomSheet` content. Added 2026-06-13 after being referenced in XAML before it existed.

---

## MD3 Terminology Conventions

### "Body" means a structural slot — not text content
In MD3 component anatomy, **"body"** refers to a **structural container or slot**:
- Bottom sheet: Container → Header → **Body** (entire scrollable content area)
- Dialog: Container → Header → **Body** (supporting text + actions area)

**Never** name a BindableProperty `Body` for text content — it collides with MD3's container/slot meaning.

**Use `SupportingText` instead** — MD3's cross-component term for secondary descriptive text. Consistent across Lists, Cards, Chips, Dialogs, and Empty state (supporting text slot). Our existing `ListItem.SupportingText` already follows this.

### Complete MD3 type scale — MAUI StyleClass keys

| MD3 role | Style class | Family | sp | Weight |
|---|---|---|---|---|
| Display Large | `Display.Large` | RobotoRegular | 57 | Regular |
| Headline Large | `Headline.Large` | RobotoRegular | 32 | Regular |
| Title Large | `Title.Large` | RobotoRegular | 22 | Regular |
| Title Medium | `Title.Medium` | RobotoMedium | 16 | Medium |
| Body Large | `Body.Large` | RobotoRegular | 16 | Regular |
| Body Medium | `Body.Medium` | RobotoRegular | 14 | Regular |
| Body Small | `Body.Small` | RobotoRegular | 12 | Regular |
| Label Large | `Label.Large` | RobotoMedium | 14 | Medium |
| Label Medium | `Label.Medium` | RobotoMedium | 12 | Medium |
| Label Small | `Label.Small` | RobotoMedium | 11 | Medium |

> All 10 entries are defined in `MaterialStyles.xaml` as `StyleClass` entries. `Label.Small` weight is Medium per MD3 spec.

### Anatomy slot terms used in this codebase

| MD3 anatomy term | Used for |
|---|---|
| `Headline` | Primary text in list items, empty states, dialogs |
| `SupportingText` | Secondary/descriptive text (replaces "Body" for text) |
| `Illustration` | Icon or image in empty states |
| `LeadingContent` | Left slot in list items |
| `TrailingContent` | Right slot in list items |
| `Overline` | Label above headline in list items |

---

> **This file is now an index** (split 2026-07-14 for token-scoped subagent reads). Read ONLY the section file(s) your task needs — never all of them. Inbound `§` references resolve via the table below.

| Section file | Covers |
|---|---|
| `m3-appbars.md` | M3 Top App Bars (Small, Search, standalone) + shared base class + tokens + files — SmallAppBar, SearchAppBar, AppBarBase, token mapping |
| `m3-lists.md` | M3 Lists — list item component — ListItem anatomy + variants |
| `m3-floating-toolbar.md` | M3 Floating Toolbar — FloatingToolbar spec |
| `m3-emptystate-chips.md` | M3 Empty State + Filter Chip — EmptyState, FilterChip |

---

## Search — autocomplete (cross-ref stub)

Responsive autocomplete rule (compact → full-screen Search View / larger → docked exposed dropdown),
component invariants, and the no-match `Add "<typed text>"` affordance:
**`ux-patterns.md § Autocomplete — Responsive Full-Screen Expansion (MD3)`**.
Components: `AutocompleteField` (docked) + `AutocompleteMobileField` (Search View) — governed, four-gate rule applies.
