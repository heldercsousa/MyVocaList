# Team Environment Setup (moved from CLAUDE.md 2026-07-14 — token-scoped context)

> One-time developer-onboarding procedure. Moved out of the always-loaded CLAUDE.md because it is read once per developer ever, yet was paid on every agent dispatch. CLAUDE.md keeps a one-line pointer.

Environment variables for MCP integrations (Playwright token, Context7 API key) are stored in `.env.local` (gitignored, team-shared).

**First-time setup (each developer, once):**
1. Copy `.env.local.example` to `.env.local`
2. Fill in your API keys (Playwright MCP token, Context7 API key)
3. Run `. .\.claude\scripts\load-env.ps1` **once** to persist vars to User scope (no admin needed — User scope writes go to HKCU)
4. **Close ALL terminal windows completely** (not just the tab — the terminal host process itself, and VS Code / Visual Studio if Claude Code is launched from there), then open a fresh terminal from the Start menu / taskbar
5. Verify with `$env:CONTEXT7_API_KEY` (should print the key), then start Claude Code

> **Why step 4 matters:** Windows processes get an environment *snapshot* from their parent at launch. The script only updates the registry — already-running processes (including an open Windows Terminal host that spawns "new" tabs/windows) keep the stale snapshot, so Claude Code launched from them won't see the keys and `/mcp` will warn the key is missing. Restarting Claude Code inside the same stale terminal does NOT help.

After step 4, env vars are **automatically available** to Claude Code on every restart — no further action needed.

**If keys change:** update `.env.local`, then re-run the script and repeat step 4. Env vars are only read from `.env.local` during script execution; afterward they live in the registry.

> **Design note:** `.env.local` is the source of truth for key rotation across the team. Each dev maintains their own `.env.local` (never committed). The script is a one-time bridge to the registry.

## Android Release Signing (`/sln-deploy-android`)

Required to build a signed APK and upload it to Google Drive.

**One-time per machine:**
1. Create a keystore (once per developer — keep it outside the repo):
   ```powershell
   keytool -genkey -v -keystore C:\Users\<you>\keystrokes\myvocalist-release.jks -alias myvocalist -keyalg RSA -keysize 2048 -validity 10000
   ```
2. Configure rclone with Google Drive (remote name must be `googledrive`):
   ```powershell
   rclone config   # new remote → drive → blank client_id/secret → use auto config
   ```
3. Add your passwords to `.env.local` (copy from `.env.local.example`) and run `load-env.ps1`:
   ```
   ANDROID_KEYSTORE_PASS=<keystore password>
   ANDROID_KEY_PASS=<key password>   # same as keystore password if you pressed Enter
   ```
   Then: `. .\.claude\scripts\load-env.ps1` and restart terminals.

**Variables registered by this setup:**

| Variable | Purpose |
|---|---|
| `ANDROID_KEYSTORE_PASS` | Keystore file password (read by MSBuild at publish time) |
| `ANDROID_KEY_PASS` | Key entry password (same value if not set separately) |

The keystore path and alias are hardcoded in `MyVocaList/MyVocaList.csproj` (Release Android PropertyGroup). If the keystore moves, update both the `.csproj` and this file.
