# Claude Code Task: Generate Rules, Commands, and Update CLAUDE.md

## Before Starting — Enable MCPs
Run `/mcp` and temporarily enable `devexpress-maui` GitMCP.
You will disable it again at the end of this task.

## Preparation — Read Everything First
Before writing a single file, read ALL of the following:

**Codebase:**
- `CLAUDE.md`
- `MauiProgram.cs`
- `App.xaml` and `AppShell.xaml`
- `Directory.Build.props`
- Every `GlobalUsings.cs` in every project
- Every existing `.xaml` page in the MAUI project
- Every existing ViewModel
- `GlobalExceptionHandler.cs` if it exists in Services
- All existing files in `.claude/rules/` and `.claude/commands/`

**Plugins (active during this task — consult all):**
- `obra/superpowers` — TDD, systematic debugging
- `nesbo/dotnet-claude-code-skills` — ddd-dotnet, data-dotnet, bdd-dotnet
- `Aaronontheweb/dotnet-skills` — .NET quality patterns
- `teslasoft-de/claude-skills-marketplace` — ux (UX design, information architecture)
- `manutej/luxor-claude-marketplace` — mobile-design (touch patterns, gestures, mobile UX)

**Skills — read SKILL.md for each:**
- `.claude/skills/maui-current-apis/`
- `.claude/skills/maui-performance/`
- `.claude/skills/maui-safe-area/`
- `.claude/skills/maui-data-binding/`
- `.claude/skills/maui-dependency-injection/`
- `.claude/skills/dotnet-skills/` (if present)
- `.claude/skills/ddd-dotnet/` (if present)
- `.claude/skills/maui-unit-testing/` (if present)

**DevExpress GitMCP:**
- Fetch DevExpress MAUI theming and styling patterns
- Fetch DevExpress editors/validation patterns
- Fetch DevExpress BottomSheet/dialog patterns
- Fetch DevExpress CollectionView patterns

---

## Files to Create or Update

### 1. `.claude/commands/build.md`
Generate a build command appropriate for .NET MAUI 10 on Windows with Android target.
Base it on what `dotnet-skills`, `maui-current-apis`, and `maui-hot-reload-diagnostics` skills
recommend for MAUI builds. Consider:
- Correct dotnet build invocation for MAUI 10 Android
- How to interpret MAUI-specific warnings vs errors
- After N consecutive failed fix attempts (suggest 3), stop looping and instead:
  - Identify which skill is most relevant to the error type
  - Ask for guidance rather than continuing blind fix attempts
  - Never present work as complete while errors remain

### 2. `.claude/commands/commit.md`
Commit workflow. Commit types must match changelog types exactly:
`feat:` `fix:` `refactor:` `docs:` `perf:` `test:`

Include:
- Pre-commit checklist (build clean, no half-finished work)
- Files to never commit (`.claude/settings.local.json`, `bin/`, `obj/`, `.vs/`)
- After committing, always run `/project:changelog`

### 3. `.claude/commands/changelog.md`
Changelog update workflow. Types must match commit types exactly:
`feat` `fix` `refactor` `docs` `perf` `test`

File: `Docs/Changelog/changelog.md`
Format: `- **MM/dd/yyyy** - <type> - <description>`

### 4. `.claude/commands/review.md`  ← NEW
Post-task review command. Run this after EVERY completed task before committing.

When reviewing UX/interaction patterns, consult `ux@teslasoft-skills` and
`mobile-design@manutej-luxor-claude-marketplace` plugins in addition to rules files.

The review must check:
- Build is clean
- Code follows rules in `.claude/rules/code-principles.md`
- UI follows rules in `.claude/rules/devexpress-patterns.md`
- No native dialogs used (check for DisplayAlert, DisplayActionSheet, DisplayPromptAsync)
- No hardcoded colors, sizes, or strings
- No non-English text introduced
- No try-catch used where GlobalExceptionHandler should handle it
- SafeAreaEdges set on any new ContentPage
- GlobalUsings.cs updated if new using applies to 2+ types in the project

**After review, always ask:**
> "Are there patterns, constraints, or lessons from this task that should be
> added to any rules or commands file? If yes, propose the additions before committing."

This incremental enhancement step is mandatory — it ensures rules improve continuously
as the project evolves.

### 5. `.claude/rules/theme-locale.md`
Generate from:
- Actual `MauiProgram.cs` ThemeManager configuration
- Actual `App.xaml` ResourceDictionary contents
- DevExpress GitMCP theming and styling patterns — primary source
- `maui-performance`, `maui-current-apis`, and `maui-data-binding` skills

Note: Only one page exists in the codebase and it has known issues — do NOT
use it as a theming reference. Trust DevExpress GitMCP and skills over any
existing page patterns at this stage.

Document inline property exceptions where DevExpress requires them by design.
Include the full list of DevExpress ThemeColor tokens.

