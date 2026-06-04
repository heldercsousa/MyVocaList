# Evaluation: About Page for MyVocaList

## Context

Helder proposed adding an "About" page to the flyout menu containing: app goal, main features, current version (header), "Since XXXX", distribution model, and current release notes (What's New). Request also covers whether the planned What's New feature can share code with this page to avoid duplication.

---

## UX Verdict: YES — with pruning

The About page is **justified**, but two proposed content elements should be dropped. Here is the element-by-element verdict using UX strategy (HEART/trust signals) and IA principles (navigability, redundancy, depth):

| Content element | Verdict | Rationale |
|---|---|---|
| **Version in header** | ✅ Keep | Version is displayed **nowhere** in the current UI. For a B2B-adjacent event admin tool, this is a practical support need — users must know their version to report bugs or verify updates. |
| **App logo + title** | ✅ Keep | Standard mobile About pattern. Reinforces branding, consistent with industry convention. |
| **"Since XXXX"** | ✅ Keep | Trust signal for professional/venue users. Communicates maturity. Particularly meaningful for a tool used by venue operators who evaluate software longevity before relying on it. |
| **Distribution model** | ✅ Keep | Venue administrators and event organizers may need to reference licensing terms. Legitimate and expected. |
| **Current release notes** | ✅ Keep | The What's New modal appears once per version. About provides the **permanent, on-demand** access path to the same content. Clean reuse of `IWhatsNewService` — no duplication. |
| **App goal (brief)** | ⚠️ Marginal | Only keep as a single sentence. A section is too heavy — active users already know the app's purpose. If included at all: one sentence under the logo, not a heading. |
| **Main features list** | ❌ Remove | Active users already use the features. This is onboarding content, not About content. A features list in About adds spec and maintenance weight with zero functional return. |

**Why About is justified for this specific app:**  
MyVocaList is not a consumer app where About pages are ignored. It is a karaoke event management tool used by operators, venue staff, and possibly multiple administrators. This audience checks version numbers, references licensing, and re-reads release notes — all behaviors consistent with About page usage. The HEART framework's Trust and Task Success dimensions are served by making version and release information permanently accessible.

---

## IA Placement

**Where:** Add as the last item in the **System** menu group, after Backup & Restore and before Exit.

```
System
  ├── Preferences
  ├── Backup & Restore
  ├── About          ← new
  └── Exit
```

This placement follows the universal convention (iOS Settings, Android system apps, desktop apps). Depth: 1 level from flyout → correct and shallow.

---

## Code Reuse with What's New

The planned `IWhatsNewService` (spec: `Docs/Management/BusinessFeatures/whats-new/design.md`) reads `releases.json` and exposes `ReleaseEntry`. The About page ViewModel can call this service **without the gating logic** — it always shows the current release notes, regardless of whether the user has already dismissed the modal.

**Pattern:**
- `WhatsNewService.GetCurrentReleaseAsync()` — new method (or reuse of existing, stripped of seen-check)
- About page displays the same `ReleaseEntry` DTO that the bottom sheet uses
- **No duplication of data or parsing logic**

**Sequencing constraint:**  
What's New must be implemented first (it owns `IWhatsNewService` and `ReleaseEntry`). About page is a consumer. This is captured in the task dependency below.

---

## Spec Plan (if approved)

If Helder approves this evaluation, the next steps are:

1. **Invoke `superpowers:brainstorming`** — to finalise content structure, interaction design (is About a page or a bottom sheet?), and edge cases (version format, "Since" year source)
2. **Write `Docs/Management/BusinessFeatures/about-page/requirements.md`** — acceptance criteria for each content element
3. **Write `Docs/Management/BusinessFeatures/about-page/design.md`** — page structure, VM interface, reuse contract with `IWhatsNewService`, navigation entry
4. **Write `Docs/Management/BusinessFeatures/about-page/tasks.md`** — phased tasks with explicit dependency on What's New tasks

**Key open question for brainstorming:**  
Is About a **full Shell page** (navigates to) or a **BottomSheet** (modal from flyout tap)? Both are valid. A BottomSheet is lighter and matches the app's modal patterns, but a page gives more vertical space for release notes. This should be decided before speccing.

**BACKLOG.md routing:**  
About page belongs in `Docs/Management/BusinessFeatures/about-page/` (business feature, not a DevCycleCraft item).

---

## Decisions (resolved)

| Decision | Answer |
|---|---|
| UI pattern | **Full Shell page** — navigates to `AboutPage` from flyout, AppBar shows version |
| "Since" year source | **Hardcoded constant** — `AppConstants.FoundedYear` (or equivalent); never changes at runtime |
| Features list | **Removed** from scope |
| App goal | **One sentence** under the logo, no dedicated section |
| What's New dependency | About page consumes `IWhatsNewService`; What's New tasks must be completed first |

## Pre-Spec Checklist

- [x] Helder confirms the pruning (drop Features list; app goal as one sentence only)
- [x] Helder decides: full page (Shell routing)
- [x] "Since" year: hardcoded constant
- [ ] Helder approves this evaluation — spec writing begins
