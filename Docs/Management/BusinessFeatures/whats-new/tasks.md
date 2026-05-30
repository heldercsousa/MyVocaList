# What's New / Release Notes — Tasks
**Status:** Pending spec approval — tasks to be detailed during plan phase

---

## Placeholder

Tasks will be written after Helder approves `requirements.md` and `design.md`.

**Prerequisite:** App Versioning plan (`Docs/Management/DevCycleCraft/app-versioning/plan.md`) must be executed first.

High-level phases anticipated:

1. Add `ReleaseEntry` DTO to `MyVocaList.Contracts`
2. Add `IWhatsNewService` interface to `MyVocaList.Domain`
3. Implement `WhatsNewService` in `MyVocaList.Services` + unit tests
4. Add `releases.json` MauiAsset with initial entry
5. Add `WhatsNewBottomSheet` ContentView
6. Wire `AppShellViewModel` startup call + `AppShell` message handler
7. Register `IWhatsNewService` in `MauiProgram.cs`
