---
name: Neon Nightclub MD3
colors:
  surface: '#131318'
  surface-dim: '#131318'
  surface-bright: '#39383e'
  surface-container-lowest: '#0e0e13'
  surface-container-low: '#1b1b20'
  surface-container: '#1f1f25'
  surface-container-high: '#2a292f'
  surface-container-highest: '#35343a'
  on-surface: '#e4e1e9'
  on-surface-variant: '#e4bdc3'
  inverse-surface: '#e4e1e9'
  inverse-on-surface: '#303036'
  outline: '#ab888e'
  outline-variant: '#5b3f44'
  surface-tint: '#ffb1c0'
  primary: '#ffb1c0'
  on-primary: '#660029'
  primary-container: '#ff4c83'
  on-primary-container: '#5a0023'
  inverse-primary: '#bc0051'
  secondary: '#deb7ff'
  on-secondary: '#4a007f'
  secondary-container: '#6a17ad'
  on-secondary-container: '#d4a5ff'
  tertiary: '#e9c400'
  on-tertiary: '#3a3000'
  tertiary-container: '#c8a900'
  on-tertiary-container: '#4b3e00'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#ffd9df'
  primary-fixed-dim: '#ffb1c0'
  on-primary-fixed: '#3f0017'
  on-primary-fixed-variant: '#90003d'
  secondary-fixed: '#f0dbff'
  secondary-fixed-dim: '#deb7ff'
  on-secondary-fixed: '#2c0050'
  on-secondary-fixed-variant: '#6712aa'
  tertiary-fixed: '#ffe16d'
  tertiary-fixed-dim: '#e9c400'
  on-tertiary-fixed: '#221b00'
  on-tertiary-fixed-variant: '#544600'
  background: '#131318'
  on-background: '#e4e1e9'
  surface-variant: '#35343a'
typography:
  display-lg:
    fontFamily: Roboto
    fontSize: 57px
    fontWeight: '700'
    lineHeight: 64px
    letterSpacing: -0.25px
  headline-lg:
    fontFamily: Roboto
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
  headline-lg-mobile:
    fontFamily: Roboto
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
  title-lg:
    fontFamily: Roboto
    fontSize: 22px
    fontWeight: '500'
    lineHeight: 28px
  body-lg:
    fontFamily: Roboto
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
    letterSpacing: 0.5px
  body-md:
    fontFamily: Roboto
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
    letterSpacing: 0.25px
  label-lg:
    fontFamily: Roboto
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.1px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  margin-mobile: 16px
  margin-desktop: 24px
  gutter: 12px
  stack-sm: 4px
  stack-md: 16px
  stack-lg: 32px
---

## Brand & Style

This design system is built for high-energy, low-light environments where legibility and "vibe" are equally critical. It adopts a **High-Contrast / Bold** aesthetic rooted in **Material Design 3 (MD3)** logic, optimized for a permanent dark-mode state. 

The brand personality is electric, professional, and rhythmic. It mimics the atmosphere of a premium nightclub—pulsating with light against a void of deep black. The visual language uses neon accents as functional signifiers rather than mere decoration, ensuring that the interface remains a professional tool for event management while feeling native to the nightlife scene.

**Key Principles:**
- **Luminosity over Mass:** Elements should feel like light sources rather than solid blocks.
- **Strict Darkness:** Surfaces must maintain a deep black base to prevent screen glare in dark venues.
- **Urgency & Energy:** High-contrast colors guide the eye to the most important actions in a fast-paced environment.

## Colors

The palette is strictly dark-mode, leveraging high-chroma neon seeds to ensure WCAG AA compliance against a `#0a0a0f` background.

- **Primary (Hot Pink Neon):** Used for the most critical actions, like "Add to Queue" or "Start Performance." It represents the energy of the lead singer.
- **Secondary (Electric Violet):** Used for navigation, selection states, and secondary management features. It represents the ambient mood of the venue.
- **Tertiary (Neon Gold):** Reserved for VIP status, high-priority queue items, or spotlight alerts.
- **Neutral/Surface:** A true "Deep Black" foundation. Surfaces should not use standard MD3 grey elevations; instead, they use very subtle primary/secondary tints at low opacity (2-5%) to maintain the deep black feel while providing depth.

## Typography

The typography strategy prioritizes universal legibility and functional clarity in low-light settings using a unified typeface.

- **Typeface:** The system uses **Roboto** across all levels. Its familiar, neutral grotesque forms provide high legibility for both large headlines and dense technical data.
- **Headlines:** Large display and headline sizes use bold and semi-bold weights of Roboto to command attention and maintain clarity from a distance.
- **Body & Utility:** Roboto's regular weights are used for technical data like queue positions and menu settings.
- **Weight Strategy:** Use medium and semi-bold weights more frequently than regular weights to combat the "haloing" effect of light text on black backgrounds.

## Layout & Spacing

The design system follows the MD3 **8dp grid** for all spatial relationships. 

- **Fluid Grid:** The layout expands to fill the mobile width, utilizing a 4-column structure for mobile and 12-column for tablet/desktop.
- **Safe Areas:** Padding must be strictly enforced around the edges of the device to prevent "light bleed" from the neon elements into the device's physical bezel.
- **Rhythm:** Vertical spacing should be generous (`stack-md` or `stack-lg`) to prevent the high-contrast elements from feeling cluttered.

## Elevation & Depth

In a deep black environment, traditional drop shadows are invisible. This design system uses **Tonal Glows** and **Outlines** to communicate hierarchy:

1.  **Level 0 (Base):** Deep black `#0a0a0f`.
2.  **Level 1 (Cards/Lists):** Surface-variant color or a 1px `outline` at 20% opacity.
3.  **Level 2 (Active States/Floating):** A subtle outer glow (4-8px blur) using the `primary` or `secondary` color at very low opacity (10-15%).
4.  **Glassmorphism:** Use Backdrop Blurs (10px - 20px) on top-level navigation bars and modals to maintain the sense of depth while allowing the neon colors of the content below to "bleed" through.

## Shapes

The shape language follows the **Rounded** (Level 2) logic of MD3, creating a balance between modern tech and approachable design.

- **Small Components (Buttons, Inputs):** 0.5rem (8px).
- **Medium Components (Cards, Modals):** 1rem (16px).
- **Large Components (Navigation Drawers):** 1.5rem (24px).
- **Pills:** Full rounding is used exclusively for "Status Chips" (e.g., "Now Playing" or "Up Next").

## Components

**Buttons**
- **Primary:** Filled Hot Pink (`primary`) with White text. High-contrast and impossible to miss.
- **Secondary:** Outlined with Electric Violet (`secondary`). Use for "Cancel" or "Edit."

**Chips**
- Used for song genres or queue status. Use the `secondary-container` color for the background with `on-secondary-container` text to provide a clear but non-distracting visual tag.

**Lists & Queue Items**
- Each list item should have a bottom border of 1px `surface-variant` to define boundaries without adding bulk.
- Use `tertiary` (Neon Gold) icons for high-priority or paid "fast-pass" queue items.

**Input Fields**
- Fields are "Filled" MD3 style with a dark surface. The active state must use a high-visibility Hot Pink bottom line and cursor.

**Cards**
- Use for featured songs or performer profiles. Cards should have a 1px border using the `outline` token to ensure they don't disappear into the deep black background.

**Additional Components**
- **Progress Bar:** Used for "Song Completion." Utilize a gradient from `primary` to `secondary` to reinforce the nightclub aesthetic.
- **Now Playing Indicator:** A pulsing neon ring around the current performer's avatar using the `primary` glow effect.