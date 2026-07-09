# Solution Review Command (`/sln-review`)

Post-task review of THIS solution's changes — distinct from the built-in `/review` skill, which reviews GitHub PRs. Run after EVERY completed task before committing.

## Severity Levels
- 🔴 **Blocker** — Must be fixed before any further work. Examples: build failure, `DisplayAlert` usage, cross-layer dependency violation, hardcoded color/string, missing `SafeAreaEdges`.
- 🟡 **Warning** — Should be fixed; may proceed with documented justification. Examples: missing XML doc on a public interface method, single `ReplaceRange` violation with no ANR risk.
- 🟢 **Suggestion** — Optional improvement. Examples: naming refinement, comment clarity.

A task is only `To Review` status when there are zero Blockers.

Test quality criteria per `testing.md` must also pass (see Test Quality Audit Checklist in `testing.md`) before setting `To Review`.

---

## Checklist

### 1. Build
- [ ] 🔴 Run `/sln-build` — confirm 0 errors

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

---

## Spec and AC Verification

### 6. Spec Consistency *(primary spec check — canonical for all spec-vs-code verification)*
- [ ] 🔴 If the task changed any behavior defined in `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/`, confirm the corresponding `requirements.md` and/or `design.md` were updated
- [ ] 🔴 Flag any spec file that describes behavior that no longer matches the implementation
- [ ] 🔴 If code deviated from `design.md`, was `design.md` updated?
- [ ] 🔴 **Interface signatures:** Open `design.md` for the feature — do all interface signatures in the code match `design.md`? *(Consolidates checks from former §8 and §12)*
- [ ] 🟡 Open `requirements.md` — is each acceptance criterion verified by a test or a manual check? *(See §9 AC Traceability for the full mapping table)*
- [ ] 🟡 Is anything implemented that is explicitly listed as "Out of Scope"?

### 9. AC Traceability *(canonical AC-to-test mapping — referenced by §6 and §7)*
For each AC in `requirements.md`, confirm:

| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| [AC-N] | [brief description] | [file + method] | [TestClass.TestMethod] |

- [ ] 🔴 If any AC row has no test → task is INCOMPLETE — return to implementation
- [ ] 🔴 If any AC row has no implementation reference → task is INCOMPLETE — return to implementation

**Exception:** Level C code (per `testing.md` TDD Level Guidance) is exempt from mandatory test coverage. Document the Level C classification in the task-log when no test is written for a listed AC.

A passing test suite with unmapped ACs is not verified; it is untested.

### 7. Spec Conformance
- [ ] 🟡 **Diff scope:** Does the diff touch only the files this task was scoped to? Extra files: are they incidental (formatting) or material (undeclared feature)?
- [ ] 🔴 **Intent alignment:** Does the code embody `design.md` intent, not a simpler interpretation? Example of intent drift: spec says "sort by relevance"; agent implemented alphabetical.
- [ ] 🔴 **No spec update skipped:** If the implementation deviates from `design.md`, was `design.md` updated first? If not — this is spec drift. Reject and request spec update before re-review. *(See §6 Spec Consistency)*
- [ ] 🟡 For each checked task in `tasks.md`: does the committed code implement the acceptance criteria stated in that task? *(See §9 AC Traceability)*
- [ ] 🟡 Are there any code paths or UI behaviors present that are NOT described in `requirements.md` or `design.md`? (Scope creep indicator)

### 8. Spec Alignment
- [ ] 🔴 Relevant spec file(s) identified and read before implementation
- [ ] PR changes behavior:
  - [ ] 🔴 Yes — spec updated and version-noted (`> **Spec updated [YYYY-MM-DD]:** ...` in file)
  - [ ] 🟢 No — refactor/optimization only (confirm: no new behavior)
- [ ] 🔴 Read `requirements.md` acceptance criteria — each criterion has a corresponding test or implementation path *(See §9 AC Traceability)*
- [ ] 🔴 Read `design.md` — service interface signatures match design; no behaviors in design are absent from implementation *(See §6 for interface signature check)*
- [ ] 🔴 Validation rules documented in the spec (name length, required fields, uniqueness) are enforced in the Service layer with a corresponding service unit test
- [ ] 🔴 No acceptance criteria are unmapped (if any, mark as `blocked: spec gap` not `To Review`)
- [ ] 🔴 If implementation differs from spec: spec is updated through change control (documented in commit message), NOT silently changed in code *(See §6 Spec Consistency)*

### 10. Spec Drift Detection
- [ ] 🟡 Check if `requirements.md` or `design.md` was updated since the last task was reviewed. If yes, verify tasks completed before the spec update still conform to the new spec. *(See §6 Spec Consistency)*
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
- [ ] 🟡 Does the implementation match the feature's `design.md`? Check key interfaces, data flow, and validation rules. *(See §6 for interface signature check; see §6 Spec Consistency for the canonical spec-vs-code check)*
- [ ] 🔴 If implementation diverged from the spec (different approach chosen, new constraint discovered), has `design.md` been updated to reflect the actual design? *(See §6 Spec Consistency)*
- [ ] 🟡 If a new edge case was handled that the spec did not cover, has `requirements.md` been updated?
- [ ] 🟡 Tasks that were completed — are they checked off in `tasks.md`?

Note: Internal refactors that do not change external behavior do NOT require spec updates. Only update specs when behavior or design intent changes.

---

### 13. Enhancement Check *(main agent step — performed after the subagent commits and stops)*

> "Are there patterns, constraints, or lessons from this task that should be added to any rules or commands file? If yes, propose the additions before committing."

**This step is performed by the main agent after the subagent commits and stops.** Subagents do not perform this step — it is outside their exit checklist scope. See `.claude/agents/verifier.md` for the authoritative enhancement checklist.

This step is **not optional**. If the task revealed:
- A new confirmed DevExpress pattern → add to `devexpress-patterns.md`
- A new code principle confirmed → add to `code-principles.md`
- A new dialog/validation pattern → add to `dialogs-validation.md`
- A new architecture decision → update `CLAUDE.md`
- A new constraint discovered → add to `.claude/rules/constraints-registry.md`
- A recurring mistake or question → create a new rules file

Propose the specific text addition(s) to Helder before committing.

Also run the Session-End Spec Update Ritual (`workflow.md` Rule 3a) to update spec files with any decisions or discoveries from this task.
