# DX AutoCompleteEdit Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the frozen custom `AutocompleteField`/`AutocompleteMobileField` with DevExpress `AutoCompleteEdit` on SongFormPage (Artist) and PersonFormPage (Full Name), preserving all ViewModel/Service behavior.

**Architecture:** Behavior-preserving control swap (Approach A — direct per-page use, no wrapper). All search/validation logic stays in existing ViewModels + Services. Thin page code-behind glue routes DX editor events to the existing VM commands, exactly as the old component did.

**Tech Stack:** .NET MAUI 10 (net10.0-android/ios), DevExpress MAUI **25.2.4** (`DevExpress.Maui.Editors`), CommunityToolkit.Mvvm, xUnit.

## Global Constraints

- Worktree mandatory: all code edits on a task branch based on `develop` (verify `git merge-base --is-ancestor develop HEAD`).
- Incremental XAML edits: ONE XAML file per task; build before the next file.
- Business logic in Services/VMs only — code-behind may only forward events to existing VM commands/methods (`[Unamendable]`).
- No native dialogs; DevExpress-first; English only; MD3 terminology.
- Typed text is never cleared by any code path (REQ-DXAC-03).
- DX client-side filtering disabled — Service results shown as-is (REQ-DXAC-06).
- Existing VM test suites must pass UNCHANGED (REQ-DXAC-08). Builder never edits a test to make it pass.
- Spec: `requirements.md` / `design.md` in this folder. AC IDs REQ-DXAC-01…12.
- New `Docs/` files → `.sln` registration in the same commit (docs land on `develop`).

---

### Task 1: Pin the DX 25.2.4 AutoCompleteEdit API surface (SPIKE-lite, no production code)

**Files:**
- Modify: `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/findings.md` (create)

**Interfaces:**
- Consumes: installed NuGet package `devexpress.maui.editors/25.2.*` XML docs.
- Produces: `findings.md` § "Pinned API" — the exact member names Tasks 2–4 substitute where marked ⚠, and the wiring decision **Option A or B** (defined in Task 3).

Online doc indexes are unreliable for this control (DevExpress MCP index empty 2026-07-19; Context7 thin). Ground truth is the installed package's IntelliSense XML.

- [ ] **Step 1: Locate the package XML doc**

Run (Git Bash):
```bash
ls ~/.nuget/packages/devexpress.maui.editors/*/lib/*/DevExpress.Maui.Editors.xml
```
Expected: at least one path for version 25.2.4 (any TFM is fine — API surface is shared).

- [ ] **Step 2: Extract the relevant members**

```bash
grep -o '<member name="[^"]*AutoCompleteEdit[^"]*"' <path-from-step-1> | sort -u
grep -o '<member name="[^"]*AsyncItemsSourceProvider[^"]*"' <path-from-step-1> | sort -u
grep -o '<member name="[^"]*ItemsRequest[^"]*"' <path-from-step-1> | sort -u
```
Confirm/correct these candidates (from Context7, 2026-07-19): `AutoCompleteEdit.SelectedItem`, `AutoCompleteEdit.ItemsSourceProvider` (or direct `ItemsSource`), `AsyncItemsSourceProvider.RequestDelay`, an items-request event using `ItemsRequestEventArgs` (candidate name `ItemsRequested`), `ItemTemplate`, `HasError`/`ErrorText` (inherited from `EditBase`), `LabelText`, `BoxMode`, `DropDownShowMode`, min-input-length property if any (candidate `FilterMinLength`).

- [ ] **Step 3: Record findings + wiring decision**

Write `findings.md` § "Pinned API (2026-07-19)": table of intended-role → exact member name, plus: **choose Option A** (Task 3) if `AsyncItemsSourceProvider` + items-request event exist with a result callback; **else Option B**. If neither the async provider nor a way to disable client filtering on direct `ItemsSource` exists, STOP — status `blocked: spec gap`, escalate to Helder. Do not guess member names.

- [ ] **Step 4: Register findings.md in .sln + commit**

`findings.md` is new under `Docs/` → add `Docs\Management\DevCycleCraft\autocomplete-component\changes\2026-07-19-dx-autocompleteedit-replacement\findings.md` to solution folder `{FA1234BC-0001-4000-8000-000000000053}`'s SolutionItems in `MyVocaList.sln`. Commit:
```bash
git add Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/findings.md MyVocaList.sln
git commit -m "docs(findings): pin DX 25.2.4 AutoCompleteEdit API surface (T1)"
```

---

### Task 2: AutoCompleteEdit form style in MaterialStyles.xaml

