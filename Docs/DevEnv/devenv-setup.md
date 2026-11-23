# MyVocaList: Dev Environment Setup From Scratch

> **The one guide for a new Windows development machine.** It replaces the early-days
> `SETUP_QUICKSTART.md` and the old version of this file, both deleted on 2026-11-21. Those guides
> bootstrapped files (`.mcp.json`, `.claude/rules/`, `CLAUDE.md` sections) that now exist in the
> repo.
>
> For three topics this guide sends you to the file that is their single source. Do not copy
> their content here:
> - env keys (Context7, Playwright) and Android release signing → [`.claude/library/dev-env-setup.md`](../../.claude/library/dev-env-setup.md)
> - database, migrations and the MCP query DB → [`.claude/library/database-setup.md`](../../.claude/library/database-setup.md)
> - adding or changing an MCP server → [`.claude/library/mcp-governance.md`](../../.claude/library/mcp-governance.md)
>
> **Last verified:** 2026-11-21 on the author's machine. The versions shown are what that machine
> runs, not hard minimums. Package versions come only from `Directory.Packages.props`.

Platform: **Windows** (the dev machine). Targets: **Android** and **iOS**. iOS builds need a Mac
build host and are not covered here.

---

## 1. Install the toolchain

| Tool | Why the project needs it | Verify |
|------|--------------------------|--------|
| **Git for Windows** | Source control. The git hooks are `sh` scripts and run in Git's shell. | `git --version` |
| **Visual Studio 2026** (18.x) with the **.NET Multi-platform App UI development** workload | Installs the .NET SDK, the MAUI workloads, the Android SDK and a JDK. The author's machine has SDK `11.0.100`. The projects target `net10.0*` and there is no `global.json`, so any SDK from .NET 10 up works. | `dotnet --version` · `dotnet workload list` shows `android`, `ios`, `maui-windows` |
| **Android emulator** | Running and verifying the app. The SDK lands in `C:\Program Files (x86)\Android\android-sdk`. Create an AVD in Visual Studio's Android Device Manager (the author uses Pixel 5, API 36). | `adb devices` |
| **Python 3** (standard library only) | Claude Code hooks, git hooks and the backlog generator all run `python`. | `python --version` |
| **Node.js LTS** | `npx` runs the Playwright MCP server. | `node --version` |
| **uv** (`uvx`) | Runs the SQLite MCP server. Install: `powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 \| iex"`, add `%USERPROFILE%\.local\bin` to PATH, then restart the terminal. | `uvx --version` |
| **GitHub CLI** (`gh`) | Every GitHub operation (the GitHub MCP is disabled on purpose). Run `gh auth login` once. | `gh --version` |
| **Claude Code** | The AI implementation agent. Install it with the official instructions at <https://docs.claude.com/en/docs/claude-code/setup>. | `claude --version` |
| **rclone** *(optional)* | Only for `/sln-deploy-googledrive`. Setup: `.claude/library/dev-env-setup.md`. | `rclone version` |

**.NET global tools:**

```powershell
dotnet tool install --global dotnet-ef               # EF Core migrations (see database-setup.md)
dotnet tool install --global Redth.MauiDevFlow.CLI   # maui-devflow: inspect and drive the running Debug app
dotnet tool install --global androidsdk.tool         # android: SDK / AVD command line
```

The DevFlow agent package (`Redth.MauiDevFlow.Agent`) is already referenced by
`MyVocaList/MyVocaList.csproj`, so nothing needs wiring in code.

---

## 2. DevExpress (required to build)

The UI uses **DevExpress .NET MAUI 25.2**, which needs a license. DevExpress's free MAUI offer
ended on 2024-12-31. The choice of licensed library is open: see the backlog item *UI component
library licensing* (`Docs/Management/cross-cutting/ui-component-library-licensing/`).

1. Add your personal DevExpress NuGet feed. Copy the URL from your account at
   <https://nuget.devexpress.com>, then run:
   ```powershell
   dotnet nuget add source "<your DevExpress feed URL>" -n DevExpress
   ```
2. Put your license key in `%APPDATA%\DevExpress\DevExpress_License.txt`. Create this file **by
   hand** and never commit it. Without it the build still succeeds, but it reports `DX1000` /
   `DX1001` ("For evaluation purposes only") warnings.

### 2a. Syncfusion license key (Release builds only)

The UI is migrating to **Syncfusion .NET MAUI**. Its license key is read at build time from the
user environment variable `SYNCFUSION_LICENSE_KEY` and compiled into a generated file under
`obj/`. The key is never committed.

1. Get the key from your Syncfusion Community License account (the account owner does this).
2. Set it as a user environment variable on Windows, then restart Visual Studio or the shell so
   the new value is picked up:
   ```powershell
   setx SYNCFUSION_LICENSE_KEY "<your Syncfusion license key>"
   ```
