# App Update Check — Design
**Date:** 2026-05-31  
**Status:** Draft — pending Helder review

---

## Approach

Remote version manifest (GitHub raw) + startup check + two-tier bottom sheets (soft nudge / hard block). No NuGet dependency beyond what the project already uses. Fail-open on network failure.

**Why remote manifest over alternatives:**

| Alternative | Rejected because |
|-------------|-----------------|
| iTunes Lookup API (iOS only) | Android has no equivalent public API; platform-divergent implementation |
| Firebase Remote Config | Infra overhead; Firebase SDK is a heavy dependency not otherwise needed |
| Bundled version list (like `releases.json`) | Cannot enforce a minimum version remotely; requires app update to change the threshold |
| GitHub Releases API | Rate-limited (60 req/hr unauthenticated); JSON schema complex |
| Remote JSON manifest (chosen) | Zero infra cost, no rate limits, full control over schema, updated at release time |

---

## Version Manifest

**File:** `version-manifest.json` at repository root (committed, public via GitHub raw URL).

**Hosted URL:** `https://raw.githubusercontent.com/heldercsousa/MyVocaList/main/version-manifest.json`

**Schema:**
```json
{
  "latestVersion": "0.3.0",
  "minRequiredVersion": "0.2.0",
  "storeUrls": {
    "android": "https://play.google.com/store/apps/details?id=com.myvocalist",
    "ios": "https://apps.apple.com/app/myvocalist/idXXXXXXX"
  },
  "updateMessage": "This version is no longer supported. Please update to continue."
}
```

**Lifecycle:** Updated by the main agent as part of the `/project:release` command — `latestVersion` bumped on every release; `minRequiredVersion` raised only when backward compatibility is broken.

---

## IVersionCheckService

New interface in `MyVocaList.Domain/ServicesInterfaces/`:

```csharp
public interface IVersionCheckService
{
    /// <summary>Fetches the version manifest and determines if the current app version requires action.</summary>
    /// <returns>UpdateCheckResult indicating the update state and store URL.</returns>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default);
}
```

---

## DTOs

```csharp
// MyVocaList.Contracts/DTOs/VersionManifest.cs
public record VersionManifest(
    string LatestVersion,
    string MinRequiredVersion,
    Dictionary<string, string> StoreUrls,
    string UpdateMessage);

// MyVocaList.Contracts/DTOs/UpdateCheckResult.cs
public record UpdateCheckResult(
    bool IsUpToDate,
    bool IsUpdateAvailable,   // soft nudge: latestVersion > current >= minRequired
    bool IsUpdateRequired,    // hard block: current < minRequired
    string StoreUrl,
    string LatestVersion,
    string UpdateMessage);
```

---

## VersionCheckService Implementation

`MyVocaList.Services/VersionCheckService.cs`:

```csharp
// Pseudocode
public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct)
{
    VersionManifest manifest;
    try
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        manifest = await _httpClient.GetFromJsonAsync<VersionManifest>(ManifestUrl, cts.Token);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Version manifest fetch failed — fail-open");
        return UpdateCheckResult.UpToDate;
    }

    var current = NuGetVersion.Parse(AppInfo.VersionString);
    var latest  = NuGetVersion.Parse(manifest.LatestVersion);
    var minReq  = NuGetVersion.Parse(manifest.MinRequiredVersion);

    var storeUrl = DeviceInfo.Platform == DevicePlatform.Android
        ? manifest.StoreUrls.GetValueOrDefault("android", string.Empty)
        : manifest.StoreUrls.GetValueOrDefault("ios", string.Empty);

    if (current < minReq)
        return new UpdateCheckResult(false, false, true, storeUrl, manifest.LatestVersion, manifest.UpdateMessage);

    if (current < latest)
        return new UpdateCheckResult(false, true, false, storeUrl, manifest.LatestVersion, manifest.UpdateMessage);

    return UpdateCheckResult.UpToDate;
}
```

**Version comparison:** Uses `NuGet.Versioning.NuGetVersion` (add `NuGet.Versioning` package reference to Services project — small, zero-dependency package). Handles pre-release labels (`-alpha.N`) correctly.