**Files:**
- Modify: `MyVocaList/Resources/Styles/MaterialStyles.xaml` (near the implicit `dx:TextEdit` style, lines ~62-71)

**Interfaces:**
- Consumes: Task 1 pinned names (⚠ markers).
- Produces: implicit style `TargetType="dxe:AutoCompleteEdit"` that Tasks 3–4 rely on (no per-page styling needed).

- [ ] **Step 1: Add the implicit style** (mirror the existing TextEdit style values exactly — REQ-DXAC-12):

```xml
<Style TargetType="dxe:AutoCompleteEdit">  <!-- ⚠ namespace prefix per file's existing dxe/dx usage -->
    <Setter Property="BoxMode" Value="Outlined" />
    <Setter Property="FocusedBorderColor" Value="{StaticResource Primary}" />
    <Setter Property="BorderColor" Value="{StaticResource Outline}" />
    <Setter Property="BackgroundColor" Value="{StaticResource SurfaceContainerHighest}" />
    <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
</Style>
```
Copy the exact setter list/resource-key syntax (StaticResource vs DynamicResource/AppThemeBinding) from the adjacent `dx:TextEdit` style — do not invent variants.

- [ ] **Step 2: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add MyVocaList/Resources/Styles/MaterialStyles.xaml
git commit -m "feat(styles): AutoCompleteEdit implicit style matching Outlined TextEdit convention (T2, REQ-DXAC-12)"
```

---

### Task 3: SongFormPage Artist field swap

**Files:**
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml:28-38` (replace `<autocomplete:AutocompleteField …>`)
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (add thin event glue)
- Do NOT modify: `SongFormViewModel.cs` unless Option B requires a public search method rename — it must not; VM stays untouched.

**Interfaces:**
- Consumes: `SongFormViewModel` members — `ArtistSearchText` (string), `ArtistSuggestions` (collection of `AutocompleteSuggestion`), `SearchArtistsCommand` (param: string), `SelectArtistCommand` (param: `AutocompleteSuggestion`), `ArtistBlurredWithoutSelectionCommand`, `HasError`/`ErrorText`, `IsArtistLocked`. Style from Task 2. Pinned names from Task 1.
- Produces: the wiring pattern Task 4 repeats.

**Option A (preferred — async provider):** editor owns debounce (`RequestDelay="300"` — REQ-DXAC-07) and request correlation (stale-result protection); code-behind fulfils requests via the existing Service path.

- [ ] **Step A1: Replace the XAML element** (exact bindings; ⚠ names per Task 1):

```xml
<dxe:AutoCompleteEdit x:Name="artistEdit"
    LabelText="Artist"
    Text="{Binding ArtistSearchText, Mode=TwoWay}"
    IsEnabled="{Binding IsArtistLocked, Converter={StaticResource InverseBoolConverter}}"  <!-- keep the page's EXISTING enable/lock binding expression verbatim from the removed element -->
    HasError="{Binding HasError}"              <!-- ⚠ bindability/direction per T1 -->
    ErrorText="{Binding ErrorText}"            <!-- ⚠ bindability/direction per T1 -->
    ItemsRequested="OnArtistItemsRequested"    <!-- ⚠ event name per T1 -->
    SelectionChanged="OnArtistSelectionChanged"> <!-- ⚠ selection event name per T1 — do not drop this handler -->
    <dxe:AutoCompleteEdit.ItemsSourceProvider> <!-- ⚠ property name per T1 -->
        <dxe:AsyncItemsSourceProvider RequestDelay="300" />
    </dxe:AutoCompleteEdit.ItemsSourceProvider>
    <dxe:AutoCompleteEdit.ItemTemplate>
        <DataTemplate x:DataType="models:AutocompleteSuggestion">
            <VerticalStackLayout Padding="16,8">
                <Label Text="{Binding Headline}" Style="{StaticResource BodyLarge}" />
                <Label Text="{Binding SupportingText}" Style="{StaticResource BodyMedium}" TextColor="{StaticResource OnSurfaceVariant}" />
            </VerticalStackLayout>
        </DataTemplate>
    </dxe:AutoCompleteEdit.ItemTemplate>
</dxe:AutoCompleteEdit>
```
Keep the removed element's row/grid placement attributes verbatim. Reuse existing style keys for the two labels if the page already has equivalents (check page resources before adding new keys).

- [ ] **Step A2: Code-behind glue** (thin forwarding only — no logic):

