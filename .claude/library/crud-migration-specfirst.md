# CRUD Page Design Laws — Page migration checklist + spec-first development

> Section file split from `crud-pages.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `crud-pages.md`.

## Page migration checklist

Use when migrating an existing CRUD list page to `CrudListView` (or building a new one from scratch).

**XAML**
- [ ] Root element is `<pages:CrudListPageBase>` with `xmlns:pages="clr-namespace:MyVocaList.UI.Pages.Base"`
- [ ] `xmlns:views="clr-namespace:MyVocaList.UI.Components"` declared for CrudListView
- [ ] `SafeAreaEdges="Container"` present on the root element
- [ ] `Shell.BackButtonBehavior IsVisible="False" IsEnabled="False"` present
- [ ] `Shell.TitleView` contains `Grid` with `SmallAppBar` + `SearchAppBar`
- [ ] Single `<views:CrudListView>` element as page content — no manual ShimmerView, DXCollectionView, FloatingToolbar, EmptyState, or BottomSheet in the page XAML
- [ ] All required BindableProperties set (`ItemsSource`, `SelectedItemsSource`, `IsEmptyNoItems`, `EmptyNoItemsIllustration`, `EmptyNoItemsHeadline`, `FabCommand`, `FabDescription`)
- [ ] `ItemTemplate` and `SelectedItemTemplate` slots defined with entity-specific DataTemplates

**Code-behind**
- [ ] Class inherits `CrudListPageBase`
- [ ] `ListViewModel` abstract property implemented (`protected override ICrudListViewModel ListViewModel => _viewModel;`)
- [ ] `ViewModel` public property present for compiled-binding DataTemplates
- [ ] `AttachViewModel()` called from constructor
- [ ] No `OnCollectionViewScrolled`, `OnSelectionChanged`, or `OnConfirmSheetStateChanged` overrides — CrudListView owns these

**ViewModel**
- [ ] Inherits `CrudListViewModelBase<TDto>`
- [ ] All abstract methods implemented: `FetchPageAsync`, `FetchMoreAsync`, `ExecuteDeleteAsync`, `BuildDeleteConfirmMessage`, `NavigateToAddAsync`, `NavigateToEditAsync`, `RaiseEntityEmptyStateProperties`
- [ ] Entity-specific `IsEmptyNoXxx` bool property present (e.g. `IsEmptyNoVenues`) and raised inside `RaiseEntityEmptyStateProperties`
- [ ] `IList SelectedXxxRaw` non-generic wrapper property present for `SelectedItemsSource` binding

**DI**
- [ ] Page and ViewModel both registered as `AddTransient` in `MauiProgram.cs`

---

## Spec-First Development

Every new CRUD feature gets a spec before any code is written. Copy the structure from `Docs/specs/venues/` — three files:

| File | What it answers |
|------|----------------|
| `Docs/Management/[section-or-filing-dir]/[feature]/requirements.md` | What the feature must do. User stories, acceptance criteria, data model, validation rules, out-of-scope. |
| `Docs/Management/[section-or-filing-dir]/[feature]/design.md` | How it works technically. Architecture layers, interfaces, page structure, interaction flows, error handling, key decisions. |
| `Docs/Management/[section-or-filing-dir]/[feature]/tasks.md` | Ordered, checkboxed implementation steps. Checked off as work completes. |

The spec is the contract. Code that contradicts the spec is a bug or a spec update — one of the two must change.

**Requirement syntax (GEARS):** Write acceptance criteria as `shall` statements:
```
When [trigger], the [subject] shall [behavior].
While [state], the [subject] shall [behavior].
If [condition], then the [subject] shall [behavior].
```
One behavior per sentence. One sentence per line.

### Collaborative workflow — how to start a new CRUD with Claude

1. **Brainstorm** — invoke `superpowers:brainstorming`. Discuss the feature together: data model, UX flows, edge cases, approaches. Reach agreement on the design before any writing.
2. **Write spec** — Claude writes `Docs/Management/[section-or-filing-dir]/[feature]/requirements.md`, `design.md`, `tasks.md` based on the agreed design. User reviews and approves.
3. **Write plan** — invoke `superpowers:writing-plans`. Claude produces `Docs/Management/[section-or-filing-dir]/[feature]/plan.md` — the step-by-step implementation plan with code templates.
4. **Implement** — invoke `superpowers:executing-plans` (or `superpowers:subagent-driven-development`). Follow the plan task by task, building against the spec.
5. **Review** — invoke `superpowers:requesting-code-review` after each major task or phase.

Never skip straight to implementation. Brainstorm → Spec → Plan → Implement → Review, in that order.

---
