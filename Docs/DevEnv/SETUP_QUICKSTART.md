# MyVocaList — Dev Environment Quickstart
> Windows · .NET MAUI 10 · Claude Code CLI
> **Start here.** For full technical details see `docs/devenv-setup.md`.
> **.NET MAUI 10 requires Visual Studio 2026 Insiders** — download from visualstudio.microsoft.com/insiders/
> VS 2022 17.14 does NOT support .NET 10 targeting.

---

## What Each File Does

| File | Purpose | Audience |
|------|---------|----------|
| `SETUP_QUICKSTART.md` (this file) | Step-by-step checklist. What YOU do vs what Claude Code does. | You / any dev onboarding |
| `docs/devenv-setup.md` | Full technical reference: all configs, schemas, troubleshooting. Loaded automatically by Claude Code during setup. | Claude Code + devs hitting problems |

---

## Overview: Who Does What

| Your tasks | Claude Code tasks |
|------------|-------------------|
| Install system tools (Node.js, uv) | Create `.mcp.json` |
| Get API keys and tokens | Run `maui-devflow update-skill` |
| Run the app once (creates SQLite db) | Edit `MauiProgram.cs` for DevFlow agent |
| Install skill plugins (`/plugin` commands) | Create `.claudeignore` |
| Verify everything works | Create `.claude/rules/` stub files |

---

## Step 1 — developer tasks (do these first, in order)

### 1.1 Install Node.js
Download LTS from **nodejs.org** and install.
```bash
node --version   # verify
npm --version    # verify
```

### 1.2 Install Claude Code
```bash
npm install -g @anthropic-ai/claude-code
claude           # first launch triggers browser OAuth — sign in with your Anthropic account
```

### 1.3 Install Python uv (needed for SQLite MCP)
```powershell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```
Add `C:\Users\<you>\.local\bin` to your PATH, then restart terminal.
```bash
uvx --version    # verify
```
If `uvx` is still not found after restart, note the full path — you'll need it: `C:\Users\<you>\.local\bin\uvx.exe`

### 1.4 Get a Context7 API key
Go to **context7.com/dashboard** → sign in with GitHub → copy your API key.

### 1.5 Get a GitHub Personal Access Token
Go to **github.com/settings/tokens** → Generate new token (classic).
Scopes: `repo`, `read:org`, `read:user`. Copy it — you only see it once.

### 1.6 Run the app once on Android emulator 
**not done yet**
This creates the SQLite database file. The SQLite MCP server cannot start without it.
After the app launches successfully, find the db path:
```bash
dir /s /b *.db 2>nul | findstr MyVocaList
```
Note the full path — you'll give it to Claude Code in the future, when SQLite server will be necessary sometimes

---

## Step 2 — CLAUDE CODE tasks (paste this prompt)

Open terminal at `<clone_folder>\MyVocaList`, type `claude`, then paste:

```
Read the file at <root>\Docs\DevEnv\devenv-setup.md first, then do the following:

1. Create .mcp.json in the project root with:
   - context7 (HTTP, key: YOUR_CONTEXT7_KEY)
   - sequential-thinking (cmd /c stdio wrapper)
   - github (HTTP, token: YOUR_' - keep it disabled by default)
   - devexpress-maui GitMCP (HTTP)
2. Run: dotnet tool install --global Redth.MauiDevFlow.CLI
3. Run: dotnet tool install --global androidsdk.tool
4. Run: maui-devflow update-skill (from project root)		
5. Add DevFlow agent to MauiProgram.cs inside #if DEBUG
6. Create .claudeignore at project root
7. Create .claude/rules/devexpress-patterns.md stub
8. Create .claude/rules/mediatr-patterns.md stub
9. Tell me what requires manual action before we continue.

This is Windows. Use cmd /c wrapper for all npx stdio servers.
SQLite MCP server will be added in the future, don´t concern about it now
```

> Replace `YOUR_CONTEXT7_KEY`, `YOUR_GITHUB_PAT`, and the db path with your actual values before pasting.

---

## Step 3 — YOUR tasks (after Claude Code finishes)

### 3.1 Install maui-skills (selective — DevExpress conflict risk)
The `davidortinau/maui-skills` plugin does not support `/plugin marketplace add`. Clone into solution root, then run **one** of the commands below from solution root in a regular terminal — **not inside Claude Code**.

```bash
# From solution root:
cd C:\Users\HELDER SOUSA\source\repos\MyVocaList

git clone https://github.com/davidortinau/maui-skills.git maui-skills
```
**About maui-current-apis — critical for .NET 10, prevents deprecated API usage** 

**CMD:**
```cmd
for %s in (maui-shell-navigation maui-data-binding maui-dependency-injection maui-performance maui-app-lifecycle maui-safe-area maui-unit-testing maui-rest-api maui-geolocation maui-permissions maui-secure-storage maui-authentication maui-localization maui-platform-invoke maui-accessibility maui-animations maui-app-icons-splash maui-local-notifications maui-hot-reload-diagnostics maui-current-apis) do xcopy "maui-skills\plugins\maui-skills\skills\%s" ".claude\skills\%s\" /E /I
```

