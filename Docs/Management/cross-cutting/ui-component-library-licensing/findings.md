# Findings — UI component library licensing (2026-11-21)

Each claim is marked **V** (verified on the official page) or **I** (inferred or third-party). Nothing here is a decision.

## 1. DevExpress .NET MAUI

- **The free offer has ended.** DevExpress's page says it "expired at 23:59 GMT on December 31, 2024". It never included support. **V** — https://devexpress.com/xamarin-free
- **What existing users keep:** people who signed up before the deadline may keep using MAUI **v21.2.x–v24.1.x** (Radial Gauge v23.2–24.1). Same page. **V**
  - The page does not say whether that use is permanent, or whether NuGet feed access continues.
- **This repo uses 25.2.4** (`Directory.Packages.props`: `DevExpress.Maui.Core`, `.CollectionView`, `.Controls`, `.Editors`), which is **outside** the free range. **V**
  - Going back to 24.1 on net10.0 is unlikely to be practical. **I**
- **Current model:** MAUI is sold only inside the Universal subscription, about US$2,300/year. **V** — https://www.devexpress.com/maui/
  - A reseller lists about US$2,254 new and US$1,150 to renew. **I** — https://www.componentsource.com/product/devexpress-maui/prices
- **EULA:** a 12-month subscription, "licensed, not sold". Free offers can end at any time. The trial forbids building apps and redistributing the DLLs. **V** — https://www.devexpress.com/Support/EULAs/maui.xml
  - The EULA does not say clearly whether versions obtained during a subscription stay usable after it lapses.
- **Conflicting signal:** the DevExpress docs still link to a "get your free copy" page. **V** — https://docs.devexpress.com/maui
- **Action:** ask clientservices@devexpress.com before deciding.

## 2. Syncfusion Community License

https://www.syncfusion.com/products/communitylicense — **V**
- **Eligibility:** under US$1M annual gross revenue, 5 or fewer developers, 10 or fewer employees, and never more than US$3M in outside capital. Government or taxpayer-funded bodies are excluded.
- **Coverage:** the full UI suite, including .NET MAUI. Commercial apps are allowed.
- **Term:** no fixed term; the license "continues until you or your company are ineligible". Approval is a ticket-based eligibility check.
- Helder (solo, pre-revenue) qualifies. If the paid phase ever grows past the caps, a paid license is needed then.

## 3. MudBlazor (MAUI Blazor Hybrid)

- **License:** MIT. **V** — https://github.com/MudBlazor/MudBlazor
- **.NET support:** v9.x supports .NET 8–11. **V**
- **Material version:** follows **Material Design 2**; no Material 3 roadmap found. **I** This conflicts with the MD3 terminology/design constraint.
- **Runtime:** Blazor inside a WebView (Android WebView, WKWebView). Long lists need `Virtualize`; scrolling and gestures feel like a web page, not native. **I**
- **Effort:** a full rewrite of all 39 XAML files and their code-behind into Razor. Only the ViewModels, Services and data layer carry over.

## 4. Other options (one line each, all **I**)

- **.NET MAUI Community Toolkit:** MIT; has a Popup but no native bottom sheet or chips.
- **Uranium UI:** MIT; Material-style inputs, chips and a bottom sheet; a small project.
- **Telerik UI for .NET MAUI:** paid only.
- **Stock MAUI + own Material 3 styles:** free and no lock-in, but the most work.

## 5. Current DevExpress footprint (codebase scan, 2026-11-21)

| Item | Finding |
|---|---|
| XAML files using DX | 39, including `AppShell.xaml` and `Resources/Styles/MaterialStyles.xaml` |
| C# | `GlobalUsings.cs` imports 4 DX namespaces. 20 `.cs` files reference DX types, **including ViewModels** (`CrudListViewModelBase`, `ICrudListViewModel`, `PersonsViewModel`, `VenuesViewModel`) — a layer leak. 1 test file. |
| Control counts | DXButton ≈66, TextEdit 15, DXBorder 11, BottomSheet ≈11, ShimmerView ≈7, DXCollectionView ≈4, DXImage 4, ComboBoxEdit 3, Filter/ChoiceChipGroup 2, AutoCompleteEdit 1, MultilineEdit 1, PasswordEdit 1, CheckEdit 1, DXPopup 1, DXScrollView 1 |
| Wrapper components (the chokepoints) | `CrudListView` (CollectionView + Shimmer + 2 sheets); `AutocompleteField`, `AutocompleteMobileField`, `AutocompleteDXField`, `TapToEditField`; `SearchAppBar`, `SearchBar`, `SmallAppBar`, `FloatingToolbar`; `ConfirmSheet` and 3 update/what's-new sheets; `Menu` (DXPopup), `OverflowMenuButton`; the `ListItem*` slot components; `ImagePickerField`, `EmptyState` |
| Pages | 14, plus `AppShell` |

## 6. Comparison and recommendation

| | Syncfusion Community | MudBlazor Hybrid |
|---|---|---|
| Cost | Free while eligible | Free (MIT) |
| Rewrite size | Control-for-control swap in XAML; the work sits in about 24 wrappers + `MaterialStyles.xaml`; pages change lightly | Full UI rewrite in Razor |
| Rough effort (solo) | about 1–2 weeks **I** | several weeks **I** |
| Native feel / list performance | Native controls | WebView |
| MD3 fit | To be checked in the spike | MD2 |
| Project rules | Needs the `[Unamendable]` review | Needs the review, and conflicts with MD3 and the native-dialog rule's intent |

**Recommendation:** Syncfusion Community License, if DevExpress confirms 25.2.4 is not licensed for free use. Reject MudBlazor. Validate with a time-boxed spike (`CrudListView` + Venues) before committing to the migration. Every governed component touched needs its own task under `component-change-governance.md`.
