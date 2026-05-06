# Review Command

Post-task review. Run after EVERY completed task before committing.

## Checklist

### 1. Build
- [ ] Run `/project:build` — confirm 0 errors

### 2. Code Quality
- [ ] No `DisplayAlert`, `DisplayActionSheet`, or `DisplayPromptAsync` in modified files
- [ ] No hardcoded colors (use `{StaticResource X}` or `{dx:ThemeColor X}`)
- [ ] No hardcoded font sizes or magic numbers without a comment
- [ ] No hardcoded strings in UI (Text must be a resource or binding, not a raw string literal in logic)
- [ ] No non-English text introduced in code, comments, or UI strings
- [ ] No empty catch blocks (except the two documented exceptions in code-principles.md)
- [ ] No new try-catch where GlobalExceptionHandler already covers it

### 3. MAUI / .NET 10 Specifics
- [ ] Any new `ContentPage` has `SafeAreaEdges="Container"` set (breaking change in .NET MAUI 10)
- [ ] No deprecated APIs used (check `maui-current-apis` skill if unsure)

### 4. Architecture
- [ ] Business logic is in Services, not ViewModels or pages
- [ ] No direct Infra dependency from Services or MAUI (only MAUI → Infra for DI wiring)
- [ ] New `using` statements that apply to 2+ types in a project → added to that project's `GlobalUsings.cs`
- [ ] New `using` statements that apply across 2+ projects → added to `Directory.Build.props`

### 5. DevExpress
- [ ] Check `.claude/rules/devexpress-patterns.md` — no stock MAUI control used where a DX equivalent exists
- [ ] No inline styles on DevExpress controls where a Style exists in `MaterialStyles.xaml`

## After Review — Mandatory Enhancement Check

> "Are there patterns, constraints, or lessons from this task that should be added to any rules or commands file? If yes, propose the additions before committing."

This step is **not optional**. If the task revealed:
- A new confirmed DevExpress pattern → add to `devexpress-patterns.md`
- A new code principle confirmed → add to `code-principles.md`
- A new dialog/validation pattern → add to `dialogs-validation.md`
- A new architecture decision → update `CLAUDE.md`
- A recurring mistake or question → create a new rules file

Propose the specific text addition(s) to Helder before committing.
