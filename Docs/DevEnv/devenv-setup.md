# MyVocaList Dev Environment Setup
> Windows · .NET MAUI 10 · Claude Code CLI
> Delete from .claude/rules/ and add to .claudeignore after setup is complete.

---

## CRITICAL: Windows Rules
- Every stdio MCP server using `npx` MUST use `cmd /c` wrapper — no exceptions
- Config file: `.mcp.json` in project root (NOT `.claude.json`, NOT `.claude/settings.local.json`)
- Path format in JSON: forward slashes or escaped backslashes

Wrong:
```json
"command": "npx", "args": ["-y", "package"]
```
Correct:
```json
"command": "cmd", "args": ["/c", "npx", "-y", "package"]
```

---

## Complete .mcp.json

Place at `D:\Projects\MyVocaList\.mcp.json`:

```json
{
  "mcpServers": {
    "context7": {
      "type": "http",
      "url": "https://mcp.context7.com/mcp",
      "headers": {
        "CONTEXT7_API_KEY": "YOUR_KEY_HERE"
      }
    },
    "sequential-thinking": {
      "command": "cmd",
      "args": ["/c", "npx", "-y", "@modelcontextprotocol/server-sequential-thinking"]
    },
    "github": {
      "type": "http",
      "url": "https://api.githubcopilot.com/mcp/",
      "headers": {
        "Authorization": "Bearer REDACTED_GITHUB_TOKEN"
      }
    },
    "devexpress-maui": {
      "type": "http",
      "url": "https://gitmcp.io/DevExpress/maui-demo-app"
    }
  }
}
```
**In the future:** SQLite MCP will be added. Json:
```json
 "sqlite": {
      "command": "cmd",
      "args": ["/c", "uvx", "mcp-server-sqlite",
               "--db-path",
               "C:/Users/Helder/AppData/Local/MyVocaList/myvocalist.db"]
    }
```
Notes:
- (future only - discard by now) Find SQLite db path: `dir /s /b *.db 2>nul | findstr MyVocaList`
- If `uvx` not in PATH use full path: `C:/Users/Helder/.local/bin/uvx.exe`
- (future only - discard by now) SQLite db file must exist before server starts — run app once first
- If `claude mcp add-json` returns "Invalid input" use `--transport http` flag instead
- `devexpress-maui` — keep DISABLED during active coding (`/mcp` inside Claude Code)
- `github` — keep DISABLED during active coding (`/mcp` inside Claude Code)

---

## MauiDevFlow CLI Setup

```bash
dotnet tool install --global Redth.MauiDevFlow.CLI
dotnet tool install --global androidsdk.tool

# From project root:
cd D:\Projects\MyVocaList
maui-devflow update-skill
```

This creates `.claude/skills/maui-ai-debugging/` — Claude Code detects it automatically.

**NuGet package** (DEBUG only — check github.com/Redth?tab=packages first, may not be on public nuget.org):
```xml
<PackageReference Include="Redth.MauiDevFlow.Agent" Version="*" />
```

**MauiProgram.cs wiring:**
```csharp
#if DEBUG
using MauiDevFlow.Agent;
#endif

// inside CreateMauiApp():
#if DEBUG
builder.AddMauiDevFlowAgent();
#endif
```

**Windows warning:** Agent works partially on Windows + Android emulator. CDP/Blazor incomplete. Test before depending on it.

---

## Skill Plugins
All commands run INSIDE Claude Code session (type `claude` first — these are NOT shell commands):

```
/plugin marketplace add obra/superpowers
/plugin install superpowers@obra-superpowers
# NOTE: TDD enforcement applies to NEW code only — will not delete existing code.
# Skip enforcement on specific tasks: "add this without TDD enforcement"

/plugin marketplace add nesbo/dotnet-claude-code-skills
/plugin install ddd-dotnet@nesbo-dotnet-claude-code-skills
/plugin install data-dotnet@nesbo-dotnet-claude-code-skills
/plugin install bdd-dotnet@nesbo-dotnet-claude-code-skills

/plugin marketplace add Aaronontheweb/dotnet-skills
/plugin install dotnet-skills@Aaronontheweb-dotnet-skills

# davidortinau/maui-skills does NOT support /plugin marketplace add
# Clone into solution root, then run ONE of the commands below from solution root
# (CMD/PowerShell/Bash) — NOT inside Claude Code

git clone https://github.com/davidortinau/maui-skills.git maui-skills

# ***About maui-current-apis — critical for .NET 10, prevents deprecated API usage** 
# CMD:
for %s in (maui-shell-navigation maui-data-binding maui-dependency-injection maui-performance maui-app-lifecycle maui-safe-area maui-unit-testing maui-rest-api maui-geolocation maui-permissions maui-secure-storage maui-authentication maui-localization maui-platform-invoke maui-accessibility maui-animations maui-app-icons-splash maui-local-notifications maui-hot-reload-diagnostics maui-current-apis) do xcopy "maui-skills\plugins\maui-skills\skills\%s" ".claude\skills\%s\" /E /I

# PowerShell:
@("maui-shell-navigation","maui-data-binding","maui-dependency-injection","maui-performance","maui-app-lifecycle","maui-safe-area","maui-unit-testing","maui-rest-api","maui-geolocation","maui-permissions","maui-secure-storage","maui-authentication","maui-localization","maui-platform-invoke","maui-accessibility","maui-animations","maui-app-icons-splash","maui-local-notifications","maui-hot-reload-diagnostics", "maui-current-apis") | ForEach-Object { xcopy "maui-skills\plugins\maui-skills\skills\$_" ".claude\skills\$_\" /E /I }

# Bash (Git Bash / WSL):
for s in maui-shell-navigation maui-data-binding maui-dependency-injection maui-performance maui-app-lifecycle maui-safe-area maui-unit-testing maui-rest-api maui-geolocation maui-permissions maui-secure-storage maui-authentication maui-localization maui-platform-invoke maui-accessibility maui-animations maui-app-icons-splash maui-local-notifications maui-hot-reload-diagnostics maui-current-apis; do cp -R "maui-skills/plugins/maui-skills/skills/$s" ".claude/skills/"; done

# Claude Code auto-detects SKILL.md files under .claude/skills/ — no further install needed

# DO NOT copy — DevExpress conflict:
# maui-collectionview  → use DXCollectionView, not stock CollectionView
# maui-gestures        → DevExpress has its own gesture handling
# maui-sqlite-database → teaches sqlite-net-pcl, conflicts with EF Core

# DO NOT copy — not relevant to MyVocaList:
# maui-maps, maui-speech-to-text, maui-push-notifications, maui-aspire
# maui-deep-linking, maui-hybridwebview, maui-media-picker, maui-graphics-drawing

# Verify:
/skills list
```

