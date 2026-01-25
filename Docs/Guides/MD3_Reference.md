# Material Design 3: Complete Technical Specification Reference

> **Purpose**: Comprehensive MD3 design system specifications
> **Version**: Includes M3 Expressive updates (May 2025)
> **For Implementation**: See `DesignSystem_Implementation_Guide.md`

---

## Table of Contents

1. [Design Token Architecture](#design-token-architecture)
2. [Color System](#color-system)
3. [Typography System](#typography-system)
4. [Shape System](#shape-system)
5. [Elevation System](#elevation-system)
6. [Motion System](#motion-system)
7. [Icons](#icons)
8. [Layout System](#layout-system)
9. [Interaction Patterns](#interaction-patterns)
10. [Accessibility](#accessibility)
11. [Components](#components)
12. [M3 Expressive Components (2025)](#m3-expressive-components-2025)
13. [Content Design](#content-design)
14. [Platform Implementation](#platform-implementation)
15. [Tools and Resources](#tools-and-resources)
16. [Critical Values Summary](#critical-values-summary)
17. [Glossary](#glossary)

---

## Design Token Architecture

MD3 uses a three-tier token hierarchy enabling theme-wide changes through single modifications.

### Reference Tokens (md.ref.*)

Foundational layer with concrete values - hex colors, pixels, font names. Never change based on theme.

**Typeface tokens:**
```
--md-ref-typeface-brand: 'Roboto'
--md-ref-typeface-plain: 'Roboto'
--md-ref-typeface-weight-regular: 400
--md-ref-typeface-weight-medium: 500
--md-ref-typeface-weight-bold: 700
```

**Palette tokens**: Each key color generates 13 tonal values:
- Tones: 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 95, 99, 100

### System Tokens (md.sys.*)

Semantic layer that adapts based on theme mode. ~50 color tokens total.

**Token categories:**
- `md.sys.color.*` - Color roles
- `md.sys.typescale.*` - Typography scales
- `md.sys.shape.*` - Corner radius values
- `md.sys.elevation.*` - Shadow definitions
- `md.sys.motion.*` - Animation parameters

### Component Tokens (md.comp.*)

Pattern: `--md-<component>-<element>-<property>`

Examples:
```
--md-filled-button-container-color
--md-filled-button-label-text-color
--md-filled-button-container-shape
--md-text-field-container-color
```

---

## Color System

### HCT Color Space

**HCT = Hue + Chroma + Tone** - Hybrid color space guaranteeing accessibility through math.

| Property | Range | Description |
|----------|-------|-------------|
| Hue | 0-360 | Color wheel position |
| Chroma | 0-~120 | Color intensity/saturation |
| Tone | 0-100 | Lightness (0=black, 100=white) |

### Contrast Guarantees

| Tone Difference | Contrast Ratio | WCAG Level |
|-----------------|----------------|------------|
| 40 | ≥3:1 | AA large text |
| 50 | ≥4.5:1 | AA normal text |
| 60 | ≥7:1 | AAA (recommended) |

### Five Key Colors from Single Source

| Key Color | Chroma | Hue Rotation | Purpose |
|-----------|--------|--------------|---------|
| Primary | 36 | 0° | Main brand, prominent components |
| Secondary | 16 | 0° | Less prominent elements |
| Tertiary | 24 | +60° | Contrasting accents |
| Neutral | 6 | 0° | Surfaces, backgrounds |
| Neutral Variant | 8 | 0° | Surface variants, outlines |

### Color Role Mappings

**Primary Group:**

| Role | Light Tone | Dark Tone |
|------|------------|-----------|
| Primary | 40 | 80 |
| OnPrimary | 100 | 20 |
| PrimaryContainer | 90 | 30 |
| OnPrimaryContainer | 10 | 90 |

**Secondary Group:**

| Role | Light Tone | Dark Tone |
|------|------------|-----------|
| Secondary | 40 | 80 |
| OnSecondary | 100 | 20 |
| SecondaryContainer | 90 | 30 |
| OnSecondaryContainer | 10 | 90 |

**Tertiary Group:**

| Role | Light Tone | Dark Tone |
|------|------------|-----------|
| Tertiary | 40 | 80 |
| OnTertiary | 100 | 20 |
| TertiaryContainer | 90 | 30 |
| OnTertiaryContainer | 10 | 90 |

**Error Group:**

| Role | Light Tone | Dark Tone |
|------|------------|-----------|
| Error | 40 | 80 |
| OnError | 100 | 20 |
| ErrorContainer | 90 | 30 |
| OnErrorContainer | 10 | 90 |

### Surface Container Hierarchy (7 Levels)

Replaces elevation-based tints with explicit surface colors:

| Role | Light Tone | Dark Tone | Purpose |
|------|------------|-----------|---------|
| Surface | 98 | 6 | Default background |
| SurfaceDim | 87 | 6 | Subdued areas |
| SurfaceBright | 98 | 24 | Elevated areas |
| SurfaceContainerLowest | 100 | 4 | Lowest emphasis |
| SurfaceContainerLow | 96 | 10 | Low emphasis |
| SurfaceContainer | 94 | 12 | Default containers |
| SurfaceContainerHigh | 92 | 17 | Higher emphasis |
| SurfaceContainerHighest | 90 | 22 | Highest emphasis |

### Fixed Accent Colors

Constant across light/dark themes:

| Role | Tone | Purpose |
|------|------|---------|
| PrimaryFixed | 90 | Fixed primary accent |
| PrimaryFixedDim | 80 | Stronger fixed primary |
| OnPrimaryFixed | 10 | Content on fixed |
| OnPrimaryFixedVariant | 30 | Variant content |
| SecondaryFixed | 90 | Fixed secondary |
| SecondaryFixedDim | 80 | Stronger fixed secondary |
| TertiaryFixed | 90 | Fixed tertiary |
| TertiaryFixedDim | 80 | Stronger fixed tertiary |

### Dynamic Color

**Sources:**
- User wallpaper (Android 12+)
- Content-based extraction (album art, images)
- App-defined seed color

**Decision tree:**
1. Platform supports dynamic? → Use dynamic with app accent fallback
2. No dynamic support? → Use static brand colors

### Advanced Color Customization

**Defining new color roles:**
1. Create tonal palette from source color
2. Assign light/dark tone values following contrast rules
3. Define On-color with 60+ tone difference
4. Define Container with 50+ tone difference from base

---

## Typography System

### Default Typeface: Roboto

MD3 uses Roboto as the default typeface. Download from Google Fonts.

**Roboto Font Files Required:**

| Weight | File | Usage |
|--------|------|-------|
| 400 (Regular) | Roboto-Regular.ttf | Body, Display, Headline |
| 500 (Medium) | Roboto-Medium.ttf | Title, Label |
| 700 (Bold) | Roboto-Bold.ttf | Emphasis (custom) |

**Roboto Characteristics:**
- Sans-serif typeface
- Open curves for legibility
- Mechanical skeleton with friendly forms
- Optimized for digital screens
- Supports 300+ languages

### Typography Scale (15 Roles)

**Display Styles** (Brand typeface - expressive):

| Role | Size (sp) | Line Height | Weight | Tracking | Use Case |
|------|-----------|-------------|--------|----------|----------|
| Display Large | 57 | 64 | 400 | -0.25 | Hero text, splash |
| Display Medium | 45 | 52 | 400 | 0 | Large headlines |
| Display Small | 36 | 44 | 400 | 0 | Section headers |

**Headline Styles** (Brand typeface - expressive):

| Role | Size (sp) | Line Height | Weight | Tracking | Use Case |
|------|-----------|-------------|--------|----------|----------|
| Headline Large | 32 | 40 | 400 | 0 | Page titles |
| Headline Medium | 28 | 36 | 400 | 0 | Card titles |
| Headline Small | 24 | 32 | 400 | 0 | Subsections |

**Title Styles** (Plain typeface - functional):

| Role | Size (sp) | Line Height | Weight | Tracking | Use Case |
|------|-----------|-------------|--------|----------|----------|
| Title Large | 22 | 28 | 400 | 0 | App bar, dialog |
| Title Medium | 16 | 24 | 500 | 0.15 | List headers |
| Title Small | 14 | 20 | 500 | 0.1 | Tabs, chips |

**Body Styles** (Plain typeface - functional):

| Role | Size (sp) | Line Height | Weight | Tracking | Use Case |
|------|-----------|-------------|--------|----------|----------|
| Body Large | 16 | 24 | 400 | 0.5 | Primary content |
| Body Medium | 14 | 20 | 400 | 0.25 | Secondary content |
| Body Small | 12 | 16 | 400 | 0.4 | Captions |

**Label Styles** (Plain typeface - functional):

| Role | Size (sp) | Line Height | Weight | Tracking | Use Case |
|------|-----------|-------------|--------|----------|----------|
| Label Large | 14 | 20 | 500 | 0.1 | Buttons, inputs |
| Label Medium | 12 | 16 | 500 | 0.5 | Tags, badges |
| Label Small | 11 | 16 | 500 | 0.5 | Timestamps |

### M3 Expressive: Emphasized Typography

New bolder variants for visual hierarchy (2025):

| Role | Size (sp) | Weight | Use Case |
|------|-----------|--------|----------|
| Display Large Emphasized | 57 | 500 | Hero with emphasis |
| Headline Large Emphasized | 32 | 500 | Important titles |
| Title Large Emphasized | 22 | 600 | Critical headers |
| Body Large Emphasized | 16 | 500 | Important paragraphs |
| Label Large Emphasized | 14 | 600 | Primary buttons |

### Typography Token Naming

Pattern: `--md-sys-typescale-{role}-{property}`

```
--md-sys-typescale-display-large-font-family-name
--md-sys-typescale-display-large-font-size
--md-sys-typescale-display-large-line-height
--md-sys-typescale-display-large-font-weight
--md-sys-typescale-display-large-letter-spacing
```

### Custom Typeface Implementation

To replace Roboto with brand typeface:

1. Register fonts in platform (MauiProgram.cs, Info.plist, etc.)
2. Override reference tokens:
```
--md-ref-typeface-brand: 'YourBrandFont'
--md-ref-typeface-plain: 'YourBodyFont'
```
3. Assign to Display/Headline (brand) and Body/Title/Label (plain)

---

## Shape System

### Standard Shape Scale (7 Tokens)

| Token | Radius | Usage |
|-------|--------|-------|
| None | 0dp | Rectangular elements |
| Extra-small | 4dp | Text fields (top corners) |
| Small | 8dp | Buttons, chips |
| Medium | 12dp | Cards, dialogs |
| Large | 16dp | FAB, large cards |
| Extra-large | 28dp | Bottom sheets |
| Full | 9999dp | Pills, fully rounded |

### M3 Expressive: 35 Decorative Shapes (2025)

New shape library beyond rounded rectangles:

**Organic Shapes:**
- Blob, Clover, Flower, Leaf, Oval, Pebble, Slanted

**Geometric Shapes:**
- Arrow, Diamond, Hexagon, Octagon, Pentagon, Parallelogram
- Pill, Rectangle, RoundedRectangle, Square, Triangle

**Decorative Shapes:**
- Arch, Bevel, Cookie, Fan, Ghost, Heart
- Slime, Spike, Sunny, Very Round, Wavy

**Asymmetric Shapes:**
- Cookie Four, Boom, Arch variations

### Shape Application by Component

| Component | Default Shape | Token |
|-----------|---------------|-------|
| Filled Button | Full | shape-corner-full |
| FAB | Large | shape-corner-large |
| Card | Medium | shape-corner-medium |
| Text Field | Extra-small (top) | shape-corner-extra-small |
| Dialog | Extra-large | shape-corner-extra-large |
| Chip | Small | shape-corner-small |
| Bottom Sheet | Extra-large (top) | shape-corner-extra-large |

### Shape Morphing (M3 Expressive)

Shapes can animate between states:

| Transition | From | To | Duration |
|------------|------|-----|----------|
| FAB expand | Large rounded | Extra-large | 300ms |
| Card select | Medium | Shape highlight | 200ms |
| Chip select | Small | Full | 150ms |

### Shape State Correlation

Link shapes to interaction states:

| State | Shape Modification |
|-------|-------------------|
| Default | Base shape |
| Pressed | Slightly smaller radius |
| Selected | Different shape family |
| Expanded | Larger/different shape |

---

## Elevation System

### Six Discrete Levels

| Level | dp | Shadow | Use Cases |
|-------|-----|--------|-----------|
| 0 | 0 | None | Flat surfaces, filled buttons |
| 1 | 1 | Soft | Elevated cards, search bars |
| 2 | 3 | Light | Navigation bar, menus |
| 3 | 6 | Medium | FABs, snackbars |
| 4 | 8 | Strong | Picked-up cards, dragged items |
| 5 | 12 | Heavy | Dialogs, modal sheets |

### Surface Container Mapping

| Elevation Level | Surface Container | Use |
|-----------------|-------------------|-----|
| Level 0 | Surface | Flat content |
| Level 1 | SurfaceContainerLow | Subtle elevation |
| Level 2 | SurfaceContainer | Standard cards |
| Level 3 | SurfaceContainerHigh | Prominent surfaces |
| Level 4-5 | SurfaceContainerHighest | Modals, dialogs |

### Shadow Specifications (CSS)

```css
/* Level 1 */
box-shadow: 0 1px 2px rgba(0,0,0,0.3), 0 1px 3px 1px rgba(0,0,0,0.15);

/* Level 2 */
box-shadow: 0 1px 2px rgba(0,0,0,0.3), 0 2px 6px 2px rgba(0,0,0,0.15);

/* Level 3 */
box-shadow: 0 1px 3px rgba(0,0,0,0.3), 0 4px 8px 3px rgba(0,0,0,0.15);

/* Level 4 */
box-shadow: 0 2px 3px rgba(0,0,0,0.3), 0 6px 10px 4px rgba(0,0,0,0.15);

/* Level 5 */
box-shadow: 0 4px 4px rgba(0,0,0,0.3), 0 8px 12px 6px rgba(0,0,0,0.15);
```

### Tonal Elevation (Dark Mode)

In dark mode, elevation is expressed through surface tint overlay:

| Level | Tint Opacity |
|-------|--------------|
| 0 | 0% |
| 1 | 5% |
| 2 | 8% |
| 3 | 11% |
| 4 | 12% |
| 5 | 14% |

---

## Motion System

### Traditional Duration Tokens (ms)

| Category | Token | Value |
|----------|-------|-------|
| Short | Short1 | 50 |
| Short | Short2 | 100 |
| Short | Short3 | 150 |
| Short | Short4 | 200 |
| Medium | Medium1 | 250 |
| Medium | Medium2 | 300 |
| Medium | Medium3 | 350 |
| Medium | Medium4 | 400 |
| Long | Long1 | 450 |
| Long | Long2 | 500 |
| Long | Long3 | 550 |
| Long | Long4 | 600 |
| Extra Long | ExtraLong1 | 700 |
| Extra Long | ExtraLong2 | 800 |
| Extra Long | ExtraLong3 | 900 |
| Extra Long | ExtraLong4 | 1000 |

### Traditional Easing Curves (cubic-bezier)

| Curve | Value | Use |
|-------|-------|-----|
| Standard | (0.2, 0, 0, 1) | Default motion |
| Standard Accelerate | (0.3, 0, 1, 1) | Exiting elements |
| Standard Decelerate | (0, 0, 0, 1) | Entering elements |
| Emphasized | (0.2, 0, 0, 1) | Hero moments |
| Emphasized Accelerate | (0.3, 0, 0.8, 0.15) | Dramatic exits |
| Emphasized Decelerate | (0.05, 0.7, 0.1, 1) | Dramatic entrances |

### M3 Expressive: Physics-Based Springs (2025)

Replaces traditional easing with spring physics for more natural motion.

**Spatial Springs** (position, size, shape changes):

| Preset | Damping | Stiffness | Use |
|--------|---------|-----------|-----|
| Default Spatial | ~0.9 | Medium | General movement |
| Fast Spatial | ~0.85 | High | Quick responses |
| Slow Spatial | ~0.95 | Low | Gentle transitions |

**Effects Springs** (color, opacity changes):

| Preset | Damping | Stiffness | Use |
|--------|---------|-----------|-----|
| Default Effects | ~0.7 | Medium | Color transitions |
| Fast Effects | ~0.6 | High | Quick fades |
| Slow Effects | ~0.8 | Low | Subtle changes |

**Spring Parameters:**

| Parameter | Description | Range |
|-----------|-------------|-------|
| Damping Ratio | Oscillation decay | 0-1 (1=no bounce) |
| Stiffness | Spring tension | 100-1000 |
| Mass | Element weight | 0.5-2.0 |

### Motion Schemes

| Scheme | Character | Use Case |
|--------|-----------|----------|
| Expressive | Bouncy, playful | Consumer apps, games |
| Standard | Subdued, professional | Productivity, business |

### Transition Patterns

**Container Transform:**
- Duration: 300ms
- Morphs container shape and size
- Use: Card-to-detail, FAB-to-fullscreen

**Shared Axis:**
- Duration: 300ms
- Displacement: 30dp
- Axes: X (horizontal), Y (vertical), Z (depth)
- Use: Pagination, navigation with spatial relationship

**Fade Through:**
- Duration: 300ms (90ms out + 210ms in)
- Scale: 92% → 100%
- Use: Unrelated content swaps

**Fade:**
- Duration: Variable
- Simple opacity transition
- Use: Overlays, tooltips

---

## Icons

### Material Symbols

Variable icon font with four adjustable axes:

| Axis | Range | Default | Description |
|------|-------|---------|-------------|
| Fill | 0-1 | 0 | Filled vs outlined |
| Weight | 100-700 | 400 | Stroke thickness |
| Grade | -25 to 200 | 0 | Emphasis level |
| Optical Size | 20-48dp | 24 | Size optimization |

### Icon Styles

| Style | Character | Use Case |
|-------|-----------|----------|
| Outlined | Clean, modern | Default choice |
| Rounded | Friendly, soft | Consumer apps |
| Sharp | Technical, precise | Productivity apps |

### Standard Icon Sizes

| Size | Touch Target | Use Case |
|------|--------------|----------|
| 20dp | 40dp | Dense layouts |
| 24dp | 48dp | Default (most common) |
| 40dp | 48dp | Emphasis, larger UI |
| 48dp | 56dp | FAB icons, large buttons |

### Icon Application Guidelines

| Context | Size | Weight | Fill |
|---------|------|--------|------|
| Navigation Bar | 24dp | 400 | Selected: 1, Unselected: 0 |
| App Bar Actions | 24dp | 400 | 0 |
| FAB | 24dp (Standard), 36dp (Large) | 400 | 0 |
| List Leading | 24dp | 400 | 0 |
| Button with Icon | 18dp | 500 | 0 |
| Chip | 18dp | 400 | 0 |

### Icon + Text Spacing

| Context | Gap |
|---------|-----|
| Button | 8dp |
| Chip | 8dp |
| List Item | 16dp |
| Navigation Item | 4dp (vertical) |

---

## Layout System

### Window Size Classes

| Class | Width | Navigation Pattern | Columns |
|-------|-------|-------------------|---------|
| Compact | <600dp | Navigation Bar (bottom) | 4 |
| Medium | 600-839dp | Navigation Rail | 8 |
| Expanded | 840-1199dp | Rail + Modal Drawer | 12 |
| Large | 1200-1599dp | Standard Drawer | 12 |
| Extra-Large | ≥1600dp | Permanent Drawer | 12 |

### Grid System

| Class | Columns | Margins | Gutters |
|-------|---------|---------|---------|
| Compact | 4 | 16dp | 16dp |
| Medium | 8 | 24dp | 24dp |
| Expanded | 12 | 32dp | 24dp |
| Large | 12 | 32dp | 24dp |
| Extra-Large | 12 | 32dp | 24dp |

### Spacing Scale

| Token | Value | Use |
|-------|-------|-----|
| spacing-none | 0dp | No space |
| spacing-extra-small | 4dp | Tight grouping |
| spacing-small | 8dp | Related items |
| spacing-medium | 16dp | Default spacing |
| spacing-large | 24dp | Section separation |
| spacing-extra-large | 32dp | Major sections |

### Canonical Layouts

**List-Detail:**
- Compact: Single pane (list OR detail)
- Medium+: Side-by-side (list 33%, detail 67%)
- Transition: Shared axis on Z

**Supporting Pane:**
- Compact: Stacked vertically
- Expanded: 70/30 horizontal split
- Use: Main content + related info

**Feed:**
- Compact: Single column, full-width cards
- Medium: 2 columns
- Expanded: 3+ columns with masonry option

### Layout Regions

| Region | Purpose | Example |
|--------|---------|---------|
| App Bar | Navigation, actions | Top app bar |
| Body | Primary content | List, detail view |
| Navigation | Route selection | Nav bar, rail, drawer |
| FAB | Primary action | Floating button |

### Bidirectionality (RTL Support)

**Mirrored elements:**
- Navigation icons (arrows, chevrons)
- Horizontal layouts
- Text alignment
- Swipe directions

**Non-mirrored elements:**
- Media playback controls
- Clocks, timelines
- Phone number formats
- Brand logos

---

## Interaction Patterns

### Interaction States

| State | Overlay Opacity | Trigger |
|-------|-----------------|---------|
| Enabled | 0% | Default |
| Hover | 8% | Cursor over (desktop) |
| Focus | 10% | Keyboard navigation |
| Pressed | 12% | Active tap/click |
| Dragged | 16% | Being moved |
| Disabled (content) | 38% | Not interactive |
| Disabled (container) | 12% | Not interactive |

**Rule:** Only one state layer applies at a time.

### Selection Patterns

**Single Selection:**
- Radio buttons for mutually exclusive
- Checkboxes for independent items
- Visual: Primary color fill

**Multiple Selection:**
- Checkboxes
- Long-press to enter selection mode
- Selection count in app bar

**Range Selection:**
- Shift+click for range
- Touch: Long-press start, tap end

### Gesture Specifications

| Gesture | Threshold | Duration | Use |
|---------|-----------|----------|-----|
| Tap | <10dp movement | <500ms | Primary action |
| Long-press | Stationary | 500ms | Secondary action |
| Swipe | >56dp horizontal | <300ms | Dismiss, reveal |
| Drag | Any distance | Ongoing | Move, reorder |
| Pinch | Two fingers | Ongoing | Zoom |

### Input Methods

| Method | Considerations |
|--------|---------------|
| Touch | 48dp targets, swipe gestures |
| Mouse | Hover states, right-click context |
| Keyboard | Focus indicators, shortcuts |
| Trackpad | Scroll gestures, precision |
| Stylus | Pressure sensitivity, palm rejection |

---

## Accessibility

### Contrast Requirements

| Content Type | Ratio | WCAG Level |
|--------------|-------|------------|
| Normal text (<18sp) | 4.5:1 | AA |
| Large text (≥18sp or 14sp bold) | 3:1 | AA |
| UI components | 3:1 | AA |
| Graphical objects | 3:1 | AA |
| Enhanced (any text) | 7:1 | AAA |

### Touch Target Requirements

| Requirement | Value |
|-------------|-------|
| Minimum size | 48×48dp |
| Minimum spacing | 8dp between targets |
| Recommended size | 56×56dp for primary actions |

### Focus Indicators

| Property | Specification |
|----------|---------------|
| Style | 2dp solid outline |
| Color | Primary or high-contrast |
| Contrast | 3:1 against adjacent colors |
| Offset | 2dp from element edge |

### Screen Reader Requirements

**Structure:**
- Semantic headings (h1-h6 hierarchy)
- Landmark regions (main, nav, aside)
- Logical reading order

**Element Labeling:**
- All interactive elements need labels
- Icons need accessible names
- Form fields need associated labels
- Images need alt text

### Motion Accessibility

**Reduced Motion:**
```css
@media (prefers-reduced-motion: reduce) {
  /* Replace animations with instant transitions */
  /* Or use simple fades */
}
```

**Guidelines:**
- Provide system setting respect
- Avoid auto-playing animations
- Allow pause/stop for moving content
- No flashing >3 times/second

### Color Independence

Never use color alone to convey:
- Error states (add icon + text)
- Required fields (add asterisk)
- Success/failure (add icon)
- Links (add underline)
- Data visualization (add patterns)

### Text Accessibility

| Requirement | Specification |
|-------------|---------------|
| Minimum size | 12sp |
| Recommended minimum | 14sp |
| Line length | 45-75 characters |
| Line height | 1.5× font size minimum |
| Paragraph spacing | 1.5× line height |

---

## Components

### Buttons

**All Button Variants:**
- Height: 40dp
- Shape: Full rounded
- Horizontal padding: 24dp (16dp with icon)
- Icon size: 18dp
- Icon-text gap: 8dp

| Variant | Container | Label Color | Elevation |
|---------|-----------|-------------|-----------|
| Filled | Primary | OnPrimary | 0 |
| Filled Tonal | SecondaryContainer | OnSecondaryContainer | 0 |
| Elevated | SurfaceContainerLow | Primary | 1 |
| Outlined | Transparent + 1dp outline | Primary | 0 |
| Text | Transparent | Primary | 0 |

### Icon Buttons

| Variant | Container | Icon Color | Size |
|---------|-----------|------------|------|
| Standard | Transparent | OnSurfaceVariant | 48dp touch, 24dp icon |
| Filled | Primary | OnPrimary | 48dp touch, 24dp icon |
| Filled Tonal | SecondaryContainer | OnSecondaryContainer | 48dp touch |
| Outlined | Transparent + 1dp outline | OnSurfaceVariant | 48dp touch |

### Segmented Buttons

- Height: 40dp
- Shape: Full rounded (group), squared (inner dividers)
- Selected indicator: SecondaryContainer fill
- Minimum segments: 2
- Maximum segments: 5

### FAB (Floating Action Button)

| Size | Dimensions | Icon | Corner Radius |
|------|------------|------|---------------|
| Small | 40×40dp | 24dp | 12dp |
| Standard | 56×56dp | 24dp | 16dp |
| Large | 96×96dp | 36dp | 28dp |

**Extended FAB:**
- Height: 56dp
- Horizontal padding: 16dp
- Icon-label gap: 8dp
- Minimum width: 80dp

**Placement:**
- 16dp from edges (compact)
- 24dp from edges (medium+)
- Avoid overlap with navigation

### Chips

**All Chips:**
- Height: 32dp
- Corner radius: 8dp
- Horizontal padding: 16dp (12dp with leading icon)
- Icon size: 18dp

| Type | Purpose | Selection Indicator |
|------|---------|---------------------|
| Assist | Automated actions | None |
| Filter | Content filtering | Checkmark |
| Input | User-entered tags | Trailing remove (X) |
| Suggestion | Dynamic suggestions | None |

### Checkbox

| Property | Specification |
|----------|---------------|
| Container | 18×18dp |
| Corner radius | 2dp |
| Touch target | 48×48dp |
| Checkmark stroke | 2dp |
| States | Unchecked, Checked, Indeterminate, Error |

**Colors:**
- Unchecked: OnSurfaceVariant (outline)
- Checked: Primary (fill), OnPrimary (checkmark)
- Indeterminate: Primary (fill), OnPrimary (dash)
- Error: Error (outline), Error (fill when checked)

### Radio Button

| Property | Specification |
|----------|---------------|
| Outer circle | 20dp diameter, 2dp stroke |
| Inner dot | 10dp diameter |
| Touch target | 48×48dp |
| Animation | 100ms, standard easing |

**Colors:**
- Unselected: OnSurfaceVariant (outline)
- Selected: Primary (outline + dot)

### Switch

| Property | Specification |
|----------|---------------|
| Track | 52×32dp |
| Thumb (off) | 24dp diameter |
| Thumb (on) | 28dp diameter |
| Touch target | 48×48dp |

**Colors:**
- Track off: SurfaceContainerHighest
- Track on: Primary
- Thumb off: Outline
- Thumb on: OnPrimary

**Optional:** Icon in thumb (16dp)

### Sliders

| Property | Specification |
|----------|---------------|
| Track height | 4dp |
| Thumb | 20dp diameter |
| Touch target | 48dp |
| Value indicator | 28dp height |
| Discrete tick marks | 2dp×2dp |

**Types:**
- Continuous: Smooth value selection
- Discrete: Stepped values with tick marks
- Centered: Zero point in middle
- Range: Two thumbs for min/max

### Text Fields

| Property | Specification |
|----------|---------------|
| Height | 56dp (44dp dense) |
| Typography | Body Large |
| Corner radius | 4dp (top corners for filled) |

| Variant | Container | Active Indicator |
|---------|-----------|------------------|
| Filled | SurfaceContainerHighest | Bottom 2dp, Primary |
| Outlined | Transparent | All sides 1dp (2dp focused) |

**States:** Enabled, Focused, Error, Disabled

**Anatomy:**
- Leading icon (optional): 24dp
- Label text: Animates on focus
- Supporting text: Below field
- Trailing icon (optional): 24dp
- Character counter (optional)

### Cards

| Type | Container | Elevation | Outline |
|------|-----------|-----------|---------|
| Elevated | SurfaceContainerLow | Level 1 | None |
| Filled | SurfaceContainerHighest | Level 0 | None |
| Outlined | Surface | Level 0 | 1dp Outline |

**Specifications:**
- Corner radius: 12dp (Medium)
- Content padding: 16dp
- Minimum touch target: 48×48dp (if interactive)

**Content Zones:**
- Media area (top)
- Header (title, subtitle)
- Supporting text
- Action area (buttons, icons)

### Lists

| Variant | Height | Leading | Trailing |
|---------|--------|---------|----------|
| One-line | 56dp | Optional | Optional |
| Two-line | 72dp | Optional | Optional |
| Three-line | 88dp | Optional | Optional |

**Leading element types:**
- Icon (24dp)
- Avatar (40dp)
- Image (56dp square)
- Checkbox/Radio

**Trailing element types:**
- Icon (24dp)
- Text (supporting info)
- Checkbox/Switch
- Drag handle

**Dividers:**
- Full-bleed: Edge to edge
- Inset: Aligned with text start
- Height: 1dp

### Menus

| Property | Specification |
|----------|---------------|
| Minimum width | 112dp |
| Maximum width | 280dp |
| Item height | 48dp |
| Vertical padding | 8dp |
| Corner radius | 4dp (Extra-small) |
| Elevation | Level 2 (3dp) |

**Types:**
- Dropdown: Attached to trigger
- Context: Right-click/long-press
- Overflow: More actions menu

**Cascading:** Submenus open to side with 8dp offset

### Tooltips

**Plain Tooltip:**
- Max width: 200dp
- Padding: 4dp vertical, 8dp horizontal
- Corner radius: 4dp
- Background: InverseSurface
- Text: InverseOnSurface, Body Small
- Trigger delay: 500ms hover

**Rich Tooltip:**
- Max width: 320dp
- Supports: Title, body text, actions
- Persistent until dismissed
- Corner radius: 12dp

### Search

**Search Bar:**
- Height: 56dp
- Corner radius: 28dp (Full)
- Container: SurfaceContainerHigh

**Search View (Expanded):**
- Full-screen on compact
- Docked on medium+
- Shows recent searches, suggestions

### Dialogs

| Property | Specification |
|----------|---------------|
| Width | 280-560dp |
| Corner radius | 28dp |
| Padding | 24dp |
| Container | SurfaceContainerHigh |
| Elevation | Level 3 |

**Anatomy:**
- Icon (optional): 24dp, centered
- Headline: Headline Small
- Supporting text: Body Medium
- Actions: Text buttons, right-aligned
- Divider (optional): Above actions

**Full-screen Dialog:**
- Used on compact screens
- Has close/back button
- Title in app bar

### Bottom Sheets

| Property | Specification |
|----------|---------------|
| Corner radius (top) | 28dp |
| Handle (optional) | 32×4dp, centered, 22dp from top |
| Container | SurfaceContainerLow |
| Max height | 90% of screen |

**Types:**
- Standard: Coexists with main content
- Modal: Blocks main content, has scrim

### Side Sheets

| Property | Specification |
|----------|---------------|
| Width | 256-400dp |
| Corner radius | 0 (docked), 16dp (modal) |
| Container | Surface |

**Types:**
- Standard: Persistent alongside content
- Modal: Overlays with scrim

### Navigation Bar (Bottom)

| Property | Specification |
|----------|---------------|
| Height | 80dp |
| Container | SurfaceContainer |
| Active indicator | 64×32dp pill |
| Destinations | 3-5 |

**Item anatomy:**
- Icon: 24dp
- Label: Label Medium
- Indicator: SecondaryContainer

### Navigation Rail

| Property | Specification |
|----------|---------------|
| Width | 80dp (collapsed), 256-360dp (expanded) |
| Active indicator | 56×32dp pill |
| Destinations | 3-7 |
| FAB position | Top (optional) |

### Navigation Drawer

| Property | Specification |
|----------|---------------|
| Width | 360dp |
| Item height | 56dp |
| Active indicator | 28dp full-rounded |
| Container | SurfaceContainerLow |

**Types:**
- Standard: Persistent
- Modal: Overlay with scrim

### Top App Bar

| Size | Height | Collapse Behavior |
|------|--------|-------------------|
| Center-aligned | 64dp | Fixed |
| Small | 64dp | Fixed |
| Medium | 112dp | Collapses to 64dp |
| Large | 152dp | Collapses to 64dp |

**Anatomy:**
- Navigation icon: 48×48dp touch
- Title: Title Large (or Headline Small for Medium/Large)
- Action icons: Up to 3 visible

### Tabs

| Property | Specification |
|----------|---------------|
| Height | 48dp (64dp with icons) |
| Primary indicator | 3dp height |
| Secondary indicator | 2dp height |
| Minimum width | 90dp |
| Maximum width | 360dp |

**Types:**
- Primary: For main navigation
- Secondary: For content filtering

### Snackbar

| Property | Specification |
|----------|---------------|
| Minimum width | 344dp |
| Maximum width | 568dp |
| Corner radius | 4dp |
| Container | InverseSurface |
| Text | InverseOnSurface |
| Duration | 4-10 seconds |

**Placement:**
- 8dp from edges
- Above FAB and navigation

### Bottom App Bar

| Property | Specification |
|----------|---------------|
| Height | 80dp |
| Container | SurfaceContainer |
| FAB cutout | Optional embedded FAB |
| Action icons | Left-aligned |

### Divider

| Property | Specification |
|----------|---------------|
| Height | 1dp |
| Color | OutlineVariant |
| Types | Full-bleed, Inset, Vertical |

### Progress Indicators

**Linear:**
- Height: 4dp
- Default width: 240dp
- Track: SurfaceContainerHighest
- Indicator: Primary

**Circular:**
- Default size: 48dp
- Stroke width: 8.33% of diameter
- Track: SurfaceContainerHighest
- Indicator: Primary

**States:**
- Determinate: Shows progress percentage
- Indeterminate: Animated, unknown duration

### Date Pickers

| Type | Width | Use |
|------|-------|-----|
| Modal | 328dp | Most common |
| Docked | Inline | Embedded in forms |
| Input | Text field | Quick entry |

**Calendar specifications:**
- Day cell: 40×40dp
- Selected: Primary fill
- Today: Primary outline
- Range: PrimaryContainer fill

### Time Pickers

| Type | Specification | Use |
|------|---------------|-----|
| Dial | 256dp diameter | Touch/visual |
| Input | 96×72dp fields | Keyboard |

### Carousel

| Property | Specification |
|----------|---------------|
| Item aspect ratios | 3:4, 1:1, 4:3, 16:9 |
| Visible items | 1-3 depending on size |
| Indicator | Dot row below |
| Scroll physics | Snap to item |

---

## M3 Expressive Components (2025)

### FAB Menu

Expandable FAB with action menu:

| Property | Specification |
|----------|---------------|
| Anchor FAB sizes | Small, Medium, Large |
| Menu panel | Above or beside FAB |
| Color sets | 3 options |
| Animation | 300ms spring |

**Behavior:**
- Tap FAB to expand menu
- Tap action or outside to collapse
- Replaces "speed dial" pattern

### Split Button

Two-part button with primary action and dropdown:

| Property | Specification |
|----------|---------------|
| Height | 40dp |
| Divider | 1dp vertical |
| Primary zone | Left (main action) |
| Secondary zone | Right (dropdown trigger) |

### Floating Toolbar

Contextual action bar:

| Property | Specification |
|----------|---------------|
| Shape | Pill (full rounded) |
| Height | 56dp |
| Position | Floating above content |
| Actions | 2-5 icons |

**Use cases:**
- Photo editing
- Document actions
- Selection actions

### Button Groups

Connected button sets:

| Property | Specification |
|----------|---------------|
| Shapes | Leading, middle, trailing |
| Orientation | Horizontal or vertical |
| Selection | Single or multiple |

### Loading Indicator

Shape-based loading animation:

| Property | Specification |
|----------|---------------|
| Animation | Cycles through M3E shapes |
| Contained variant | Custom color background |
| Modes | Determinate, indeterminate |

### Rich Tooltips

Enhanced tooltips with more content:

| Property | Specification |
|----------|---------------|
| Max width | 320dp |
| Content | Title, body, actions |
| Persistence | Until dismissed |
| Corner radius | 12dp |

---

## Content Design

### UX Writing Principles

| Principle | Description |
|-----------|-------------|
| Clear | Use simple, direct language |
| Concise | Remove unnecessary words |
| Useful | Provide actionable information |
| Conversational | Use natural tone |

### Microcopy Guidelines

| Element | Max Length | Tone |
|---------|------------|------|
| Button | 2-3 words | Action-oriented |
| Toast | 1 line | Informative |
| Error | 2 lines | Helpful, not blaming |
| Empty state | 2-3 lines | Encouraging |

### Notification Content

| Component | Character Limit |
|-----------|-----------------|
| Title | 40 characters |
| Body | 90 characters |
| Action | 12 characters |

### Alt Text Guidelines

**Images:**
- Describe content, not appearance
- Include relevant text in image
- Skip decorative images (empty alt)

**Icons:**
- Describe action, not icon
- "Settings" not "Gear icon"

### Grammar and Punctuation

| Rule | Example |
|------|---------|
| Sentence case | "Save changes" not "Save Changes" |
| No periods in buttons | "Submit" not "Submit." |
| Use contractions | "You'll" not "You will" |
| Active voice | "Save your work" not "Your work will be saved" |

### Global Writing

| Consideration | Guideline |
|---------------|-----------|
| Date format | Use locale-appropriate |
| Number format | Respect decimal separators |
| Currency | Show local currency first |
| Avoid idioms | May not translate |
| Avoid humor | Cultural sensitivity |

---

## Platform Implementation

### Android (Jetpack Compose)

**Dependency:**
```kotlin
implementation("androidx.compose.material3:material3:1.4.0")
```

**Theme setup:**
```kotlin
MaterialTheme(
    colorScheme = dynamicDarkColorScheme(context),
    typography = Typography,
    shapes = Shapes
) { content() }
```

**M3 Expressive:**
```kotlin
@OptIn(ExperimentalMaterial3ExpressiveApi::class)
```

### Android (MDC-Android)

**Theme:**
```xml
<style name="AppTheme" parent="Theme.Material3.DayNight">
```

**Requirements:**
- Minimum SDK: 21
- Compile SDK: 35

### Flutter

**Enable M3:**
```dart
ThemeData(useMaterial3: true)
```

**Color generation:**
```dart
ColorScheme.fromSeed(seedColor: Colors.blue)
```

### .NET MAUI

**Libraries:**
- UraniumUI 2.14 (TextField, DataGrid, validation)
- HorusSoftware.Maui.MaterialDesignControls 10.0 (FAB, Cards, Snackbar)

**Setup:** See DesignSystem_Implementation_Guide.md

### Web

**Package:**
```
npm install @material/web
```

**Note:** Currently in maintenance mode.

---

## Tools and Resources

### Material Theme Builder

**Web:** https://m3.material.io/theme-builder

**Features:**
- Visual color customization
- Dynamic color preview
- Export to multiple formats

**Export formats:**
- Android (Kotlin, XML)
- iOS (Swift)
- Web (CSS)
- Flutter (Dart)
- JSON DSP

### Material Symbols

**Web:** https://fonts.google.com/icons

**Formats:**
- Variable font (recommended)
- Static fonts
- SVG
- PNG

### Figma Resources

**Material 3 Design Kit:**
- Components library
- Color styles
- Typography styles
- Layout templates

---

## Critical Values Summary

| Specification | Value |
|---------------|-------|
| Minimum touch target | 48×48dp |
| Touch target spacing | 8dp |
| Text contrast (normal) | 4.5:1 |
| Text contrast (large) | 3:1 |
| UI component contrast | 3:1 |
| Button height | 40dp |
| FAB size (standard) | 56×56dp |
| FAB size (small) | 40×40dp |
| FAB size (large) | 96×96dp |
| Chip height | 32dp |
| Text field height | 56dp |
| App bar height | 64dp |
| Navigation bar height | 80dp |
| Compact breakpoint | 600dp |
| Medium breakpoint | 840dp |
| Expanded breakpoint | 1200dp |
| Pressed state opacity | 12% |
| Hover state opacity | 8% |
| Focus state opacity | 10% |
| Standard transition | 300ms |
| Long-press threshold | 500ms |
| Swipe threshold | 56dp |
| Grid margins (compact) | 16dp |
| Grid margins (expanded) | 32dp |

---

## Glossary

| Term | Definition |
|------|------------|
| **Chroma** | Color intensity in HCT color space |
| **Container** | Background surface of a component |
| **Dynamic Color** | Colors derived from user content/wallpaper |
| **Elevation** | Visual layering through shadow/tint |
| **FAB** | Floating Action Button |
| **HCT** | Hue-Chroma-Tone color space |
| **M3 Expressive** | 2025 update with enhanced visual expression |
| **On-color** | Text/icon color on a specific background |
| **Seed Color** | Source color for generating palette |
| **State Layer** | Overlay indicating interaction state |
| **Surface** | Background color for content |
| **Tonal Palette** | 13 tones generated from a color |
| **Tone** | Lightness value (0-100) in HCT |
| **Touch Target** | Minimum tappable area |
| **Type Scale** | Predefined typography styles |

---

> **Document Version:** 2.0  
> **Last Updated:** January 2026  
> **Includes:** M3 Expressive (May 2025)  
> **Reference:** https://m3.material.io