3. A **Release** build fails without it, with `SYNCFUSION_LICENSE_KEY is not set — required for
   Release builds.` A **Debug** build works without it, and the app then shows Syncfusion's trial
   notice.

---

## 3. Clone and enable the git hooks

```powershell
git clone https://github.com/heldercsousa/MyVocaList.git
cd MyVocaList
git config core.hooksPath .claude/githooks
```

The last line is **required** because git does not pick up the hooks on its own:
- **pre-commit** runs the tests when `.cs` / `.xaml` files are staged. It also checks that the
  generated BACKLOG is current and runs the process checks (`.sln` registration, `SafeAreaEdges`,
  task-log `Changed files`).
- **commit-msg** requires the `Item:` trailer on `feat:` / `refactor:` / `perf:` / `amend:`
  commits.

---

## 4. Build, test, run

```powershell
dotnet restore
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android          # 0 errors expected
dotnet test Tests/MyVocaList.Tests.csproj                              # unit + real-SQLite integration tests
dotnet build MyVocaList/MyVocaList.csproj -t:Run -f net10.0-android   # deploy to the running emulator
```

The app creates and migrates its own SQLite database on the device at startup. Nothing needs to
run on the desktop for the app to work.

---

## 5. Claude Code for this repo

1. **Env keys.** Do steps 1–5 of `.claude/library/dev-env-setup.md`: copy `.env.local.example`
   to `.env.local`, fill in `CONTEXT7_API_KEY` and `PLAYWRIGHT_MCP_EXTENSION_TOKEN`, run
   `load-env.ps1`, then restart the terminal host.
2. **MCP servers.** These are already declared in the committed [`.mcp.json`](../../.mcp.json):
   Context7, Microsoft Learn, DevExpress MAUI (GitMCP), Playwright, SQLite and MudBlazor.
   - **SQLite:** `--db-path` is an absolute path into the author's clone. Point it at
     `<your clone>/.claude/MyVocaList.db`, then create that database as described in
     `.claude/library/database-setup.md` ("Initial Setup").
   - **Playwright** runs in `--extension` mode. It needs the Playwright MCP Bridge browser
     extension, whose token goes in `PLAYWRIGHT_MCP_EXTENSION_TOKEN`.
   - **MudBlazor** is disabled in `.claude/settings.json` (`disabledMcpjsonServers`). It needs a
     local MudMCP checkout, so ignore it unless you work on that.
   - On Windows every stdio server that uses `npx` / `uvx` must go through `cmd /c`. The
     committed file already does this.
3. **Plugin.** `.claude/settings.json` enables only `superpowers@claude-plugins-official`. All the
   other listed plugins are disabled on purpose. If the plugin is not offered when you first trust
   the folder, run `/plugin install superpowers@claude-plugins-official` inside Claude Code.
4. **Skills** are committed under `.claude/skills/`: `myvocalist-coding`, the vendored .NET skills
   (`VENDORED.md`) and the MAUI skills. Nothing to install.
5. **Verify.** Start `claude` at the repo root:
   - The SessionStart hook prints `HOOK HEALTH OK`.
   - `/mcp` lists the servers as connected (MudBlazor stays disabled).
   - `python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"` prints the active work
     set.

How the AI-assisted workflow runs once the setup works: [`CLAUDE.md`](../../CLAUDE.md) and
[`.claude/rules/`](../../.claude/rules/).

---

## 6. Android release builds *(optional)*

Release signing (keystore, `ANDROID_KEYSTORE_PASS` / `ANDROID_KEY_PASS`) and the Google Drive
upload are covered in `.claude/library/dev-env-setup.md` § Android Release Signing. Deployments run
only when Helder asks for them.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `/mcp` warns a key is missing after `load-env.ps1` | The terminal host kept its old environment. Close **all** terminal windows (and VS / VS Code if Claude Code runs inside them), then open a fresh one. Details: `.claude/library/dev-env-setup.md`. |
| `uvx` not found | Add `%USERPROFILE%\.local\bin` to PATH, or put the full path to `uvx.exe` in `.mcp.json`. |
| SQLite MCP fails to start | The `--db-path` file does not exist. Create it (`database-setup.md`) and check the path points at your clone. |
| Build warnings `DX1000` / `DX1001` | The DevExpress license file is missing (step 2). |
| App crashes at launch with an NRE in `ShellFlyoutTemplatedContentRenderer.LoadView` | Stale Android build output. Delete every `bin/` and `obj/` folder (`limparBinObj.bat` at the repo root does this), then rebuild. |
| A tool or agent says there is no emulator | Look in the Visual Studio SDK path `C:\Program Files (x86)\Android\android-sdk\emulator\`, not `C:\Android`. |
| `maui-devflow list` is empty | Use a Debug build on a running emulator, and check `adb devices`. |
| Commits go through without tests running | `core.hooksPath` is not set (step 3). |
