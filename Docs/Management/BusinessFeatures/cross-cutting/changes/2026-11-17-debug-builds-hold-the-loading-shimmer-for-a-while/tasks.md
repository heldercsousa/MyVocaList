# Tasks: Debug builds hold the loading shimmer for a while

- [ ] DLH1 **`LoadingHold`, start-up switch, list base and song form.** After the song-form load fix is merged. (REQ-DLH-01 to 04) Level B.
  - Files owned: new `MyVocaList/UI/LoadingHold.cs` (or the folder the code style puts shared UI helpers in), `MyVocaList/MauiProgram.cs`, `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`, `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, new test file.
  - Demo: a debug build shows each list's shimmer and the song form's shimmer for about 1.5 s; a release build shows none.
- [ ] DLH2 **Device check (Helder).**