nesbo plugin uses Paramore.Brighter (not MediatR) — DDD patterns transfer, bridge with `mediatr-patterns.md`.

---

## Verification

```bash
# Terminal (outside Claude Code):
claude mcp list
# Expected: context7 Connected, sequential-thinking Connected, sqlite Connected, github Connected

# Inside Claude Code:
/skills list
# Expected: skills from superpowers, dotnet-skills, dotnet-claude-code-skills, maui-skills, maui-ai-debugging

# With Android emulator running + app deployed:
maui-devflow list
```

Smoke test (inside Claude Code):
- `"Use context7 to show me DevExpress MAUI DataForm binding documentation."`
- `"Use sequential thinking to plan the QueueEntry aggregate."`
- `"Query the SQLite database and show me all tables."`

---

## Context Window Budget

| Server | Tokens |
|--------|--------|
| Context7 (HTTP) | ~2,000 |
| Sequential Thinking | ~500 |
| SQLite MCP | ~1,500 |
| GitHub MCP | ~8,000 (80+ tools) |
| **Total (all 4)** | **~12,000 — acceptable** |

Skill plugins = 0 base tokens (loaded on demand).

**GitHub MCP lifecycle:**
- MVP coding phase → DISABLED (save 8,000 tokens)
- Post-MVP issue/PR work → enable, disable again when coding resumes
- Toggle: `/mcp` inside Claude Code → select github → Enable/Disable

**DevExpress GitMCP:**
- Building new DevExpress page → Enable, get pattern, disable
- Iterating existing code → Disabled (`devexpress-patterns.md` serves patterns)
- `devexpress-patterns.md` takes priority over GitMCP when both active

---

## Rules Files to Create

### `.claude/rules/devexpress-patterns.md`
One minimal working example per DevExpress component. Add as you build pages.

```xml
<!-- DataForm — ViewModel binding -->
<dxdf:DataForm DataObject="{Binding FormModel}" />
<!-- ViewModel exposes single [ObservableProperty] FormModel of POCO type -->

<!-- CollectionView — basic list -->
<dxcv:DXCollectionView ItemsSource="{Binding Items}">
  <dxcv:DXCollectionView.ItemTemplate>
    <DataTemplate>
      <dxcv:ContentItemBase>...</dxcv:ContentItemBase>
    </DataTemplate>
  </dxcv:DXCollectionView.ItemTemplate>
</dxcv:DXCollectionView>
```

### `.claude/rules/mediatr-patterns.md`
```csharp
// Command
public record AddSingerCommand(string Name) : IRequest<Result<Singer>>;
public class AddSingerCommandHandler : IRequestHandler<AddSingerCommand, Result<Singer>>
{
    public async Task<Result<Singer>> Handle(AddSingerCommand req, CancellationToken ct) { ... }
}

// Query
public record GetQueueQuery : IRequest<Result<IReadOnlyList<QueueEntry>>>;

// Pipeline behavior (validation)
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> { ... }
```

### `.claudeignore` (project root)
```
devexpress-samples/
MauiDevFlow-samples/
obj/
bin/
*.designer.cs
.vs/
```

---

## CLAUDE.md — Required Rule Addition

Add this to your `CLAUDE.md` at solution root to prevent skills from overriding DevExpress with stock MAUI controls:

```
## UI Component Priority

When building UI components, always check devexpress-patterns.md first.
Use stock MAUI controls only when DevExpress has no equivalent for the
required functionality.
```

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| MCP server disconnected | stdio: check `cmd /c` wrapper. HTTP: check API key/PAT |
| `uvx` not found | Full path: `C:/Users/Helder/.local/bin/uvx.exe` |
| sqlite db not found | Run app once to create db file, then restart MCP server |
| GitHub MCP auth error | Regenerate PAT: scopes `repo`, `read:org`, `read:user` |
| `/plugin` not recognized | Must be inside `claude` session, not regular terminal |
| `maui-devflow list` empty | Debug config, `AddMauiDevFlowAgent()` registered, check `adb devices` |
| Context window ~70K at startup | Disable GitHub MCP via `/mcp` |

---

## Quick Reference

| Command | Where |
|---------|-------|
| `claude mcp list` | Terminal |
| `claude mcp add --transport http name url -H "Header: val"` | Terminal |
| `claude mcp remove name` | Terminal |
| `/plugin marketplace add owner/repo` | Claude Code session |
| `/plugin install name@marketplace` | Claude Code session |
| `/mcp` | Claude Code session — toggle servers |
| `/skills list` | Claude Code session |
| `maui-devflow list` | Terminal |
| `maui-devflow MAUI tree` | Terminal |
| `maui-devflow MAUI screenshot` | Terminal |
| `maui-devflow update-skill` | Terminal, project root |
