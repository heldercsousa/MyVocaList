# Tasks: Debug builds hold the loading shimmer for a while

- [x] DLH1 **`LoadingHold`, start-up switch, list base and song form.** After the song-form load fix is merged. (REQ-DLH-01 to 04) Level B.
  - Files owned: new `MyVocaList/UI/LoadingHold.cs` (or the folder the code style puts shared UI helpers in), `MyVocaList/MauiProgram.cs`, `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`, `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test file.
  - Demo: a debug build shows each list's shimmer and the song form's shimmer for about 1.5 s; a release build shows none.
- [ ] DLH3 **Shimmer on every list load that replaces the rows.** After DLH1. (REQ-DLH-05) Level B. Added 2026-11-17 on Helder's decision.
  - Files owned: `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`, its test file(s); `ICrudListViewModel.cs` only if a member must be added. `CrudListView` (governed) is not edited: it already binds the shimmer to `IsInitialLoading`.
  - Demo: a search, a filter change and a later visit show the shimmer while loading (1.5 s in debug builds); pull-to-refresh and load-more do not.
- [ ] DLH2 **Device check (Helder).**
