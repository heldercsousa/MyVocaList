# FROZEN — AutocompleteField component family

This component family is frozen per decision D-AC1 (`Docs/Management/DevCycleCraft/autocomplete-component/2026-07-19-dx-autocomplete-adoption-decision.md`): both consumers now use the DevExpress `AutoCompleteEdit`. All files in this folder (and their 6 test files in `MyVocaList.Tests/Unit/Components/`) are excluded from compilation via `<Compile Remove>` / `<MauiXaml Remove>` in the respective `.csproj`s. They are retained on disk as reference material for the future full-screen autocomplete guideline ①.
