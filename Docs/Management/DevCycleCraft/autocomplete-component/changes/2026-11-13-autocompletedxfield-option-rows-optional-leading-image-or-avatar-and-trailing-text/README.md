---
id: autocompletedxfield-option-rows-optional-leading-image-or-avatar-and-trailing-text
title: "AutocompleteDXField option rows: optional leading image or avatar and trailing text"
status: 🟡 In Progress
target: 2026-11-13
section: DevCycleCraft
parent: autocomplete-component
goal: "An option may carry a leading image (56dp square) or avatar (40dp circle) and a short trailing text; options without them render as today. Needed by the Deezer artwork item. Gate: four-gate component record in this folder; device check that the Person form menu is unchanged."
kind: change
---

# AutocompleteDXField option rows: optional leading image or avatar and trailing text

An option may carry a leading image (56dp square) or avatar (40dp circle) and a short trailing text; options without them render as today. Needed by the Deezer artwork item. Gate: four-gate component record in this folder; device check that the Person form menu is unchanged.

## Origin
Needed by `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/`
(REQ-ART-06, 08, 09). Registered as its own component task, nested under `autocomplete-component`, per
`component-change-governance.md` (no bundling). Predecessor: the `m3-lists` change
`2026-11-13-listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar`.

## The change
1. **`AutocompleteSuggestion`** (`Contracts/Models/`) gains optional `init` properties, all defaulting
   to "nothing", so every existing constructor call compiles and renders as before:
   - `AutocompleteLeadingKind LeadingKind` (`None` default, `Image`, `Avatar`), a new enum beside the record;
   - `string? ImageUrl`; `string? PlaceholderIcon`; `string? TrailingText`;
   - computed `bool HasLeadingImage => LeadingKind == Image`, `bool HasLeadingAvatar => LeadingKind == Avatar`.
2. **`AutocompleteDXField.xaml` option row template** becomes a three-column grid
   (`Auto,*,Auto`, `Padding="16,8"` kept, no column spacing):
   - column 0: `ListItemLeadingImage` (visible when `HasLeadingImage`) and `ListItemLeadingAvatar`
     (visible when `HasLeadingAvatar`), each with a 12 dp right margin, vertically centred, bound to
     `ImageUrl` and `PlaceholderIcon`;
   - column 1: the existing two labels (label text `Body.Large`, supporting text `Body.Medium` /
     `OnSurfaceVariant`, hidden when null), now `MaxLines="1"` with tail truncation, vertically centred;
   - column 2: trailing text label (`Label.Small`, `OnSurfaceVariant`, 12 dp left margin, vertically
     centred, hidden when `TrailingText` is null).
   An option with `LeadingKind = None` and no trailing text shows only column 1, as today.
3. No BindableProperty, command, event or logic of `AutocompleteDXField` changes. `AutocompleteDXFieldLogic` is untouched.

## Gate 1 — dedicated task + MD3 review (m3.material.io, captured 2026-11-13)
Evidence file: `findings.md` of the artwork item (verbatim quotes and URLs).
- The field's dropdown is a menu: "Use menus in situations that need extra actions, like: Overflow menus / Text field dropdown menus / Select menus / Context menus"; "A menu can include a text field to filter options. This pattern is also known as autocomplete." (menus guidelines).
- Images in a menu row: "Menus have custom slots that support more flexible item layouts." "Slots work best with simple content such as: Images / Progress indicators / Color swatches". Constraints: "Keep the same menu item padding", "Targets should be 48x48dp or larger", "Don't add buttons, switches, or other direct actions into the menu item." The row keeps its existing padding (`Padding="16,8"`), grows to its content (project outcome: about 60 dp with an avatar, 72 dp with an image) and gains no interactive element.
- Gap: "Padding between elements within a list item 12dp" (menus specs).
- Trailing text: the menu item anatomy lists "Trailing text (optional)" and "Supporting text (optional)".
- Truncation: "Both the label and supporting text can wrap or be truncated." (lists guidelines). One line each with an ellipsis is a project decision (artwork item `design.md` D19); MD3 permits it and does not require it.
- The 12 dp gap after the leading element is the menus value; `ListItem` uses 16 dp for its leading slot today. The difference is intended (menu item versus list item).
- **Not in the official docs:** the height of a menu row that holds a 40 or 56 dp image (the row grows to its content plus the kept padding).

## Gate 2 — consumer map (grep 2026-11-13)
`AutocompleteDXField` in `*.xaml`:
| Consumer | Options built by |
|----------|------------------|
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml:32` (`titleField`) | `SongFormViewModel.ToTitleOption` (`SongFormViewModel.cs:450`) |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml:66` (`artistField`) | `SongFormViewModel.cs:342` |
| `MyVocaList/UI/Pages/People/PersonFormPage.xaml:20` (`nameField`) | `PersonFormViewModel.cs:301` |

Other `new AutocompleteSuggestion(` call sites: `ArtistFormViewModel.cs:239-240` (not bound to any page
today) and tests. All use the three-argument constructor, which is unchanged.

## Gate 3 — per-consumer risk
| Consumer | What could break | Verification |
|----------|------------------|--------------|
| `PersonFormPage` `nameField` | Row height or text position changes; a long name or supporting text is now cut with an ellipsis instead of wrapping | Device: Person form menu rows look as before for normal-length names; unit tests of `PersonFormViewModel` and `AutocompleteDXFieldLogic` stay green |
| `SongFormPage` `titleField` | Menu taller per row (72 dp), so fewer rows fit above the keyboard; pick, lock and X must still work | Device: type a title, see covers, pick a Deezer row, X; `SongFormViewModelTests` green |
| `SongFormPage` `artistField` | Row gains an avatar (56 dp row) | Device: type an artist, see avatar or placeholder, pick |
| `ArtistFormViewModel` options | None (no page binds them) | Build |

## Gate 4 — approval
Pre-authorised by Helder on 2026-11-13 ("take decisions by yourself … don't ask me anything").
Decision taken by the orchestrator; Helder can overrule it after the device check.
