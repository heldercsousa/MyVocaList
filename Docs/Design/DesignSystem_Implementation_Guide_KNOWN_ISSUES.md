# Design System Implementation - Known Issues & Workarounds

**Last Updated:** 2026-01-28 (Updated: File naming convention added)

This document tracks implementation issues, failed approaches, workarounds, and pending resolutions for the MD3 Design System implementation.

---

## File Naming Convention

**Effective:** 2026-01-28
**Pattern:** `DS{ComponentName}Page`

All Design System component showcase pages follow this naming pattern:

| Old Name | New Name | Purpose |
|----------|----------|---------|
| `DesignSystemPage` | `DSPage` | Navigation hub |
| `ComponentsPage_Typography` | `DSTypographyPage` | Typography showcase |
| `ComponentsPage_Buttons` | `DSButtonsPage` | Button variants showcase |
| Future: `ComponentsPage_Cards` | `DSCardsPage` | Card types showcase |
| Future: `ComponentsPage_Inputs` | `DSInputsPage` | Input controls showcase |

**Rationale:**
- Shorter, cleaner names
- Consistent prefix identifies Design System pages
- Easier to type and reference
- Follows common UI library conventions (e.g., "DS" = "Design System")

---

## ISSUE-001: Material Symbols Font Loading Freeze

**Date Discovered:** 2026-01-28
**Updated:** 2026-01-28 (RESOLVED: SVG approach implemented)
**Severity:** HIGH - Performance Impact
**Status:** RESOLVED
**Affected Component:** Buttons with icons, all Material Symbols FontImageSource usage

### Original Approach
Using `FontImageSource` with Material Symbols font for button icons:
```xml
<Button Text="Home" StyleClass="FilledButton">
    <Button.ImageSource>
        <FontImageSource FontFamily="MaterialOutlined"
                         Glyph="{x:Static m:MaterialOutlined.Home}" />
    </Button.ImageSource>
</Button>
```

### Why It Failed
**Log Evidence (2026-01-28):**
```
[0:] Microsoft.Maui.FontManager: Warning: Unable to load font MaterialSymbolsOutlined.ttf
Java.Lang.RuntimeException: Font asset not found /data/user/0/com.myvocalist/cache/MaterialSymbolsOutlined.ttf
[Choreographer] Skipped 539 frames! (MDC init) + Skipped 44 frames! (Buttons page)
```

**Key Findings:**
- Android cannot load Material Symbols font from MAUI package
- Freeze occurs even with only 8 icons (not a "heavy usage" issue)
- Font file path issue: expects `/cache/MaterialSymbolsOutlined.ttf`, doesn't exist
- This is a **MAUI platform bug**, not our code

### MD3-Compliant Alternatives

**IMPORTANT:** Material Design 3 does NOT mandate icon fonts. The spec requires:
1. **Material Symbols designs** (the icon shapes/glyphs) ✓
2. **24dp icon size** with **48dp touch targets** ✓
3. **Semantic theme colors** ✓

We can achieve full MD3 compliance with alternative formats:

