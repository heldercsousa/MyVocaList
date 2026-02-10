# Final Application Iconography Guideline

This document provides the official, validated SVG icons for the application. Each icon has a direct download link to the official Google Material Design Icons repository to ensure correct rendering and a consistent user experience.

## Icon Download Information

All icons are sourced from Google's official Material Design Icons repository:
**Repository:** https://github.com/google/material-design-icons

**URL Pattern for downloading icons:**
- **Outlined variant:** `https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/{icon_name}/materialsymbolsoutlined/{icon_name}_24px.svg`
- **Filled variant:** `https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/{icon_name}/materialsymbolsoutlined/{icon_name}_fill1_24px.svg`

**Important:** All icons use `viewBox="0 -960 960 960"` format for proper rendering in .NET MAUI.

---

## Implementation Guide - DevExpress MAUI

### Icon Variants

| Variant | When to Use | File Suffix |
|---------|-------------|-------------|
| **Outlined** | Navigation menus, unselected states, secondary actions | `_outlined` |
| **Filled** | Selected/active states, primary actions, emphasis | `_filled` |

### Using Icons with DevExpress DXButton

```xml
<!-- DXButton with icon (file name without .svg extension) -->
<dx:DXButton Content="Queue"
             Icon="format_list_numbered_outlined"
             IconColor="{dx:ThemeColor OnSurfaceVariant}" />

<!-- Icon-only button -->
<dx:DXButton Icon="settings_outlined"
             IconColor="{dx:ThemeColor OnSurface}"
             BackgroundColor="Transparent" />
```

### Using Icons with MAUI Image

```xml
<!-- Standard Image with theme tint -->
<Image Source="nightlife_outlined.svg"
       WidthRequest="24" HeightRequest="24">
    <Image.Behaviors>
        <IconTintColorBehavior TintColor="{StaticResource OnSurface}" />
    </Image.Behaviors>
</Image>
```

### Icon File Naming Convention

Icons are stored in `Resources/Images/` with this naming pattern:
- `{icon_name}_outlined.svg` - Outlined variant
- `{icon_name}_filled.svg` - Filled variant

MAUI build system compiles SVG to PNG at appropriate densities (1x, 2x, 3x). Reference icons by filename without extension.

### Icon Selection Process

1. Find the feature in this document (Core Functions, User Actions, etc.)
2. Read the rationale to ensure it matches your use case
3. Use the `_outlined` variant for navigation and secondary contexts
4. Use the `_filled` variant for selected states and primary emphasis
5. Reference by filename without extension (e.g., `nightlife_outlined`)

---

## Core App Functions

### Musicians/Bands
- **Symbol Name:** `music_note`
- **Rationale:** The most direct symbol for music. Defines the artists by their primary purpose: creating music.
- [**Download `music_note_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/music_note/materialsymbolsoutlined/music_note_24px.svg)
- [**Download `music_note_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/music_note/materialsymbolsoutlined/music_note_fill1_24px.svg)

### Musics Catalog
- **Symbol Name:** `library_music`
- **Rationale:** A collection or repertoire of songs.
- [**Download `library_music_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/library_music/materialsymbolsoutlined/library_music_24px.svg)
- [**Download `library_music_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/library_music/materialsymbolsoutlined/library_music_fill1_24px.svg)

### Venues (Karaoke)
- **Symbol Name:** `nightlife`
- **Rationale:** Captures the social, bar, and event atmosphere of a karaoke venue.
- [**Download `nightlife_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/nightlife/materialsymbolsoutlined/nightlife_24px.svg)
- [**Download `nightlife_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/nightlife/materialsymbolsoutlined/nightlife_fill1_24px.svg)

### Events
- **Symbol Name:** `event`
- **Rationale:** Universal symbol for a scheduled event or a specific date/occasion.
- [**Download `event_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/event/materialsymbolsoutlined/event_24px.svg)
- [**Download `event_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/event/materialsymbolsoutlined/event_fill1_24px.svg)

### Singers
- **Symbol Name:** `mic`
- **Rationale:** Defines the participant by their primary tool and action: singing.
- [**Download `mic_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/mic/materialsymbolsoutlined/mic_24px.svg)
- [**Download `mic_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/mic/materialsymbolsoutlined/mic_fill1_24px.svg)

### Event Queue
- **Symbol Name:** `format_list_numbered`
- **Rationale:** Represents a numbered, ordered list of singers for the event.
- [**Download `format_list_numbered_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/format_list_numbered/materialsymbolsoutlined/format_list_numbered_24px.svg)
- [**Download `format_list_numbered_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/format_list_numbered/materialsymbolsoutlined/format_list_numbered_fill1_24px.svg)

---

## Singer Status in Queue

### Currently Singing
- **Symbol Name:** `graphic_eq`
- **Rationale:** The equalizer icon clearly indicates active music or sound.
- [**Download `graphic_eq_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/graphic_eq/materialsymbolsoutlined/graphic_eq_24px.svg)
- [**Download `graphic_eq_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/graphic_eq/materialsymbolsoutlined/graphic_eq_fill1_24px.svg)