**`HttpClient` registration:** Named client `"version-check"` registered in `MauiProgram.cs` via `IHttpClientFactory`. `VersionCheckService` receives `IHttpClientFactory`.

---

## Trigger Point — AppShell

`AppShellViewModel.InitializeAsync()` calls `IVersionCheckService.CheckForUpdatesAsync()`. Result dispatched via `WeakReferenceMessenger` (same pattern as What's New).

```csharp
// AppShellViewModel
var result = await _versionCheckService.CheckForUpdatesAsync();
if (result.IsUpdateRequired)
    Messenger.Send(new ShowUpdateRequiredMessage(result));
else if (result.IsUpdateAvailable)
    Messenger.Send(new ShowUpdateAvailableMessage(result));
```

`AppShell.xaml.cs` subscribes to both messages and shows the appropriate sheet.

---

## UI

### UpdateAvailableBottomSheet (soft nudge)

`MyVocaList/UI/Components/UpdateAvailableBottomSheet.xaml`  
`dx:BottomSheet` — dismissible (`IsCancelable="True"`)

- Title: "Update Available" (`Title.Large`)
- Body: "Version {LatestVersion} is ready. Update for the latest features and fixes." (`Body.Medium`)
- Buttons (horizontal row): "Later" (`OutlinedButton`) + "Update Now" (`FilledButton` → `Launcher.OpenAsync(StoreUrl)`)

### UpdateRequiredBottomSheet (hard block)

`MyVocaList/UI/Components/UpdateRequiredBottomSheet.xaml`  
`dx:BottomSheet` — non-dismissible (`IsCancelable="False"`)

- Title: "Update Required" (`Title.Large`)
- Body: manifest `UpdateMessage` field (`Body.Medium`)
- Single button: "Update Now" (`FilledButton`, full-width → `Launcher.OpenAsync(StoreUrl)`)

No dismiss path. Back gesture swallowed by `IsCancelable="False"`.

---

## Layers Affected

| Layer | Change |
|-------|--------|
| `version-manifest.json` (repo root) | New hosted manifest file |
| `MyVocaList.Contracts` | Add `VersionManifest` and `UpdateCheckResult` DTOs |
| `MyVocaList.Domain` | Add `IVersionCheckService` interface |
| `MyVocaList.Services` | Add `VersionCheckService` implementation |
| `MyVocaList` (MAUI) | `AppShellViewModel` — startup call + messenger sends |
| `MyVocaList` (MAUI) | `AppShell.xaml.cs` — subscribe to update messages, show sheets |
| `MyVocaList` (MAUI) | Add `UpdateAvailableBottomSheet` + `UpdateRequiredBottomSheet` components |
| `MauiProgram.cs` | Register `IVersionCheckService` (Singleton) + named `HttpClient` |
| `MyVocaList.Services.csproj` | Add `NuGet.Versioning` package reference |

---

## Invariants & Postconditions

- `CheckForUpdatesAsync` never throws; all exceptions return fail-open result.
- `IsUpdateRequired` and `IsUpdateAvailable` are mutually exclusive.
- The hard block sheet cannot be dismissed by any user gesture.
- The manifest URL is a compile-time constant; not configurable at runtime.

---

## Release Workflow Integration

`/project:release` command gains a step:
1. Auto-bump `latestVersion` in `version-manifest.json` to the new version.
2. Prompt: "Raise `minRequiredVersion`?" → yes/no (raise only on breaking changes).
3. Commit `version-manifest.json` as part of the release commit.

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| Fail-open on network error | A live-event app must never be blocked by a transient connectivity issue |
| `NuGet.Versioning` for comparison | Handles pre-release SemVer (`-alpha.N`) correctly; lexicographic string comparison gives wrong results |
| Named `HttpClient` via `IHttpClientFactory` | Avoids socket exhaustion; .NET best practice |
| `WeakReferenceMessenger` for trigger | Decouples ViewModel from sheet; consistent with What's New pattern |
| Singleton registration | Fetch happens once per session at startup |
| `IsCancelable="False"` for hard block | DX BottomSheet property handles both swipe and back gesture dismissal |
