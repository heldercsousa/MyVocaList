# MD3: list rows with artwork (autocomplete menu + CRUD song list) - evidence report

Capture date: 2026-11-13. Method: Playwright + Edge, `main.innerText` + image alt text. All quotes verbatim from the captures in this folder.

Captures (all under `...\scratchpad\md3\`):
- `reused\` - crawl by another session on 2026-11-12 (text + `ALT:` lines): lists overview/guidelines/specs/accessibility, `components_lists_specs_tokens.txt` (token viewer values), menus overview/guidelines/specs/accessibility, `components_menus_specs_tokens.txt`.
- `new\` - crawled 2026-11-13: search overview/guidelines/specs/accessibility, text-fields overview/guidelines/specs, `styles_shape_corner-radius-scale`, `components_menus_specs` (fresh), `components_lists_overview`.
- Script: `crawl.js`.
- Caveat: the specs pages put most measurements in diagram images (alt text carries only a description, not the numbers). Numbers below come from (a) the "Attribute / Value" text table (baseline), and (b) the token viewer dump (`*_specs_tokens.txt`, "List - Common" = expressive + baseline). The shape-token VALUES (avatar/image corner radius) did not render in the dump.

Legend: [RULE] = prose guidance. [SPEC] = spec table / token value. [ALT] = image alt text / example only. NOT FOUND = silent in the crawl.

---

## Step C

### 1. Leading elements, sizes, shape, square vs rectangular
[RULE] https://m3.material.io/components/lists/guidelines
- "Leading images, videos, icons, or avatars"
- Anatomy: "Leading avatar container", "Leading avatar text", "Leading icon", "Leading media - image or video"
- "List items can contain a leading avatar, image, or video. Anchor visuals to the leading edge of the list to improve scannability."
- "Avatars: List items can include images in circular or expressive shapes to represent a person or entity."
- "Use square or rectangular images for other content, such as products or videos."
- Do: "Place supporting visuals, like thumbnails, at the leading edge of a row to improve scannability"; Caution: "Avoid placing visuals in the center of a row because it makes the list difficult to scan"
- Specs (slots) https://m3.material.io/components/lists/specs : "Leading slots can contain: Visual elements: Avatar, icon, image, or video thumbnail ... Customizations: Badge or larger image"; "The leading and trailing slot positions must be a smaller width than the content section."

[SPEC] tokens (https://m3.material.io/components/lists/specs, token viewer "List - Common", `reused\components_lists_specs_tokens.txt`):
- "List list item leading avatar size | 40dp"
- "List list item leading icon size | 24dp"; "List list item leading icon expressive size | 20dp"
- "List list item leading image width | 56dp"; "List list item leading image height | 56dp"  (image token is square 56x56)
- "List list item leading video width | 100dp"; "small leading video width 100dp / height 56dp"; "large leading video width 114dp / height 64dp"  (video is 16:9-ish rectangular)
- Shape tokens exist ("List list item leading avatar shape", "leading video shape", "leading image shape", "leading image expressive shape") but their values are NOT FOUND in the crawl.
- Corner radius for the image: NOT FOUND. The scale itself (https://m3.material.io/styles/shape/corner-radius-scale): "None - 0dp / Extra small - 4dp / Small - 8dp / Medium - 12dp / Large - 16dp ... Full - fully rounded corners".
- [ALT] "3 list items with avatars using different expressive shapes." ; "List of contacts with avatars with a circular, expressive crop to indicate a person." ; "A list of plants with images at leading edge." ; "A song list with a leading images. When selected, a list item's container matches the image's color scheme."

Answer to "square vs rectangular": stated as rule - avatars (person/entity) circular/expressive; products/videos and "other content" square or rectangular. Token value for a leading image is 56x56 (square); video tokens are rectangular. Album cover = "other content" -> square image (56x56 token); artist picture = person -> avatar (40dp token, circular/expressive).

### 2. Heights, line configurations, paddings
[SPEC] tokens (token viewer, expressive+baseline common set):
- "One line list item container height 56dp"; "Two lines list item container height 72dp"; "Three lines list item container height 88dp"
- "List list item leading space 16dp"; "trailing space 16dp"; "top space 10dp"; "bottom space 10dp"; "between space 12dp"
- "List list item divider height 1dp"; "divider leading space 16dp"; "divider trailing space 16dp"; "List segment gap 2dp"
- Text: "List item size is determined by the tallest element within the list item." [RULE] (lists/guidelines, Anatomy > Container)

[SPEC] baseline Attribute/Value table (https://m3.material.io/components/lists/specs, "Layout"):
- "Label left padding 16dp"; "Leading element left padding 16dp"; "Trailing element left padding 16dp"; "Trailing element right padding 24dp"; "Targets 48dp"
- "Leading icon top padding 8dp"; "Leading icon top padding when height is 88dp or taller 12dp"
- Gap leading element -> text with an avatar/image: no separate attribute in the table; only the token "List list item between space 12dp" (applies to elements within an item - NOT confirmed as the leading-to-text gap specifically).
- Segmented/expressive: "Unselected corner radius: 4dp inner, 16dp outer; Selected corner radius: 16dp"

Note: 56dp image + 10dp top + 10dp bottom = 76dp > 72dp (two-line height); 56dp image + 2x8 = 72. The docs state "List item size is determined by the tallest element", so a 56dp image in a 2-line item is allowed to grow the row; exact resulting height NOT FOUND.

### 3. Text roles
[SPEC] token viewer:
- Label text: Roboto 16 / line height 24 / tracking 0.5 / weight 400 (= body large); color "#1D1B20" (on surface).
- Supporting text: 14 / 20 / 0.25 / 400 (= body medium); color "#49454F" (on surface variant).
- Overline: 11 / 16 / 0.5 / 500 (= label small); color on surface variant.
- Trailing supporting text: 11 / 16 / 0.5 / 500; color on surface variant.
- Avatar label: 16 / 24 / 0.15 / 500 (title medium); avatar container "#EADDFF", label "#4F378B".
- Color-role list (specs, expressive): "Surface / On surface variant / On surface / ... Outline variant / Primary container / On primary container".
[RULE] lists/guidelines:
- "Label text only: A list item can contain a single line of label text. If the text doesn't fit on one line, it can wrap or be truncated."
- "Label text with supporting text: A list item can include supporting text below the label text. Both the label and supporting text can wrap or be truncated."
- "Keep label text brief." ; "To ensure list items are scannable: Limit supporting text to one to three lines; Truncate supporting text, depending on screen size"
- "Trailing text can provide additional meta-information about a list item, such as a price, count, or other details."
- [ALT] "A list item with a leading image, concise label text "Art events", and 2 lines of truncated supporting text."
Rule on WHAT content goes in supporting text: NOT FOUND beyond the above.

### 4. Vertical alignment of the leading element
[SPEC] baseline table: "Leading element alignment (vertical) Center"; "Leading element alignment (vertical) when height is 88dp or taller Top"; "Label alignment Center"; "Label alignment when height is 88dp or taller Top"; "Trailing element alignment (vertical) Center / ... when height is 88dp or taller Top"; "Leading icon alignment (vertical) Top".
So 1- and 2-line (56/72dp): centered; 3-line (88dp): top. Rule in prose: "Place supporting visuals and primary text in the same position in each list item." / "Don't vary the position of elements within a list." (lists/guidelines).

### 5. Missing-image fallback; mixing rows with/without images
- Placeholder/fallback when an image is missing: NOT FOUND in the crawl (no mention of placeholder, fallback, monogram, initials in lists, menus, search, text-fields captures).
- Closest evidence: anatomy lists "Leading avatar text" (a monogram/text avatar exists as an element; token "leading avatar label" 16/24/500) - anatomy/token only, not a fallback rule. https://m3.material.io/components/lists/guidelines
- Consistency [RULE]: "A list should be easy to scan. Any element can be used to anchor and align list item content. Place supporting visuals and primary text in the same position in each list item. Don't vary the position of elements within a list." / [ALT] "Supporting visuals are aligned for easy scanning" / "Primary text is aligned for easy scanning". Also the spec config alt: "1, 2, and 3-line list items with and without a leading image and trailing checkbox, in dark mode." (shows "with and without" variants as configurations, not a mixing rule).
- Icon relation [RULE]: "A leading icon should provide a quick visual cue that relates to the item's label text, helping people scan the list."
- Mixed with/without images in one list: NOT FOUND as a rule; the consistency rule above implies keeping the text column aligned.

### 6. Menus (autocomplete dropdown)
[RULE] https://m3.material.io/components/menus/guidelines
- "Use menus in situations that need extra actions, like: Overflow menus / Text field dropdown menus / Select menus / Context menus"
- "A menu can include a text field to filter options. This pattern is also known as autocomplete. As someone types, the list of menu options filters to show relevant results. This helps people quickly find the right option from a long list."
- "Menu items can include label text, leading icons, trailing icons, and keyboard commands."
- Anatomy: "Menu item / Leading icon (optional) / Menu item text / Trailing icon (optional) / Badge (optional) / Trailing text (optional) / Container / Supporting text (optional) / Label text (optional) / Gap (optional) / Divider (optional)"
- Slots: "Menus have custom slots that support more flexible item layouts." ; "Slots work best with simple content such as: Images / Progress indicators / Color swatches" ; "Keep the same menu item padding" ; "Targets should be 48x48dp or larger" ; "Don't add buttons, switches, or other direct actions into the menu item." ; Caution: "Reserve the use of slots for use cases that maintain the menu's accessibility and functionality"
- Dividers: "Dividers create a more subtle separation between items. Use a divider for: Scrollable menus; Text fields with a dropdown menu, where a grouped treatment isn't appropriate"; "Don't use gaps in scrollable menus"
- Compact: "Consider adapting menus into bottom sheets on small screens. They have more space to display additional items and longer labels."
- [ALT] "A menu showing an undefined slot that could be used for a different element, such as an image." ; "1 diagram and 1 menu showing icons in each item's leading slot." ; "An animation showing a text field being typed into. As text is added, the list of menu items below filters down to show only matching options."
[SPEC] https://m3.material.io/components/menus/specs (token viewer, expressive vertical menu): "Menu item height 44dp", top/bottom space 8dp, leading/trailing space 16dp, between space 12dp, "Menu item leading icon size 20dp"; supporting text 12/16/0.4/400; label text 14/20/0.1/500. Baseline table: "Container width 112dp min, 280dp max", "Corner radius 4dp", "List item height 48dp", "Padding between elements within a list item 12dp", "Leading/trailing icon size 24dp". 
Answer: official anatomy lists leading ICON only; images are allowed only through the custom "slot" mechanism (guidelines + ALT), with the item padding kept and 48dp targets. A dedicated avatar/artwork row spec for menus: NOT FOUND. Two-line height for menu items: NOT FOUND (supporting text is an optional element with its own type token; no height value).
Text-field side (https://m3.material.io/components/text-fields/guidelines): "Images that are 24dp in height can be placed inside of text fields." ; "Dropdown icon: A dropdown arrow indicates that a text field has a nested selection component." No separate exposed-dropdown/autocomplete page or term was found in the text-field pages; the only "autocomplete" statement is the menus Filtering paragraph above. (Menu page links "Text field dropdown menus"/"Select menus" but no row-anatomy guidance for them.)

### 7. Search suggestion rows
https://m3.material.io/components/search/guidelines [RULE]
- "Search suggestions and results both appear in a list component by default."
- "To help people find information quickly, consider adding variety and context, such as: Leading icons related to suggestions / Category labels, like Recent, Contacts, or Suggestions / Avatars or other high-priority items / Filter chips to narrow down results"
- [ALT] "Include high-priority items like avatars in search suggestions or results" (caption) ; "Search with suggestions organized in a column, ending with a row of 5 contact avatars with names." ; "A gap separates the location and calendar list items from people and pets avatars."
- Specs: "Search includes a search bar and a container for suggestions and results. The container is empty by default. Use the list component to add content."
- Docked layout vs full-screen: "Docked opens a list below the search bar, with a scrim covering main content / Full-screen expands to fill the screen"; "Full-screen layout: Default for compact breakpoints".
- Leading images (album art) in search rows: NOT FOUND as such; avatars and leading icons are the stated options; images follow the list component (above).
- Accessibility: "Search suggestions and results use the list component. Screen readers automatically announce the results as a list." ; "When search suggestions and results appear, the screen reader must announce the change."

### 8. Accessibility
- Touch targets: lists/specs "Target size must be at least 48x48dp"; baseline table "Targets 48dp"; menus "Targets should be 48x48dp or larger".
- Labels: lists/accessibility "The accessibility label for a list item is typically the same as the label text and supporting text." ; "A list item's label text and supporting text is used for its accessibility label".
- Alt/content description for decorative leading list images: NOT FOUND for lists. Only menus: "For menu items with text and an icon, the icon's accessibility label should be marked as decorative to avoid redundant verbalizations." (https://m3.material.io/components/menus/accessibility; ALT: "A menu item icon of an eye next to the word "preview" has a note of "Decorative."") - icon, menus only; by analogy only, not a list-image rule.
- Selection cue: "don't rely on color as the only visual cue." (lists/accessibility)

### 9. Dividers
[RULE] lists/guidelines: "Limit dividers to uncontained or complex lists, only when a stronger visual separation is necessary." ; "Use gaps for contained lists." ; Caution: "Limit the use of dividers to uncontained lists". [SPEC] baseline table: "Divider full-width 100%"; "Divider inset left padding 16dp"; "Divider inset right padding 24dp"; "Padding above/below divider 0dp". Tokens: divider leading/trailing space 16dp. Inset-aligned-to-text (past the image) vs from item edge: NOT FOUND (only 16dp inset stated). Menus: dividers recommended for "Scrollable menus; Text fields with a dropdown menu".

---

## Step D - internal guidelines vs official docs

Internal sources read: `.claude/library/m3-lists.md`, `.claude/library/ux-patterns.md` (Autocomplete section), `Docs/Management/DevCycleCraft/autocomplete-component/md3-autocomplete-mobile-ux-guideline.md` (marked ABANDONED 2026-11-11; Gemini-sourced). `m3-components.md`/`crud-listview.md` only contain slot names and `ListItemLeadingIcon` XAML.

| # | Internal rule | Verdict | Evidence |
|---|---|---|---|
| 1 | Set heights: 1-line ~56dp, 2-line ~72dp, 3-line ~88dp | CONFIRMED | "One line list item container height 56dp ... Two lines ... 72dp ... Three lines ... 88dp" |
| 2 | 3-line leading/trailing/text top-aligned, 8dp from top | CONFIRMED (8dp for ICON only) | "Leading element alignment (vertical) when height is 88dp or taller Top"; "Leading icon top padding 8dp"; "...88dp or taller 12dp". The 8dp is a leading-ICON value; for avatar/image the top offset is NOT FOUND (the 12dp 88dp-case is stated for the icon too). |
| 3 | 1-/2-line leading/trailing centered | CONFIRMED | "Leading element alignment (vertical) Center" |
| 4 | Typography: overline labelSmall 11, headline bodyLarge 16, supporting bodyMedium 14, colors OnSurface / OnSurfaceVariant | CONFIRMED (sizes/colors) | tokens 11/16/14 and #1D1B20 / #49454F. Internal says "RobotoRegular" for overline/label; official overline weight is 500 (Medium) and label tracking 0.5. Minor mismatch: overline weight 500 vs internal RobotoRegular. |
| 5 | Terminology: Headline, Overline text, Leading/Trailing element | PARTLY CONTRADICTED | Official terms are "Label text", "Supporting text", "Overline", "Leading media - image or video", "Leading avatar", "Leading icon", "Trailing text" - "Headline" is not used on the lists pages. |
| 6 | `ListItemLeadingIcon` 24dp icon / 40dp area | PARTLY CONFIRMED | 24dp icon token confirmed; "40dp area" for an icon not found (40dp is the AVATAR size token). Expressive icon size is 20dp. |
| 7 | `ListItemLeadingAvatar` 40dp circle, CornerRadius=20 | CONFIRMED (size, circle) | "leading avatar size 40dp"; "circular or expressive shapes". Expressive alternative shape is allowed. |
| 8 | `ListItemLeadingImage` 56dp square, CornerRadius=4 | SIZE/SQUARE CONFIRMED; RADIUS UNSUPPORTED | "leading image width 56dp / height 56dp". 4dp radius: shape token value NOT FOUND (4dp is merely the "Extra small" step in the scale). |
| 9 | Container Padding="16,0,24,0" | CONFIRMED (baseline) | "Leading element left padding 16dp", "Trailing element right padding 24dp". Expressive tokens use 16/16 instead ("trailing space 16dp"). |
| 10 | Leading slot right margin 16dp to text | UNSUPPORTED / differs | Only "between space 12dp" token (expressive); baseline "Label left padding 16dp" is measured from the item edge as the table lists (leading element + label gap not stated). |
| 11 | Trailing slot left margin 8dp | UNSUPPORTED | "Trailing element left padding 16dp" (baseline). |
| 12 | MinimumHeightRequest 56 | CONFIRMED | token container height 56dp. |
| 13 | Selection control: trailing for text-only and for leading-element rows; leading when row has trailing action | PARTLY CONFIRMED | Spec configurations list "With leading icon and trailing checkbox", "With text and trailing checkbox", "With leading checkbox and trailing text"; "Multi-action ... The list item as a whole isn't selectable; only the individual actions are." The "icon dropped to avoid crowding" is an internal choice; also "Single-select list items: Don't support multi-actions" / multi-select "Can't have secondary nested actions" (contradiction risk with checkbox + catalog button row; see CLSG docs). |
| 14 | `IsSelected` -> SecondaryContainer | CONTRADICTED/DIFFERS | Official selected container color token "#E8DEF8" with "Primary container"/"On primary container" roles listed for lists; expressive selected state also changes corner shape ("Selected corner radius: 16dp"). (Exact role names for #E8DEF8 are not given as text.) |
| 15 | Multi-action: trailing DXButton gets its own touch target | CONFIRMED | "Place supplementary actions, like a bookmark or menu, in the trailing position." |
| 16 | Autocomplete = DevExpress docked menu below the field on all window sizes; no full-screen Search View | CONSISTENT, no contradiction | Official docs allow it: menus "Text field dropdown menus", "filter options. This pattern is also known as autocomplete". Official search "Full-screen layout: Default for compact breakpoints" / "docked ... best for medium and expanded" applies to Search, not to a form field's autocomplete menu - so the abandoned guideline's "full-screen on phone" claim is UNSUPPORTED as a rule for text-field autocomplete (the Search page only says full-screen is the default for compact search). |
| 17 | Guideline file claims: "A classic Exposed Dropdown Menu ... is fundamentally a desktop pattern", keyboard consumes 40-50%, "standard dropdowns should be abandoned in favor of Search Bar to Search View" | UNSUPPORTED | No such statement in menus/search/text-fields captures; Menus doc says the opposite for compact only: "Consider adapting menus into bottom sheets on small screens." (a bottom sheet, not full-screen search). "exposed dropdown" as a term: NOT FOUND in the crawl. |
| 18 | Internal map "docked menu maps to MD3 Menus - filtering / exposed dropdown" | PARTLY CONFIRMED | "Filtering ... also known as autocomplete" confirmed; "exposed dropdown" naming not on current pages. |
| 19 | (No internal rule found for) placeholder when image missing, mixed rows, menu rows with artwork, divider inset | GAP | Official docs also silent on placeholder (see Q5); a decision here is a project call, not an MD3 requirement. |

### Adaptation needs
- The DevExpress `AutoCompleteEdit` docked menu is a menu; official menu anatomy offers leading ICON/trailing icon/text/supporting text; artwork rows are only supported through "slots" (images allowed, "Keep the same menu item padding", 48dp targets, no extra interactive elements). Per internal ruling, no customisation requiring complex code -> using a DX item template with image + 2 text lines is the "slot" case; check DX template/height support and whether the menu uses 44dp (expressive menu item) or 48dp (baseline list-item/menu height) items. Row height of a menu item with a 40dp avatar or 56dp image is NOT FOUND in the menus specs.
- For the CRUD song list: use the list item spec (two-line 72dp or taller; image 56dp square; person/artist = 40dp circular avatar; centered vertically for <88dp; label 16/24, supporting 14/20). Internal `ListItemLeadingImage` radius 4 needs an owner decision (official value NOT FOUND). Row width/padding: baseline 16 start / 24 end vs expressive 16/16.
- Internal text uses "Headline"; keep code names but cite official terms (Label text) in docs.

## Files
- Report: `C:\Users\helde\AppData\Local\Temp\claude\78c429b059350e8f\cd15b820-3da3-48b6-ae46-dff728ad644b\scratchpad\md3\md3-list-artwork-report.md`
- Captures: `...\scratchpad\md3\reused\*.txt`, `...\scratchpad\md3\new\*.txt`, script `...\scratchpad\md3\crawl.js`


---

## Deezer API check (live public API, 2026-11-13)

Requests made by the orchestrator: `GET https://api.deezer.com/search/track?q=commando ramones&limit=2`
and `GET https://api.deezer.com/search/artist?q=metalica&limit=2`. Field names and values as returned.

- **Track** (`search/track`), first result `id 14618530`, "Commando (1999 Remaster)":
  - `title_short: "Commando"`, `title_version: "(1999 Remaster)"`, `md5_image: "cc0c69196d44c6ea479dcc48aef2464a"`.
  - `album.title: "Greatest Hits"`, `album.md5_image` equal to the track's `md5_image`.
  - `album.cover_small: "https://cdn-images.dzcdn.net/images/cover/cc0c69196d44c6ea479dcc48aef2464a/56x56-000000-80-0-0.jpg"`,
    `cover_medium: ".../250x250-000000-80-0-0.jpg"`, `cover_big: ".../500x500-..."`, `cover_xl: ".../1000x1000-..."`.
    The path holds the same hash as `md5_image`, so the URL is addressed by image content; the size is a path segment.
  - `artist.name: "Ramones"`, `artist.picture_small|medium|big|xl` on `https://cdn-images.dzcdn.net/images/artist/<hash>/<size>-000000-80-0-0.jpg`, same four sizes.
  - No lyric field; only `explicit_lyrics` and `explicit_content_lyrics`.
- **Artist** (`search/artist`), first result `id 119`, "Metallica": `picture_small|medium|big|xl` with the same URL shape, plus `nb_album`, `nb_fan`.
- All image URLs seen are `https` and square (56, 250, 500, 1000 px).
- **Not verified:** the byte size of a 250 px image (the "about 10 kB" in `design.md` D2 is an estimate), how
  long Deezer keeps a URL valid, and what Deezer returns for an album or artist with no picture (a
  search for obscure artists returned a hashed picture URL for every one of 9 results).