### Sung (Turn Completed)
- **Symbol Name:** `check_circle`
- **Rationale:** A checkmark inside a circle is a universally recognized symbol for a completed task.
- [**Download `check_circle_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/check_circle/materialsymbolsoutlined/check_circle_24px.svg)
- [**Download `check_circle_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/check_circle/materialsymbolsoutlined/check_circle_fill1_24px.svg)

### Declined / Skipped Turn
- **Symbol Name:** `block`
- **Rationale:** A clear and unambiguous way to show that a singer declined their turn.
- [**Download `block_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/block/materialsymbolsoutlined/block_24px.svg)
- [**Download `block_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/block/materialsymbolsoutlined/block_fill1_24px.svg)

---

## User & CRUD Actions

### Person / Profile (Default)
- **Symbol Name:** `account_circle`
- **Rationale:** Standard icon for a user profile when no photo is available.
- [**Download `account_circle_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/account_circle/materialsymbolsoutlined/account_circle_24px.svg)
- [**Download `account_circle_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/account_circle/materialsymbolsoutlined/account_circle_fill1_24px.svg)

### Save / Confirm
- **Symbol Name:** `check`
- **Rationale:** Universal symbol for confirmation, completion, and saving.
- [**Download `check_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/check/24px.svg)
- [**Download `check_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/check/24px.svg)

### Cancel
- **Symbol Name:** `close`
- **Rationale:** Standard for dismissing or canceling an action.
- [**Download `close_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/close/24px.svg)
- [**Download `close_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/close/24px.svg)

### Add New
- **Symbol Name:** `add`
- **Rationale:** Simple plus sign for adding a new item.
- [**Download `add_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/add/24px.svg)
- [**Download `add_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/add/24px.svg)

### Delete
- **Symbol Name:** `delete`
- **Rationale:** The trash can is the intuitive icon for a destructive delete action.
- [**Download `delete_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/delete/24px.svg)
- [**Download `delete_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/delete/24px.svg)

### Edit
- **Symbol Name:** `edit`
- **Rationale:** Standard pencil icon for modifying data.
- [**Download `edit_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/edit/materialsymbolsoutlined/edit_24px.svg)
- [**Download `edit_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/edit/materialsymbolsoutlined/edit_fill1_24px.svg)
---

## App Management & Miscellaneous

### Menu
- **Symbol Name:** `menu`
- **Rationale:** For opening a side navigation menu or drawer.
- [**Download `menu_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/menu/24px.svg)
- [**Download `menu_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/menu/24px.svg)

### Navigate Back
- **Symbol Name:** `arrow_back`
- **Rationale:** Standard for returning to the previous screen.
- [**Download `arrow_back_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/arrow_back/24px.svg)
- [**Download `arrow_back_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/arrow_back/24px.svg)

### Navigate Forward
- **Symbol Name:** `arrow_forward`
- **Rationale:** Standard for moving to the next step or screen.
- [**Download `arrow_forward_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/arrow_forward/24px.svg)
- [**Download `arrow_forward_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/arrow_forward/24px.svg)

### Config / Setup
- **Symbol Name:** `settings`
- **Rationale:** Universal icon for accessing application settings.
- [**Download `settings_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/settings/24px.svg)
- [**Download `settings_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/settings/24px.svg)

### Backup/Restore
- **Symbol Name:** `cloud_sync`
- **Rationale:** For managing cloud data synchronization, backup, and restore.
- [**Download `cloud_sync_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/cloud_sync/24px.svg)
- [**Download `cloud_sync_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/cloud_sync/24px.svg)

### Theme
- **Symbol Name:** `contrast`
- **Rationale:** For switching between light/dark application themes.
- [**Download `contrast_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/contrast/24px.svg)
- [**Download `contrast_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/contrast/24px.svg)

### Language
- **Symbol Name:** `language`
- **Rationale:** For opening language selection options.
- [**Download `language_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/language/24px.svg)
- [**Download `language_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/language/24px.svg)

### History
- **Symbol Name:** `history`
- **Rationale:** Excellent for showing a chronological record or audit trail.
- [**Download `history_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/history/24px.svg)
- [**Download `history_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/history/24px.svg)

### Reports
- **Symbol Name:** `bar_chart`
- **Rationale:** A bar chart is the most common icon for data analysis and reports.
- [**Download `bar_chart_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/bar_chart/24px.svg)
- [**Download `bar_chart_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/bar_chart/24px.svg)

### Birthday
- **Symbol Name:** `cake`
- **Rationale:** A friendly and clear way to represent a birthday.
- [**Download `cake_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/cake/24px.svg)
- [**Download `cake_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/cake/24px.svg)

### Quit App
- **Symbol Name:** `logout`
- **Rationale:** For exiting the application or logging out.
- [**Download `logout_outlined.svg`**](https://fonts.gstatic.com/s/i/materialiconsoutlined/logout/24px.svg)
- [**Download `logout_filled.svg`**](https://fonts.gstatic.com/s/i/materialicons/logout/24px.svg)

### Search  (Search Icon)
- **Symbol Name:** `search`
- **Rationale:** Search bars, search functionality, find features.
- [**Download `search_outlined.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/search/materialsymbolsoutlined/search_24px.svg)
- [**Download `search_filled.svg`**](https://raw.githubusercontent.com/google/material-design-icons/master/symbols/web/search/materialsymbolsoutlined/search_fill1_24px.svg)
