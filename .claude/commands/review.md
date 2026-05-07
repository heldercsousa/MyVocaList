# Review Command

Post-task review. Run after EVERY completed task before committing.

## Severity Levels
- 🔴 **Blocker** — Must be fixed before any further work. Examples: build failure, `DisplayAlert` usage, cross-layer dependency violation, hardcoded color/string, missing `SafeAreaEdges`.
- 🟡 **Warning** — Should be fixed; may proceed with documented justification. Examples: missing XML doc on a public interface method, single `ReplaceRange` violation with no ANR risk.
- 🟢 **Suggestion** — Optional improvement. Examples: naming refinement, comment clarity.

A task is only `To Review` status when there are zero Blockers.

---

## Checklist

### 1. Build
- [ ] 🔴 Run `/project:build` — confirm 0 errors

### 2. Code Quality
- [ ] 🔴 No `DisplayAlert`, `DisplayActionSheet`, or `DisplayPromptAsync` in modified files
- [ ] 🔴 No hardcoded colors (use `{StaticResource X}` or `{dx:ThemeColor X}`)
- [ ] 🟡 No hardcoded font sizes or magic numbers without a comment
- [ ] 🟡 No hardcoded strings in UI (Text must be a resource or binding, not a raw string literal in logic)
- [ ] 🔴 No non-English text introduced in code, comments, or UI strings
- [ ] 🟡 No empty catch blocks (except the two documented exceptions in code-principles.md)
- [ ] 🟡 No new try-catch where GlobalExceptionHandler already covers it

### 3. MAUI / .NET 10 Specifics
- [ ] 🔴 Any new `ContentPage` has `SafeAreaEdges="Container"` set (breaking change in .NET MAUI 10)
- [ ] 🟡 No deprecated APIs used (check `maui-current-apis` skill if unsure)

### 4. Architecture
- [ ] 🔴 Business logic is in Services, not ViewModels or pages
- [ ] 🔴 No direct Infra dependency from Services or MAUI (only MAUI → Infra for DI wiring)
- [ ] 🟡 New `using` statements that apply to 2+ types in a project → added to that project's `GlobalUsings.cs`
- [ ] 🟡 New `using` statements that apply across 2+ projects → added to `Directory.Build.props`

### 5. DevExpress
- [ ] 🔴 Check `.claude/rules/devexpress-patterns.md` — no stock MAUI control used where a DX equivalent exists
- [ ] 🟡 No inline styles on DevExpress controls where a Style exists in `MaterialStyles.xaml`

### 6. Spec Consistency
- [ ] 🔴 If the task changed any behavior defined in `Docs/specs/[feature]/`, confirm the corresponding `requirements.md` and/or `design.md` were updated
- [ ] 🔴 Flag any spec file that describes behavior that no longer matches the implementation
- [ ] 🟡 Open `design.md` for the feature — do all interface signatures match what is in code?
- [ ] 🟡 Open `requirements.md` — is each acceptance criterion verified by a test or a manual check?
- [ ] 🟡 Is anything implemented that is explicitly listed as "Out of Scope"?
- [ ] 🔴 If code deviated from `design.md`, was `design.md` updated?

### 7. Spec Conformance
- [ ] 🟡 **Diff scope:** Does the diff touch only the files this task was scoped to? Extra files: are they incidental (formatting) or material (undeclared feature)?
- [ ] 🔴 **Intent alignment:** Does the code embody `design.md` intent, not a simpler interpretation? Example of intent drift: spec says "sort by relevance"; agent implemented alphabetical.
- [ ] 🔴 **No spec update skipped:** If the implementation deviates from `design.md`, was `design.md` updated first? If not — this is spec drift. Reject and request spec update before re-review.
- [ ] 🟡 For each checked task in `tasks.md`: does the committed code implement the acceptance criteria stated in that task?
- [ ] 🟡 Are there any code paths or UI behaviors present that are NOT described in `requirements.md` or `design.md`? (Scope creep indicator)

### 8. Spec Alignment
- [ ] 🔴 Relevant spec file(s) identified and read before implementation
- [ ] PR changes behavior:
  - [ ] 🔴 Yes — spec updated and version-noted (`> **Spec updated [date]:** ...` in file)
  - [ ] 🟢 No — refactor/optimization only (confirm: no new behavior)