**PowerShell:**
```powershell
@("maui-shell-navigation","maui-data-binding","maui-dependency-injection","maui-performance","maui-app-lifecycle","maui-safe-area","maui-unit-testing","maui-rest-api","maui-geolocation","maui-permissions","maui-secure-storage","maui-authentication","maui-localization","maui-platform-invoke","maui-accessibility","maui-animations","maui-app-icons-splash","maui-local-notifications","maui-hot-reload-diagnostics", "maui-current-apis") | ForEach-Object { xcopy "maui-skills\plugins\maui-skills\skills\$_" ".claude\skills\$_\" /E /I }
```

**Bash (Git Bash / WSL):**
```bash
for s in maui-shell-navigation maui-data-binding maui-dependency-injection maui-performance maui-app-lifecycle maui-safe-area maui-unit-testing maui-rest-api maui-geolocation maui-permissions maui-secure-storage maui-authentication maui-localization maui-platform-invoke maui-accessibility maui-animations maui-app-icons-splash maui-local-notifications maui-hot-reload-diagnostics maui-current-apis; do cp -R "maui-skills/plugins/maui-skills/skills/$s" ".claude/skills/"; done
```


Claude Code auto-detects SKILL.md files under `.claude/skills/` — no further install needed.

**Do NOT copy — DevExpress conflict:**
- `maui-collectionview` → use `DXCollectionView`, not stock `CollectionView`
- `maui-gestures` → DevExpress has its own gesture handling
- `maui-sqlite-database` → teaches `sqlite-net-pcl`, conflicts with EF Core

**Do NOT copy — not relevant to MyVocaList:**
`maui-maps`, `maui-speech-to-text`, `maui-push-notifications`, `maui-aspire`, `maui-deep-linking`, `maui-hybridwebview`, `maui-media-picker`, `maui-graphics-drawing`

### 3.2 Install remaining skill plugins
Type `claude` to open a Claude Code session, then run:
```
/plugin marketplace add obra/superpowers
/plugin install superpowers@obra-superpowers

/plugin marketplace add nesbo/dotnet-claude-code-skills
/plugin install ddd-dotnet@nesbo-dotnet-claude-code-skills
/plugin install data-dotnet@nesbo-dotnet-claude-code-skills
/plugin install bdd-dotnet@nesbo-dotnet-claude-code-skills

/plugin marketplace add Aaronontheweb/dotnet-skills
/plugin install dotnet-skills@Aaronontheweb-dotnet-skills

# UX plugins
/plugin marketplace add teslasoft-de/claude-skills-marketplace
/plugin install ux@teslasoft-skills

/plugin marketplace add manutej/luxor-claude-marketplace
/plugin install mobile-design@manutej-luxor-claude-marketplace
```

> **Note on obra/superpowers TDD enforcement:** This skill enforces RED-GREEN-REFACTOR on *new* code only. It will not delete or modify existing code. You can ask Claude to skip TDD enforcement on specific tasks when needed.

### 3.3 Add DevExpress priority rule to CLAUDE.md
Add this to your `CLAUDE.md` at solution root to prevent skills from suggesting stock MAUI controls when a DevExpress equivalent exists:
```
## UI Component Priority 

When building UI components, always check devexpress-patterns.md first.
Use stock MAUI controls only when DevExpress has no equivalent for the
required functionality.
```

---

## Step 4 — Verify everything works

### Terminal (outside Claude Code):
```bash
claude mcp list
# Expected: context7 Connected, sequential-thinking Connected, github Connected
# Note: sqlite MCP is deferred to post-MVP
```

### Inside Claude Code session:
```
/skills list
# Expected: skills from superpowers, dotnet-skills, dotnet-claude-code-skills, maui-ai-debugging
```

### With Android emulator running + app deployed:
```bash
maui-devflow list
# Expected: your app agent listed
```

### Smoke test (inside Claude Code):
```
"Use context7 to show me DevExpress MAUI DataForm binding documentation."
"Use sequential thinking to plan the QueueEntry aggregate."
"Query the SQLite database and show me all tables."
```

---

## Step 5 — After setup is complete
Add these to `.claudeignore` at **solution root** to stop Claude Code scanning them on every session:
```
.claude/rules/devenv-setup.md
docs/devenv-setup.md
docs/DevEnv_Setup_Guide.docx
```

> **Folder structure note:** `.claude/`, `.mcp.json`, `.claudeignore`, and `SETUP_QUICKSTART.md` all live at **solution root** — not inside the MAUI app project folder. Claude Code needs visibility across all projects in the solution.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| MCP server disconnected | stdio: check `cmd /c` wrapper. HTTP: check API key/PAT |
| `uvx` not found | Use full path in `.mcp.json`: `C:/Users/<you>/.local/bin/uvx.exe` |
| SQLite db not found | Run app once to create db file, then restart MCP server |
| GitHub MCP auth error | Regenerate PAT with `repo`, `read:org`, `read:user` scopes |
| `/plugin` not recognized | Must be inside `claude` session, not regular terminal |
| `maui-devflow list` empty | Check DEBUG config, `AddMauiDevFlowAgent()` registered, run `adb devices` |
| `davidortinau/maui-skills` marketplace error | Use manual clone method in Step 3.1 above |
| Claude uses CollectionView instead of DXCollectionView | Check CLAUDE.md has UI Component Priority rule; ensure maui-collectionview was not copied |
| Context window ~70K at startup | Disable GitHub MCP via `/mcp` inside Claude Code |
| Pages going edge-to-edge after .NET 10 upgrade | Add SafeAreaEdges="Container" to affected ContentPages |