```csharp
void OnArtistItemsRequested(object sender, ItemsRequestEventArgs e)   // ⚠ args type per T1
{
    var vm = (SongFormViewModel)BindingContext;
    // Fulfil via the EXISTING command path so min-length gates stay in the VM:
    e.Request = () => vm.ArtistSuggestions;                            // ⚠ exact fulfilment shape per T1 findings
    vm.SearchArtistsCommand.Execute(e.Text);
}

void OnArtistSelectionChanged(object sender, EventArgs e)              // wire SelectionChanged → SelectArtistCommand
{
    var vm = (SongFormViewModel)BindingContext;
    if (artistEdit.SelectedItem is AutocompleteSuggestion s)
        vm.SelectArtistCommand.Execute(s);
}
```
Also forward the editor's unfocus (blur) without selection to `ArtistBlurredWithoutSelectionCommand` using the same trigger the old component used (`Unfocused` event): execute only when `SelectedItem == null`. If T1 findings show the provider fulfilment shape differs (e.g. async callback returning `IEnumerable`), adapt the two ⚠ lines to the pinned shape — the invariants are: VM command performs the search; editor never clears `Text`; results come only from the Service.

**Option B (fallback — direct ItemsSource):** bind `ItemsSource="{Binding ArtistSuggestions}"`, disable client filtering with the T1-pinned mechanism, keep `TextChanged`→`SearchArtistsCommand` with the VM-side gate; add a 300 ms debounce in code-behind using `IDispatcherTimer` (restart timer on each change; on tick execute the command). All other bindings identical to Option A. **Stale-result protection (REQ-DXAC-07):** before relying on it, verify the VM already applies latest-query-wins (compare/cancel on new term); if it does not, add it in the VM with unit tests — Option B is not complete without this.

- [ ] **Step 3: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors.

- [ ] **Step 4: Run VM tests unchanged**

Run: `dotnet test MyVocaList.Tests --filter "FullyQualifiedName~SongFormViewModel"`
Expected: PASS, same count as on develop (REQ-DXAC-08).

- [ ] **Step 5: Commit**

```bash
git add MyVocaList/UI/Pages/Songs/SongFormPage.xaml MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs
git commit -m "feat(songs): SongFormPage Artist field uses DX AutoCompleteEdit (T3, REQ-DXAC-01/03-07/12)"
```

---

### Task 4: PersonFormPage Full Name field swap

**Files:**
- Modify: `MyVocaList/UI/Pages/People/PersonFormPage.xaml:19-29`
- Modify: `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs`

**Interfaces:**
- Consumes: Task 3's proven wiring (repeat it — do not redesign). `PersonFormViewModel` members — `PersonName`, `Suggestions`, `SearchPersonsCommand`, `SuggestionSelectedCommand`, `ValidateNameCommand`, `NameHasError`/`NameErrorText`.
- Produces: —

- [ ] **Step 1: Replace the XAML element** — same structure as Task 3 Step A1 with these bindings: `Text="{Binding PersonName, Mode=TwoWay}"`, `HasError="{Binding NameHasError}"`, `ErrorText="{Binding NameErrorText}"`, items event → `OnPersonItemsRequested`, selection → `OnPersonSelectionChanged`, keep the removed element's `LabelText` and layout attributes verbatim. Same `AsyncItemsSourceProvider RequestDelay="300"` and the same two-line `ItemTemplate` (Headline/SupportingText).

- [ ] **Step 2: Code-behind glue** — mirror Task 3 Step A2 with: `SearchPersonsCommand.Execute(e.Text)` (VM keeps its 2-char gate — REQ-DXAC-02), selection → `SuggestionSelectedCommand`, blur-without-selection → `ValidateNameCommand`.

- [ ] **Step 3: Build** — `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors.

- [ ] **Step 4: VM tests unchanged** — `dotnet test MyVocaList.Tests --filter "FullyQualifiedName~PersonFormViewModel"` → PASS, same count.

- [ ] **Step 5: Commit**

```bash
git add MyVocaList/UI/Pages/People/PersonFormPage.xaml MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs
git commit -m "feat(people): PersonFormPage name field uses DX AutoCompleteEdit (T4, REQ-DXAC-02/03-07)"
```

---

### Task 5: Exclude frozen component family from build

**Files:**
- Modify: `MyVocaList/MyVocaList.csproj`
- Modify: `MyVocaList.Tests/MyVocaList.Tests.csproj`
- Create: `MyVocaList/UI/Components/AutocompleteField/README-FROZEN.md`

**Interfaces:**
- Consumes: Tasks 3–4 done (no remaining references to `AutocompleteField`; verify with `grep -r "AutocompleteField" MyVocaList/UI/Pages/` → no matches).
- Produces: solution builds without the 8 component files; 6 test files not executed.

- [ ] **Step 1: Record the before test count** — `dotnet test MyVocaList.Tests` → note total (task-log evidence).

- [ ] **Step 2: csproj exclusions**

In `MyVocaList/MyVocaList.csproj`:
```xml
<ItemGroup>
  <!-- Frozen custom autocomplete (D-AC1, 2026-07-19) — kept as reference for guideline ①, excluded from build. -->
  <Compile Remove="UI\Components\AutocompleteField\**\*.cs" />
  <MauiXaml Remove="UI\Components\AutocompleteField\**\*.xaml" />