### 6. `.claude/rules/code-principles.md`
Generate from:
- Existing Services code — observe actual error handling patterns in use
- `GlobalExceptionHandler.cs` — document what it catches and how
- `dotnet-skills` and `ddd-dotnet` skills
- `maui-current-apis` skill for .NET 10 specific guidance

On try-catch: rule is "avoid where GlobalExceptionHandler suffices" not "never".
Document the allowed scenarios clearly with examples from the actual codebase.

On ViewModels: document both patterns:
- DTOs (Contracts layer) for data transfer
- Page-specific ViewModels where a UI-formatted version of data is needed

Populate the Global Usings section from actual `Directory.Build.props`
and each project's `GlobalUsings.cs`.

### 7. `.claude/rules/dialogs-validation.md`
Generate from:
- Existing XAML pages — find actual working BottomSheet and HasError implementations
- DevExpress GitMCP dialog and editor patterns
- Do NOT invent XAML — only document patterns confirmed to compile and work

If no working BottomSheet exists yet in the codebase, fetch the pattern from
DevExpress GitMCP and mark it as "reference pattern — verify on first use".

### 8. `.claude/rules/devexpress-patterns.md` (update existing stub)
Populate from:
- Every existing XAML page — extract confirmed working DevExpress patterns
- DevExpress GitMCP — fetch patterns for components not yet used but planned:
  DXCollectionView, DataForm, TabView, Popup
- XAML namespace declarations from actual files
- Mark each pattern as either "confirmed in codebase" or "from GitMCP reference"

### 9. `.claude/rules/mediatr-patterns.md` (update existing stub)
Populate from:
- Existing command/query handlers in Services project
- `ddd-dotnet` skill patterns
- Mark patterns as "confirmed in codebase" or "reference pattern"

### 10. `CLAUDE.md` (update)
After generating all files above, review `CLAUDE.md` and update:
- Verify architecture description matches actual project structure found in codebase
- Update Rules Files section to include the new `review` command
- Add any architectural observations discovered during this task
- Keep it under 70 lines — delegate detail to rules/commands files

---

## Execution Order
1. Enable devexpress-maui GitMCP via `/mcp`
2. Read all codebase files listed above
3. Read all skill SKILL.md files listed above
4. Fetch DevExpress patterns via GitMCP
5. Generate files in order: commands first, then rules, then CLAUDE.md
6. Run `/project:build` to confirm nothing broke
7. Run `/project:review` on your own output
8. Disable devexpress-maui GitMCP via `/mcp`
9. Report:
   - What was created/updated
   - What was populated from codebase vs GitMCP reference
   - What requires Helder's review or decision
   - Any architectural observations worth discussing

## Standing Instruction — Continuous Rules Evolution
This applies not just to this task but to every future task:

After completing any task, always ask:
> "Are there patterns, decisions, errors encountered, or lessons learned during
> this task that should be reflected in CLAUDE.md, any rules file, or any command file?"

This includes:
- **Adding** new rules or command files for areas not yet covered
- **Updating** existing rules with more accurate or detailed patterns
- **Replacing** outdated patterns with confirmed working ones
- **Deleting** rules that proved wrong, too restrictive, or superseded by skills
- **Updating CLAUDE.md** when architecture, stack, or fundamental decisions change

There is no fixed list of rules/commands files — the set grows and evolves as the
project progresses. Any area where Claude Code repeatedly makes the same mistake,
asks the same question, or needs the same guidance is a candidate for a new rule.

Examples of areas that may warrant new files over time:
- Navigation patterns (shell routes, deep links)
- EF Core migration workflow
- DI registration conventions
- Testing patterns (xUnit + FluentAssertions + NSubstitute)
- Performance profiling checklist
- Accessibility requirements
- Any DevExpress component pattern confirmed working in the codebase

The goal: every task makes the next task faster and more accurate.

## MCP & Skills Usage Maturity
Usage of MCPs and skills follows a natural maturity curve:

- **Now (early phase)**: Use MCPs and skills heavily — codebase is thin, rules files
  are being established, patterns are unconfirmed. Always prefer external authoritative
  sources over assumptions.

- **Mid phase**: Rules and commands files growing. Use GitMCP only for components
  not yet encountered. Confirmed patterns in rules files take priority over re-fetching.

- **Late phase**: Rules, commands, and CLAUDE.md are mature. MCPs enabled only for
  genuinely new territory. Skills load on demand but rarely needed for established areas.

After confirming any pattern from an external source (GitMCP, skill), always ask:
> "Should this confirmed pattern be added to a rules file to reduce future
> dependency on this external source?"

**MyVocaList skill (future):** When project-specific patterns accumulate enough that
a single skill aggregating queue domain logic, karaoke-specific conventions, and
app navigation flows becomes more efficient than loading multiple rules files,
propose creating a `.claude/skills/myvocalist/SKILL.md`.

## Critical Notes
- Never invent XAML or C# patterns — only document what exists or what GitMCP confirms
- Never mark a task complete while build has errors
- The review step and incremental rules update check are mandatory, not optional
