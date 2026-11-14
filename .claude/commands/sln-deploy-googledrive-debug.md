# Deploy Debug Build to Google Drive Command (`/sln-deploy-googledrive-debug`)

Build a **Debug** APK and upload it to Google Drive. All logic lives in `MyVocaList/Platforms/Android/deployGoogleDriveDebug.bat`, so Helder can also run it himself, outside Claude Code (double-click or from any terminal).

**Manual only:** run this command only when Helder asks for it. It is not a step of any workflow, close-out or other command.

## How it differs from `/sln-deploy-googledrive`

| | Debug (this command) | Release (`/sln-deploy-googledrive`) |
|---|---|---|
| Signing | Debug key of this machine; no password needed | Release keystore; needs `ANDROID_KEYSTORE_PASS` and `ANDROID_KEY_PASS` |
| Speed on the device | Slower (no optimisation) — do not use it to judge performance | Representative of real speed |
| Debug-only data | Present (e.g. the 500-venue seed, `#if DEBUG`) | Absent |
| File on Drive | `com.myvocalist-debug.apk` | `com.myvocalist-Signed.apk` |
| Size | About 110 MB | About 63 MB |

**Signatures do not mix:** Android refuses to update a release install with a debug APK, and the reverse (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`). Uninstall the app once when changing between the two; this deletes the app's data on the device. A debug APK updates a build deployed from Visual Studio on this machine, because both use the same debug key.

The Debug configuration embeds the assemblies into the APK (`EmbedAssembliesIntoApk` in the `.csproj`), so the file installs and runs on its own, without Visual Studio.

## Steps

1. **Guard — confirm.** Print exactly, and stop if the answer is not `yes`:
   ```
   About to build a Debug APK and upload to Google Drive (MyVocaList/releases). Confirm? (yes/no)
   ```
2. **Run** (the script no longer prompts):
   ```powershell
   cmd /c "MyVocaList\Platforms\Android\deployGoogleDriveDebug.bat"
   ```
   The script checks the rclone remote, builds Debug, finds `*-Signed.apk` and uploads it as `com.myvocalist-debug.apk` (never overwriting the release APK). It exits non-zero on any failure; report the failing step and stop.
3. **Report** the script's `Debug APK uploaded: ...` line plus the version (`git describe --tags --always`).

## Notes

- rclone remote `googledrive`, folder `MyVocaList/releases`. `scope: drive.file` means Drive only shows files rclone itself created — intentional (least-privilege).
- The debug key belongs to this machine (`%LOCALAPPDATA%\Xamarin\Mono for Android\debug.keystore`). A debug APK built on another machine has another signature.
- On failure the script calls `pause` so a double-clicked window stays open. Under Claude Code that would hang: if the run times out, treat it as a failure.
