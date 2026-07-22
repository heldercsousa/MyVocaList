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
| 2026-07-22 | `.claude/rules/workflow.md` § Rule 2 — "Docs land on develop `[HARD RULE]`" (spec files, task-log, BACKLOG.md, LEDGER.md, changelog are committed to develop, never left on a worktree branch) | **Narrow, scoped exception authorized by Helder (2026-07-22).** The rule predates two conditions it cannot cover: (1) `BACKLOG.md` is now a **generated artifact** — regeneration is a whole-file rewrite, so a concurrent hand-edit is not a mergeable line conflict but a silent overwrite on the next `regen`; (2) **two sessions are live in the same repo** (this migration + the INLINE-AC fix wave), and INLINE-AC hand-edits BACKLOG bug rows while this migration regenerates the same file. Under the old rule both write develop directly, so a BACKLOG row edited between T12's regeneration and its equivalence gate would be classified as a migration diff hunk when it is another session's work — corrupting the only gate that proves the migration preserved content. **Scope is deliberately narrow:** only the *generated* artifacts move to a worktree — `Docs/Management/BACKLOG.md`, the 5 `Docs/Management/backlog-archive/*.md` files, and the generator-owned item `README.md` files. `task-log.md`, `tasks.md`, `LEDGER.md` and `Docs/Changelog/changelog.md` **continue to land on develop** — the stranding risk the original rule protects against is real and this exception does not touch it. | Worktree for the Spec Evolution destructive phase (T11a–T12b); generated artifacts only | **Expires when the concurrency/write-ownership task lands in the T13 rules bundle** (`tasks.md` → T13d). That task replaces this exception with a general protocol for generated artifacts; this row must then be removed, not renewed. If T13d is descoped, this exception must be re-authorized explicitly rather than silently persisting. |

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
