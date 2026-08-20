# About Page Copyright Year — auto-extending range · Requirements

> **Change to shipped behavior.** Supersedes the copyright line of `../../requirements.md § AC-AB-05`
> (as most recently amended by `../2026-08-20-about-page-license-text-mit/` AC-AB-05a).
> The original spec remains immutable history.

## Context

The About page renders `© 2025 Helder Sousa` as a hardcoded literal. It is already stale relative to
the current year and will drift further every January. Helder asked for the year to always reflect the
current year.

**Scope decision (Helder, 2026-08-20):** only the **copyright** line tracks the current year, and it
does so as a *range*. The `Since 2025` line is a founding-year statement and stays fixed — rendering
`Since 2026` would destroy its meaning. `Since` is already computed from `AppConstants.FoundedYear`
and is not touched by this change.

## Acceptance criteria

### AC-AB-05c — Copyright renders a founding-to-current year range *(supersedes the copyright line of AC-AB-05a)*
**Given** the founding year is `AppConstants.FoundedYear` (2025) and the current year is later,
**When** I open the About page,
**Then** the License section's copyright line reads `© 2025–2026 Helder Sousa`, using the current
year as the range end, with an en dash (`–`) separator.

### AC-AB-05d — Single year when founding year is the current year
**Given** the current year equals `AppConstants.FoundedYear`,
**When** I open the About page,
**Then** the copyright line reads `© 2025 Helder Sousa` — a single year, **no** range and no dash
(`© 2025–2025` is incorrect output).

### AC-AB-05e — No hardcoded year remains
**Given** the About page source,
**Then** the copyright line is data-bound, and no literal year appears in the copyright text in
`AboutPage.xaml`.

### AC-AB-05f — Founding year is not affected
**Given** I open the About page,
**Then** the `Since 2025` line still reads `Since 2025`, driven by `AppConstants.FoundedYear` exactly
as before this change.

## Validation / edge cases

- Current year **before** the founding year (device clock set wrong / skewed): render the single
  founding year rather than an inverted range (`© 2025`, never `© 2025–2024`).

## Out of scope

- Time-zone correctness. The device's local year is authoritative; no UTC normalization.
- Localizing the copyright symbol, dash style, or year formatting.
- Any other About page text, layout, or section ordering.
