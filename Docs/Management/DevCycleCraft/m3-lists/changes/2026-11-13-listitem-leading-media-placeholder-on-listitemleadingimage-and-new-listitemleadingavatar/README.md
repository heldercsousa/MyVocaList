---
id: listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar
title: "ListItem leading media: placeholder on ListItemLeadingImage and new ListItemLeadingAvatar"
status: 🟡 In Progress
target: 2026-11-13
section: DevCycleCraft
parent: m3-lists
goal: "ListItemLeadingImage shows a placeholder icon when its image is missing or fails to load, and a new ListItemLeadingAvatar (40dp circle) shows a picture with the same fallback. Needed by the Deezer artwork item. Gate: four-gate component record in this folder; device check that YouTube search thumbnails are unchanged."
kind: change
---

# ListItem leading media: placeholder on ListItemLeadingImage and new ListItemLeadingAvatar

ListItemLeadingImage shows a placeholder icon when its image is missing or fails to load, and a new ListItemLeadingAvatar (40dp circle) shows a picture with the same fallback. Needed by the Deezer artwork item. Gate: four-gate component record in this folder; device check that YouTube search thumbnails are unchanged.

## Origin
Needed by `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/`
(REQ-ART-06, 09, 19, 22). Registered as its own component task, nested under `m3-lists`, per
`component-change-governance.md` (no bundling). The page tasks that use it are successor tasks of that item.

## The change
1. **`ListItemLeadingImage`** gains one BindableProperty: `PlaceholderIcon` (`string`, default null).
   - When set: the 56 x 56 dp container gets a `SurfaceContainerHighest` background and shows the icon
     (24 x 24 dp, tinted `OnSurfaceVariant`, `dx:DXImage`) centred **under** the image. The image draws
     over it once loaded; with no source or a failed load the icon stays visible.
   - When null (default): the component renders exactly as today (transparent container, no icon).
   - `ImageSource`, `Aspect`, size (56 x 56) and corner radius (4) are unchanged.
2. **`ListItemLeadingAvatar`** (new component, `UI/Components/Lists/`): a 40 x 40 dp circle that clips
   its content. BindableProperties: `ImageSource` (`ImageSource`), `PlaceholderIcon` (`string`,
   default `account_circle_outlined`). Same layering: icon under the image, on `SurfaceContainerHighest`.
   Container: `dx:DXBorder` (`CornerRadius="20"`) if the DevExpress docs for the csproj version confirm
   it clips its content to the rounded corners; otherwise a MAUI `Border` with a round `StrokeShape`,
   the same precedent as `ListItemLeadingImage`. The implementor records which one and why in the task-log.
3. `ListItem` itself is **not** changed.

## Gate 1 — dedicated task + MD3 review (m3.material.io, captured 2026-11-13)
Evidence file: `findings.md` of the artwork item (verbatim quotes and URLs).
- Leading image size: "leading image width 56dp", "leading image height 56dp" (lists specs tokens). Confirms 56 x 56.
- Leading avatar size: "leading avatar size 40dp". Confirms 40 x 40.
- Shape: "Avatars: List items can include images in circular or expressive shapes to represent a person or entity." / "Use square or rectangular images for other content, such as products or videos." (lists guidelines).
- Anatomy names: "Leading avatar container", "Leading media - image or video". The component names follow them.
- **Not in the official docs** (project decisions, recorded in the artwork item's `design.md` D6/D7): the placeholder for a missing image, and the 4 dp image corner radius.

## Gate 2 — consumer map (grep 2026-11-13, `ListItemLeadingImage` in `*.xaml`)
| Consumer | Usage |
|----------|-------|
| `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml:60` | `ImageSource="{Binding ThumbnailUrl}" Aspect="AspectFill"`; does not set `PlaceholderIcon` |

`ListItemLeadingAvatar` has no consumer before this change. New consumers added by the successor
tasks: the `AutocompleteDXField` option row (image and avatar) and `SongsPage` (image).

## Gate 3 — per-consumer risk
| Consumer | What could break | Verification |
|----------|------------------|--------------|
| `YouTubeSearchPage` | Thumbnail size, crop or background changes if the default path is not identical | Device: YouTube search results show thumbnails as before (56 dp, rounded 4, no grey background, no icon) |

## Gate 4 — approval
Pre-authorised by Helder on 2026-11-13 ("take decisions by yourself … don't ask me anything").
Decision taken by the orchestrator; Helder can overrule it after the device check.

## Governance list
`ListItemLeadingImage` gets more consumers and `ListItemLeadingAvatar` its first ones in the successor
tasks; both are added to the governed list in `.claude/library/component-safety-gate.md` in this work.