</ItemGroup>
```
In `MyVocaList.Tests/MyVocaList.Tests.csproj`:
```xml
<ItemGroup>
  <Compile Remove="Unit\Components\AutocompleteFieldDebounceTests.cs;Unit\Components\AutocompleteFieldProgrammaticTextGuardTests.cs;Unit\Components\AutocompleteSuggestionsPropagationTests.cs;Unit\Components\AutocompleteWindowClassTests.cs;Unit\Components\MobileFieldReopenGuardTests.cs;Unit\Components\AutocompleteSearchGateTests.cs" />
</ItemGroup>
```

- [ ] **Step 3: README-FROZEN.md** — three sentences: frozen per D-AC1 (link `Docs/Management/DevCycleCraft/autocomplete-component/2026-07-19-dx-autocomplete-adoption-decision.md`), excluded from compilation, retained as reference for the future full-screen autocomplete guideline ①.

- [ ] **Step 4: Build solution + after test count** — `dotnet build MyVocaList.sln` → 0 errors; `dotnet test MyVocaList.Tests` → before-count minus the excluded files' tests; record delta in task-log.

- [ ] **Step 5: Commit**

```bash
git add MyVocaList/MyVocaList.csproj MyVocaList.Tests/MyVocaList.Tests.csproj MyVocaList/UI/Components/AutocompleteField/README-FROZEN.md
git commit -m "chore(build): exclude frozen AutocompleteField family from compilation (T5, REQ-DXAC-11)"
```
(README-FROZEN.md is under `MyVocaList/`, not `Docs/` — no `.sln` SolutionItems entry needed.)

---

### Task 6: Full suite + BUG-044/045/047 evaluation checklist

**Files:**
- Modify: `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md` (create if absent; + `.sln` registration in same commit)

- [ ] **Step 1: Full test run** — `dotnet test` (whole solution) → all green; paste summary into task-log (REQ-DXAC-08 evidence).

- [ ] **Step 2: Write the on-device checklist** into task-log for Helder (REQ-DXAC-09), covering on BOTH pages: (a) focus field → type → suggestions appear ≥ gate chars; (b) blur without selection → text intact + validation error shown; (c) select suggestion → selection applied, no navigation stack growth (no modal was pushed — verify via back gesture returning to the previous page, not a stale search view); (d) rapid type-delete-type → no stale popup, no cursor jump to start; (e) rotate/background-resume with popup open → no crash, text intact; (f) REQ-DXAC-06: query with a diacritic/whitespace mismatch (e.g. "cafe " for "Café") → suggestions shown exactly as the Service returned, proving client-side filtering is off; (g) smoke 16C.1: create song end-to-end with a new artist + existing artist.

- [ ] **Step 3: Commit** — task-log + `.sln` entry for it, message `docs(task-log): T6 evidence + on-device evaluation checklist`.

---

### Task 7: Helder device verification (MANUAL — not agent-executable)

- [ ] Helder runs the Task 6 checklist + smoke 16C.1 on device.
- [ ] Green → BACKLOG: this row ✅ (archive per rotation), BUG-027 gate re-evaluated against 16C.1 result, residual-evaluation row (2026-07-19) closed with checklist results; survivors get BUG-NNN rows + regression tests per `bug-tracking.md`.

---

## Self-review

- **Spec coverage:** REQ-DXAC-01/03-07/12→T3(+T2), 02→T4, 08→T3/T4/T6, 09→T6/T7, 10→T7, 11→T5. All 12 covered.
- **Placeholders:** the ⚠ markers are not placeholders — they are pinned by T1 before any dependent task runs; both wiring options carry complete code; escalation path defined if T1 cannot pin.
- **Type consistency:** `AutocompleteSuggestion` (Headline/SupportingText/Data) used consistently; VM member names match the spec's binding map exactly.