- [ ] 🔴 Read `requirements.md` acceptance criteria — each criterion has a corresponding test or implementation path
- [ ] 🔴 Read `design.md` — service interface signatures match design; no behaviors in design are absent from implementation
- [ ] 🔴 Validation rules documented in the spec (name length, required fields, uniqueness) are enforced in the Service layer with a corresponding service unit test
- [ ] 🔴 Every acceptance criterion has test coverage (not just `NotNull` assertions — actual value/behavior assertions)
- [ ] 🔴 No acceptance criteria are unmapped (if any, mark as `blocked: spec gap` not `To Review`)
- [ ] 🔴 If implementation differs from spec: spec is updated through change control (documented in commit message), NOT silently changed in code

### 9. AC Traceability
For each AC in `requirements.md`, confirm:

| AC | Implementation location | Test that fails if AC is violated |
|----|------------------------|-----------------------------------|
| [AC text] | [file + method] | [TestClass.TestMethod] |

- [ ] 🔴 If any AC row has no test → task is INCOMPLETE — return to implementation
- [ ] 🔴 If any AC row has no implementation reference → task is INCOMPLETE — return to implementation

A passing test suite with unmapped ACs is not verified; it is untested.

### 10. Spec Drift Detection
- [ ] 🟡 Check if `requirements.md` or `design.md` was updated since the last task was reviewed. If yes, verify tasks completed before the spec update still conform to the new spec.
- [ ] 🟡 For each spec update in git log since last review, identify which tasks it affects. If an affected task is already marked "To Review" or "Review task done", it may need re-review.
- [ ] 🟡 Does the implemented behavior match the acceptance criteria in `requirements.md`?
- [ ] 🟡 Were any validation rules changed during implementation? Update `requirements.md` if so.
- [ ] 🟡 Were any design decisions reversed or altered? Update `design.md` Key Decisions if so.
- [ ] 🟡 Are all `tasks.md` checkboxes accurate (checked = done, unchecked = not done)?

If spec drift is found: update the spec files in the same commit as the code. Never leave specs out of sync with working code.

### 11. Drift Categories (silent divergence checks)
- [ ] 🔴 **Behavioral contracts:** every validation rule in the spec (name length, uniqueness, required fields) has a corresponding branch in the service + a unit test for the failure path
- [ ] 🟡 **Permission/security rules:** access gates documented in `design.md` exist in the service
- [ ] 🟡 **Suppression audit:** any `#pragma warning disable`, `// ReSharper disable`, or `[SuppressMessage]` added in this task has a comment explaining why and is logged in `.claude/exception-registry.md`
- [ ] 🟡 **Scope conflict check:** if this task and another recent task both touch the same domain entity (e.g., Venue, Singer, Queue), verify the changes compose without silent incompatibility (interface change + caller change in sync)

### 12. Spec-Code Consistency
- [ ] 🟡 Does the implementation match the feature's `design.md`? Check key interfaces, data flow, and validation rules.
- [ ] 🔴 If implementation diverged from the spec (different approach chosen, new constraint discovered), has `design.md` been updated to reflect the actual design?
- [ ] 🟡 If a new edge case was handled that the spec did not cover, has `requirements.md` been updated?
- [ ] 🟡 Tasks that were completed — are they checked off in `tasks.md`?

Note: Internal refactors that do not change external behavior do NOT require spec updates. Only update specs when behavior or design intent changes.

---

## After Review — Mandatory Enhancement Check

> "Are there patterns, constraints, or lessons from this task that should be added to any rules or commands file? If yes, propose the additions before committing."

This step is **not optional**. If the task revealed:
- A new confirmed DevExpress pattern → add to `devexpress-patterns.md`
- A new code principle confirmed → add to `code-principles.md`
- A new dialog/validation pattern → add to `dialogs-validation.md`
- A new architecture decision → update `CLAUDE.md`
- A new constraint discovered → add to `.claude/rules/constraints-registry.md`
- A recurring mistake or question → create a new rules file

Propose the specific text addition(s) to Helder before committing.
