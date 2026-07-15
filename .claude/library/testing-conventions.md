# Testing — Reference — Naming conventions + what to test per layer

> Section file split from `testing-reference.md` on 2026-07-14 (token-scoped reads). Index + provenance: `testing-reference.md`. Never-miss rules: `.claude/rules/testing.md`.

## Naming Conventions

### Test Class
`{Subject}Tests` where subject is the class under test:
- `VenueServiceTests`
- `VenuesViewModelTests`
- `VenueRepositoryTests`

### Test Method
`{Method}_{Context}_{Expected}`:
- `CreateVenueAsync_NameTooLong_ReturnsFalse`
- `InitializeAsync_EmptyDb_SetsIsEmptyNoVenues`
- `SearchAsync_CaseInsensitive_FindsMatch`

All three parts are required. No generic names like `Test1`, `CreateVenue_Works`, or `ItWorks`.

---

## What to Test

| Layer | Test | Skip |
|---|---|---|
| **Service** | Validation logic, business rules, tuple return values, error messages | Framework plumbing, DI wiring |
| **ViewModel** | Command execution, state after commands, derived properties (AppBarTitle, IsEmpty*), CanExecute gates | XAML bindings, Shell.Current calls, DX control state |
| **Repository** | CRUD operations, search/filter queries, unique constraint enforcement, EF configuration | EF migration internals, DTO mapping done by services |

### Service — what defines a test boundary
Every `if` branch in a service method that changes the return value is a test case:
```csharp
// This generates 3 test cases:
if (name.Length > MaxLength) return (false, "Name too long", null);       // test 1
if (await _repo.ExistsByNameAsync(name)) return (false, "Duplicate", null); // test 2
// ... success path                                                          // test 3
```

### ViewModel — focus on observable state
Test what the ViewModel exposes to the view, not how it calls services internally:
- `[ObservableProperty]` values after commands run
- Derived `bool` properties (`CanEditSelected`, `IsEmptyNoVenues`, `IsAllSelected`)
- `AppBarTitle` string derived from `SelectedCount`

### Repository — always test the query, not just CRUD
For every repository method that takes a filter/sort/page parameter, write one test that verifies the filtering is actually applied:
```csharp
// Not enough:
[Fact] async Task AddAsync_PersistsVenue() { ... }

// Required:
[Fact] async Task GetPagedAsync_SearchQuery_ReturnsOnlyMatching() { ... }
[Fact] async Task GetPagedAsync_Page2_SkipsFirstPage() { ... }
```

---