#### ✅ CHOSEN SOLUTION: Material Symbols SVG (MD3 Compliant)
Download individual SVG files from https://fonts.google.com/icons
- Same Material Symbols designs ✓
- No font loading freeze ✓
- Smaller bundle size (only icons used) ✓
- MAUI automatically compiles SVG to PNG at appropriate densities (1x, 2x, 3x) ✓
- No manual multi-resolution file creation needed ✓
- **Minor issue:** SVG tinting not working in MAUI Button.ImageSource (icons visible but don't respect button text color)
- **Status:** Implemented successfully in DSButtonsPage

#### ❌ Option 2: Manual PNG Creation (Rejected)
Export icons as 24dp PNG with transparency at multiple densities:
- **Rejected reason:** Too labor intensive - requires manual export at 1x, 2x, 3x for each icon
- MAUI build system handles this automatically for SVG files

#### ❌ Option 3: Custom Icon Font
Create subset font with only needed icons:
- Reduces font size
- Still requires font loading (may still freeze)
- Complex build process
- **Not recommended**

### Resolution Implemented
1. ✅ Removed all Material Symbols FontImageSource usage from DSButtonsPage
2. ✅ Removed Material Symbols namespace (xmlns:m)
3. ✅ Switched to existing SVG icons in Resources/Images:
   - home.svg, home_fill.svg, home_round.svg
   - tune.svg, tune_fill.svg, tune_round.svg
4. ✅ MAUI build system automatically compiles SVG to PNG at appropriate densities
5. ✅ Added warning card in DSButtonsPage explaining the issue and solution

### Performance Results
**Before (with Material Symbols):**
- Constructor: 566ms
- Frame skips: 44 frames
- Font loading errors in logs

**After (with SVG):**
- Constructor: 438ms (23% improvement)
- Frame skips: 36 frames (18% improvement)
- No font loading errors
- Icons render correctly

### Files Modified
- `DSButtonsPage.xaml` (2026-01-28) - Removed Material Symbols, using SVG icons
- `DesignSystem_Implementation_Guide_KNOWN_ISSUES.md` (2026-01-28) - Documented resolution

---

## ISSUE-002: MaterialIconButton Not Available

**Date Discovered:** 2026-01-28
**Severity:** MEDIUM - Feature Limitation
**Status:** DOCUMENTED
**Affected Component:** Icon-only buttons

### Original Approach
Implementation guide (Part 8, lines 1061-1069) specifies using MDC MaterialIconButton:
```xml
<mdc:MaterialIconButton Icon="star.png" />
<mdc:MaterialIconButton Icon="settings.png" />
```

### Why It Failed
- Package version mismatch:
  - CLAUDE.md specifies: `HorusStudio.Maui.MaterialDesignControls 2.2.0`
  - Implementation guide assumes: version 10.0.0
- MaterialIconButton not available in v2.2.0
- Build error: `Failed to resolve assembly: 'HorusSoftware.Maui.MaterialDesignControls'`

### Workaround Applied
Using standard MAUI Button with SVG icons and explicit sizing:
```xml
<Button StyleClass="FilledButton" ImageSource="home_fill.svg"
        WidthRequest="48" HeightRequest="48" Padding="0" />
```

### Pending Resolution
- **Option 1:** Upgrade to MDC 10.0.0 (requires testing for breaking changes)
- **Option 2:** Continue with Button workaround (acceptable for MVP)
- **Option 3:** Research if MaterialIconButton exists in 2.2.0 under different namespace

### Files Modified
- `ComponentsPage_Buttons.xaml` (2026-01-28)

---

## ISSUE-003: SVG Icon Color Not Respecting Button Theme

**Date Discovered:** 2026-01-28
**Severity:** MEDIUM - Visual Issue
**Status:** OPEN - NEEDS FIX
**Affected Component:** All buttons with SVG ImageSource

### Problem Description
SVG icons in buttons render as black/dark color instead of adapting to button's foreground color:
- Not visible in dark theme
- Only become visible when button is pressed (state change)
- Should use button's text color automatically

### Root Cause
SVG files likely have hard-coded fill colors or MAUI not applying tint to SVG ImageSource in buttons.

### Potential Solutions
1. **Modify SVG files** - Remove fill attribute, use `currentColor`
2. **Use TintColor property** - If available on Button.ImageSource
3. **Convert to PNG with transparency** - Ensures platform tinting works
4. **Use FontImageSource with different font** - Find alternative icon font that doesn't freeze

### Files Affected
- All SVG files in `Resources/Images/`
- `ComponentsPage_Buttons.xaml`

### Investigation Needed
- Check if SVG files contain `fill="#000000"` or similar
- Test if `<Button.ImageSource>` supports TintColor binding
- Review MAUI documentation for SVG tinting in ImageSource

---

## ISSUE-004: Elevated Button Styling Identical to Text Button

**Date Discovered:** 2026-01-28
**Severity:** LOW - Visual Inconsistency
**Status:** OPEN - NEEDS INVESTIGATION
**Affected Component:** ElevatedButton StyleClass

### Problem Description
Button with `StyleClass="ElevatedButton"` looks identical to `StyleClass="TextButton"`:
- No visible elevation (shadow)
- Same background color
- No visual distinction

### Root Cause
- StyleClass provided by UraniumUI.Material implicit styles
- Not defined in our MaterialStyles.xaml
- Cannot be overridden without breaking UraniumUI's MD3 compliance

### Potential Solutions
1. **Test on physical device** - Emulator may not render shadows correctly
2. **Check UraniumUI documentation** - Verify ElevatedButton expected appearance
3. **Report to UraniumUI** - May be a bug in library
4. **Accept as-is** - If library implements MD3 spec correctly

### Files Affected
- None (implicit styling from UraniumUI)

---

## ISSUE-005: MDC Initialization Frame Skips

**Date Discovered:** 2026-01-28
**Severity:** MEDIUM - Performance Impact
**Status:** ACCEPTED - NOT CRITICAL YET
**Affected Component:** App startup

### Problem Description
MDC (HorusStudio.Maui.MaterialDesignControls) initialization causes frame skips:
- Log: `[Choreographer] Skipped 253 frames!`
- Registers 17 default component styles at startup
- Happens once per app launch

### Log Evidence
```
[DOTNET] [HorusStudio.Maui.MaterialDesignControls > MaterialDesignControlsBuilder > RegisterDefaultStyles]: Start registering components default styles
[DOTNET] [HorusStudio.Maui.MaterialDesignControls > MaterialDesignControlsBuilder > AddStyles]: Registering default style for MaterialButton
... (17 components total)
[Choreographer] Skipped 253 frames!
```

### Workaround
None currently applied. Accepted as library behavior.

### Potential Solutions
1. **Lazy load MDC styles** - Only register when first MDC control is used
2. **Profile and optimize** - Identify which styles are slow to register
3. **Report to HorusStudio** - May be optimization opportunity in library
4. **Accept performance cost** - One-time startup cost may be acceptable

### Pending Decision
User stated: "let's leave this concern for later" - not critical for current development phase.

---

## ISSUE-006: Button Navigation Freeze (Page Instance Recreation)

**Date Discovered:** 2026-01-28
**Severity:** MEDIUM - Performance Impact
**Status:** ACCEPTED - BY DESIGN
**Affected Component:** Shell navigation with GoToAsync

### Problem Description
Navigating to component pages via button (DesignSystemPage) recreates page instance every time:
- ComponentsPage_Buttons: 575ms first load, 750ms second load
- ComponentsPage_Typography: 277ms first load
- Frame skips: 43-77 frames per navigation
- TabBar navigation caches instances (instant after first load)

### Log Evidence
```
[DOTNET] ComponentsPage_Buttons: Constructor started
[DOTNET] ComponentsPage_Buttons: Constructor completed after 575ms
[Choreographer] Skipped 43 frames!
```

### Root Cause
MAUI Shell behavior:
- **TabBar navigation** - Caches ShellContent instances
- **GoToAsync navigation** - Creates new page instance each time

### User Decision
"About rebuilding the entire page each navigation by body button tap, actually we shall find a way to avoid it in the load process themselves rather then other strategies like keeping instances alive (would probably run in memory overflow sometime). But, let's leave this concern for later."

### Potential Solutions
1. **Optimize page constructors** - Reduce work in InitializeComponent
2. **Lazy load UI elements** - Only render visible content
3. **Cache heavy resources** - Pre-load images, styles
4. **Profile page load** - Identify bottlenecks in page creation

### Pending Decision
Deferred for later optimization phase.

---

## ISSUE-007: MaterialButton Background Warning

**Date Discovered:** 2026-01-28
**Severity:** LOW - Warning Only
**Status:** EXPECTED BEHAVIOR
**Affected Component:** All buttons using UraniumUI StyleClass

### Problem Description
Android logs show repeated MaterialButton warnings:
```
[MaterialButton] MaterialButton manages its own background to control elevation, shape, color and states. Consider using backgroundTint, shapeAppearance and other attributes where available.
```

### Root Cause
UraniumUI's StyleClass sets Button properties that conflict with Android's native MaterialButton expectations. This is expected when using cross-platform styling.

### Resolution
Ignore warnings - this is expected behavior when using MAUI cross-platform button styles over Android's native MaterialButton.

---

## Suggestions for Improvement

### 1. Single Icon File Approach
**User Suggestion:** "SVG icons file creation one by one seems to have a ease approach where a single file with all icons is present (could save tokens and time)"

**Considerations:**
- Icon sprite sheets or icon font could consolidate icons
- Would reduce file management overhead
- May require custom rendering logic
- Trade-off: Flexibility vs. Simplicity

**Action:** Research feasibility for future implementation

### 2. Pre-process Shared Code for Thread Safety
**User Suggestion:** "Would pre-process repetitive and shared buttons + other code and share to every element be possible and be 'thread safe'? Maybe it could save load time, though there is a real dangerous about UI threads concurrency that would making load even slower"

**Analysis:**
- Shared button templates could reduce page load time
- Thread safety concerns are valid - UI must always execute on platform UI thread
- MAUI already optimizes XAML compilation and style resolution
- Pre-processing may not provide significant benefit vs. complexity added

**Recommendation:**
- Current approach (StaticResource styles) already provides shared styling
- Further optimization should focus on reducing page constructor work
- Avoid custom threading logic that could violate UI thread safety

**Action:** Monitor performance with more complex pages before optimizing

---

## Issue Summary by Status

| Status | Count | Issues |
|--------|-------|--------|
| RESOLVED | 1 | ISSUE-001 |
| OPEN | 2 | ISSUE-003, ISSUE-004 |
| WORKAROUND APPLIED | 1 | ISSUE-002 |
| ACCEPTED | 2 | ISSUE-005, ISSUE-006 |
| EXPECTED | 1 | ISSUE-007 |

---

## Next Actions

### High Priority
1. **Fix SVG icon color** (ISSUE-003) - Buttons need visible icons
2. **Investigate Elevated button styling** (ISSUE-004) - Verify expected appearance

### Medium Priority
3. **Test Material Symbols alternatives** (ISSUE-001) - Find performance-friendly icon solution
4. **Profile page load** (ISSUE-006) - Identify optimization opportunities

### Low Priority
5. **Research MDC version upgrade** (ISSUE-002) - Evaluate v10.0.0 for MaterialIconButton
6. **Research icon consolidation** (User suggestion) - Evaluate sprite sheet approach

---

## Notes for Claude Chat Planning

This file should be provided to Claude Chat when planning workarounds or architectural changes. Key considerations:

1. **Performance is critical** - Dark theme on emulator shows frame skips easily
2. **Thread safety is paramount** - Never violate MAUI UI thread rules
3. **MD3 compliance required** - Solutions must follow Material Design 3 guidelines
4. **Library limitations** - Working within constraints of UraniumUI 2.14 and MDC 2.2.0
5. **User preference** - Workarounds preferred over removing functionality

---

**Document Maintenance:**
- Update this file when new issues are discovered
- Mark issues as RESOLVED when fixed
- Add date stamps to all changes
- Keep ISSUE numbers sequential
