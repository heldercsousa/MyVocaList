# User Suggestions — Design
**Date:** 2026-05-30  
**Status:** Draft — pending Helder review

---

## Chosen Approach

In-app form → GitHub Issues REST API (POST `/repos/heldercsousa/MyVocaList/issues`) using a fine-grained PAT. No backend required.

**Why GitHub Issues on the existing repo:**
| Alternative | Rejected because |
|-------------|-----------------|
| Dedicated private repo | Extra repo management overhead; same result |
| Instabug / Shake SDK | Paid; heavyweight SDK for a small feature |
| Discord / Slack webhook | No structured issue tracking; no way to close/label |
| mailto: deep link | Opens native email app; high friction; no auto-metadata |
| Simple Azure Function endpoint | Requires a backend; ops overhead for one endpoint |

**PAT security posture:** Fine-grained PAT scoped to Issues (write) on the MyVocaList repo only. If the PAT leaks, an attacker can create issues — not read code, not push commits. Acceptable blast radius for an indie app. PAT is embedded in `appsettings.json` (gitignored).

---

## PAT Storage

Same pattern as Sentry DSN — `appsettings.json` (gitignored):

```json
// appsettings.json (gitignored)
{
  "Sentry": { "Dsn": "..." },
  "GitHub": {
    "FeedbackPat": "github_pat_...",
    "FeedbackRepo": "heldercsousa/MyVocaList"
  }
}

// appsettings.template.json (committed)
{
  "Sentry": { "Dsn": "" },
  "GitHub": {
    "FeedbackPat": "",
    "FeedbackRepo": "heldercsousa/MyVocaList"
  }
}
```

---

## Service Interface — `IFeedbackService`

New interface in `MyVocaList.Domain/ServicesInterfaces/`:

```csharp
public interface IFeedbackService
{
    /// <summary>Submits a user suggestion as a GitHub Issue.</summary>
    /// <returns>(true, null) on success; (false, errorMessage) on failure.</returns>
    Task<(bool success, string? error)> SubmitAsync(FeedbackSubmission submission, CancellationToken ct = default);
}
```

DTO in `MyVocaList.Contracts/DTOs/`:

```csharp
public record FeedbackSubmission(
    FeedbackCategory Category,
    string Message,
    string? Email);

public enum FeedbackCategory { BugReport, FeatureRequest, Other }
```

---

## Service Implementation — `FeedbackService`

Located in `MyVocaList.Services/FeedbackService.cs`. Uses `HttpClient` (injected via `IHttpClientFactory`).

```
POST https://api.github.com/repos/{repo}/issues
Authorization: Bearer {PAT}
Accept: application/vnd.github+json
X-GitHub-Api-Version: 2022-11-28

{
  "title": "[Bug Report] First 60 chars of message...",
  "body": "{message}\n\n---\n**App version:** ...\n**OS:** ...\n**Device:** ...\n**Submitted:** ...\n**Contact:** {email or N/A}",
  "labels": ["user-feedback", "bug"]
}
```

Error handling:
- HTTP 2xx → success
- HTTP 4xx/5xx or network exception → `(false, "Could not send — please try again")`
- Missing PAT config → log warning locally, return failure (no crash)

---

## ViewModel — `FeedbackViewModel`

New ViewModel at `MyVocaList/UI/ViewModels/FeedbackViewModel.cs`.

```csharp
[ObservableProperty] private FeedbackCategory _selectedCategory = FeedbackCategory.BugReport;
[ObservableProperty] private string _message = string.Empty;
[ObservableProperty] private string _email = string.Empty;
[ObservableProperty] private bool _isSubmitting;

// CanExecute: !IsSubmitting && !string.IsNullOrWhiteSpace(Message)
[RelayCommand(CanExecute = nameof(CanSubmit))]
private async Task SubmitAsync() { ... }

private bool CanSubmit => !IsSubmitting && !string.IsNullOrWhiteSpace(Message);
```

---

## Page — `FeedbackPage`

New page at `MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml`.

Uses `ScrollView` + `VerticalStackLayout` (form layout — not a list, so `DXCollectionView` is not appropriate here per `constraints-registry.md`).

Form elements (all DevExpress):
- `dxe:ComboBoxEdit` — Category (Bug Report / Feature Request / Other)
- `dxe:MultilineEdit` — Message (required, max 1000 chars, character counter in helper text)
- `dxe:TextEdit` — Email (optional, keyboard hint: email)
- `dx:DXButton` (FilledButton, full-width) — "Send Feedback"; disabled when `CanSubmit` is false or `IsSubmitting` is true
- `dx:DXActivityIndicator` — shown while `IsSubmitting`

---

## Navigation

Add route `feedback` to `Routes.cs`. Entry point: Settings page — add a "Send Feedback" list item that navigates to `FeedbackPage`.

---

## Layers Affected

| Layer | Change |
|-------|--------|
| `MyVocaList.Contracts` | Add `FeedbackSubmission` DTO + `FeedbackCategory` enum |
| `MyVocaList.Domain` | Add `IFeedbackService` interface |
| `MyVocaList.Services` | Add `FeedbackService` implementation |
| `MyVocaList` (MAUI) | Add `FeedbackViewModel`, `FeedbackPage.xaml/.cs` |
| `MyVocaList` (MAUI) | `Routes.cs` — add `feedback` route |
| `MyVocaList` (MAUI) | `SettingsPage.xaml` — add "Send Feedback" entry |
| `MyVocaList` (MAUI) | `appsettings.json` / `appsettings.template.json` — add GitHub section |
| `MauiProgram.cs` | Register `IFeedbackService` → `FeedbackService` (Transient); register `HttpClient` / `IHttpClientFactory` if not already present |

---

## Invariants & Postconditions

- `SubmitAsync` never throws; it always returns `(bool, string?)`.
- The Send button is disabled for the full duration of an in-flight request (prevents double-submit).
- Form content is preserved on failure; cleared on success.
- No user data is persisted locally beyond the current form session.

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| GitHub Issues on existing repo | Zero extra tooling; issues are already where the dev team works |
| Fine-grained PAT (issues-only) | Minimal blast radius if PAT is exposed |
| `appsettings.json` for PAT | Consistent with Sentry DSN pattern; gitignored |
| Entry via Settings page | Low-traffic feature; doesn't need a nav-bar slot |
| Transient registration for `IFeedbackService` | `HttpClient` is managed by factory; stateless service |
