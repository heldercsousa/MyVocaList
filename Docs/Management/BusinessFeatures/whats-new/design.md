# What's New / Release Notes — Design
**Date:** 2026-05-30  
**Status:** Draft — pending Helder review

---

## Approach

Bundled `releases.json` MauiAsset + first-launch-per-version `dx:BottomSheet` modal. No NuGet dependency, no network call.

**Why bundled JSON over alternatives:**
| Alternative | Rejected because |
|-------------|-----------------|
| Fetch from remote URL | Requires network; adds latency to cold start; needs a backend |
| Parse Play Store / App Store listing | Fragile scraping; unavailable on sideloaded builds |
| Third-party NuGet (e.g. Plugin.WhatsNew) | No mature MAUI-native option exists; bundled JSON is simpler and fully controllable |
| Plain text / markdown asset | JSON is more structured; easier to render highlights vs fixes separately |

---

## File: `releases.json`

**Location:** `MyVocaList/Resources/Raw/releases.json` (MauiAsset, `Build Action: MauiAsset`)

**Schema:** See `requirements.md § releases.json Schema`.

**Lifecycle:** Updated manually as part of the `/project:release` command — content sourced from `Docs/Changelog/changelog.md`.

---

## Startup Check — `IWhatsNewService`

New service interface in `MyVocaList.Domain/ServicesInterfaces/`:

```csharp
public interface IWhatsNewService
{
    /// <summary>Returns the release entry for the current version if it should be shown; null otherwise.</summary>
    Task<ReleaseEntry?> GetPendingReleaseAsync(CancellationToken ct = default);

    /// <summary>Marks the current version as seen; subsequent calls to GetPendingReleaseAsync return null.</summary>
    void MarkCurrentVersionSeen();
}
```

Implementation in `MyVocaList.Services/WhatsNewService.cs`:

```csharp
// Pseudocode
public async Task<ReleaseEntry?> GetPendingReleaseAsync(CancellationToken ct)
{
    var current = AppInfo.VersionString;
    var lastSeen = Preferences.Get("last_seen_version", null);

    if (lastSeen == null)           // fresh install
    {
        MarkCurrentVersionSeen();
        return null;
    }
    if (lastSeen == current)        // no upgrade
        return null;

    var entries = await LoadReleasesJsonAsync(ct);
    return entries.FirstOrDefault(e => e.Version == current); // null = no entry → skip
}

public void MarkCurrentVersionSeen()
    => Preferences.Set("last_seen_version", AppInfo.VersionString);
```

---

## DTO

```csharp
// MyVocaList.Contracts/DTOs/ReleaseEntry.cs
public record ReleaseEntry(
    string Version,
    string Date,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Fixes);
```

---

## Trigger Point — AppShell

`AppShellViewModel.InitializeAsync()` calls `IWhatsNewService.GetPendingReleaseAsync()`. If a non-null entry is returned, it raises a message (via `WeakReferenceMessenger`) that `AppShell.xaml.cs` handles to show the `WhatsNewBottomSheet`.

```csharp
// AppShellViewModel
var entry = await _whatsNewService.GetPendingReleaseAsync();
if (entry != null)
    Messenger.Send(new ShowWhatsNewMessage(entry));
```

---

## UI — `WhatsNewBottomSheet`

New `ContentView` at `MyVocaList/UI/Components/WhatsNewBottomSheet.xaml`.

Wraps a `dx:BottomSheet` with:
- Header: "What's New in {Version}" (`Title.Large`)
- Date line: formatted from `ReleaseEntry.Date` (`Body.Small`, muted)
- Section "Highlights" (if non-empty): bulleted `VerticalStackLayout` using `BindableLayout`
- Section "Bug Fixes" (if non-empty): same pattern
- Footer: `dx:DXButton` "Got it" (`FilledButton`, full-width) → calls `IWhatsNewService.MarkCurrentVersionSeen()` + closes sheet

No scroll needed for typical release notes length; if content overflows, the sheet is scrollable via `dx:BottomSheet` built-in scroll.

---

## Layers Affected

| Layer | Change |
|-------|--------|
| `MyVocaList.Contracts` | Add `ReleaseEntry` DTO |
| `MyVocaList.Domain` | Add `IWhatsNewService` interface |
| `MyVocaList.Services` | Add `WhatsNewService` implementation |
| `MyVocaList` (MAUI) | `AppShellViewModel` — add startup call; `AppShell.xaml.cs` — handle message |
| `MyVocaList` (MAUI) | Add `WhatsNewBottomSheet` ContentView |
| `MyVocaList` (MAUI) | Add `Resources/Raw/releases.json` MauiAsset |
| `MauiProgram.cs` | Register `IWhatsNewService` → `WhatsNewService` (Singleton) |

---

## Invariants & Postconditions

- `GetPendingReleaseAsync` never throws; it returns `null` on any parsing error or missing file.
- `MarkCurrentVersionSeen` is called exactly once per upgrade: either on fresh install detection or on modal dismiss. It is idempotent.
- The modal is shown at most once per app version string, regardless of how many times the app is restarted.

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| Hide modal on fresh install | Industry standard UX: user has no baseline; changelog is confusing before first use |
| `WeakReferenceMessenger` for trigger | Decouples `AppShellViewModel` from the sheet; consistent with CommunityToolkit.Mvvm messaging patterns in the project |
| Singleton registration for `IWhatsNewService` | `Preferences` reads are cheap; single instance avoids redundant JSON parses on the same launch |
| No animation customization | `dx:BottomSheet` default animation is appropriate; no project-specific override needed |
