# Exception Registry — MyVocaList

Approved exceptions to constitutional constraints. Review quarterly.

Exceptions that accumulate (3+ for the same constraint) signal the constraint may need amendment.
If a constitutional constraint cannot be followed in a specific case, document it here before deviating.
**Never deviate silently** — silent deviations accumulate as hidden technical debt.

---

## Registry

| Date | Constraint violated | Reason for exception | Code location | Expires |
|------|---------------------|----------------------|---------------|---------|
| 2026-07-11 | CLAUDE.md § Constitutional Constraints — UI Component Priority (DevExpress first, always) | Evaluated DevExpress MAUI 25.2.4's `AutoCompleteEdit`/`FilteredItemsSourceProvider`/`AsyncItemsSourceProvider` as a replacement for the hand-rolled `AutocompleteField` (BACKLOG ② evaluation, `Docs/Management/DevCycleCraft/autocomplete-component/findings.md`). No deployable demo exists anywhere (DX demo-app repo, NuGet package, doc examples); BottomSheet-hosting compatibility is unproven and conflicts with this project's own documented BottomSheet/keyboard-conflict rule (`dialogs-validation.md`); dual local+remote provider composition is unconfirmed by any doc. Given the pending (not yet "go") Blazor Hybrid + MudBlazor migration also makes any MAUI-XAML investment (DX-based or hand-rolled) non-portable regardless, DX showed no clear win to justify the integration risk — decided (Helder, 2026-07-11) to extend the existing hand-rolled `AutocompleteField` instead of adopting the DevExpress editor. | `MyVocaList/UI/Components/AutocompleteField/` (component to be extended, not replaced) | Re-evaluate if the MudBlazor migration spike returns a "no-go" (DevExpress-first regains full force for MAUI) or if DevExpress ships a proven BottomSheet + async-dual-source example in a future release. |

---

## How to add an entry

When a constitutional constraint (CLAUDE.md Non-Negotiables, architecture rules, code-principles.md) cannot
be followed in a specific case:

1. Add a row to the table above before writing the non-conforming code.
2. **Date:** ISO date `YYYY-MM-DD`
3. **Constraint violated:** quote the rule (or the file + section)
4. **Reason:** one sentence minimum — what makes this case different
5. **Code location:** file path + line or class name
6. **Expires:** a condition or date after which the exception should be removed, or `permanent` if indefinite

## Quarterly audit

At each quarterly constitutional audit (see CLAUDE.md), review this registry for:
- Exceptions older than their expiry condition — remove the deviation or promote to rule amendment
- Entries where the same constraint is listed 3+ times — the constraint may be wrong; escalate for review
- Entries with no expiry — evaluate whether the exception should become a formal rule amendment
