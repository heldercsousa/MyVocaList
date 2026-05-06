# Build Command

Build the MyVocaList .NET MAUI 10 Android project and verify it is clean.

## Steps

1. **Run the build:**
   ```
   dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
   ```

2. **Interpret output:**
   - `Build succeeded` with 0 errors → clean. Warnings are acceptable unless they are new warnings introduced by the current change.
   - Any `error` line → must fix before proceeding.
   - MAUI-specific warnings (linker, XAML namespace, resource merge) → investigate but do not block if pre-existing.

3. **Fix errors autonomously** using the relevant skill:
   - XAML compile errors → check `maui-current-apis` skill
   - Binding/ViewModel errors → check `maui-data-binding` skill
   - DI/service errors → check `maui-dependency-injection` skill
   - DevExpress errors → check `.claude/rules/devexpress-patterns.md`

4. **After 3 consecutive failed fix attempts on the same error:**
   - Stop looping.
   - Identify which skill or rule file is most relevant.
   - Report the error to Helder with context and ask for guidance.
   - Never present work as complete while errors remain.

## Notes
- Always build the MAUI project (not just the library projects) — XAML compilation only runs in the MAUI head project.
- For iOS/Mac targets, Helder will specify; default is Android.
