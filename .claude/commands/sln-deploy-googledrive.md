# Deploy to Google Drive Command (`/sln-deploy-googledrive`)

Build a signed release APK and upload it to Google Drive. All logic lives in `MyVocaList/Platforms/Android/deployGoogleDriveRelease.bat`, so Helder can also run it himself, outside Claude Code (double-click or from any terminal). Debug variant: `/sln-deploy-googledrive-debug`.

**Manual only:** run this command only when Helder asks for it. It is not a step of any workflow, close-out or other command.

## Steps

1. **Guard — confirm.** Print exactly, and stop if the answer is not `yes`:
   ```
   About to build a signed release APK and upload to Google Drive (MyVocaList/releases). Confirm? (yes/no)
   ```
2. **Run** (the script no longer prompts):
   ```powershell
   cmd /c "MyVocaList\Platforms\Android\deployGoogleDriveRelease.bat"
   ```
   The script checks the env vars and the rclone remote, publishes, finds `*-Signed.apk` and uploads it as `com.myvocalist-release-<yyyyMMdd-HHmmss>-<commit>.apk`. The date-time (local, taken at upload) makes every upload unique, so no earlier APK is overwritten; the short commit hash tells which code the build came from. Old files accumulate on Drive — delete them there by hand when no longer needed. It exits non-zero on any failure; report the failing step and stop.
3. **Report** the line the script prints: `APK uploaded: googledrive:MyVocaList/releases/com.myvocalist-release-<yyyyMMdd-HHmmss>-<commit>.apk`.

## Notes

- Prerequisites (Windows user environment variables): `ANDROID_KEYSTORE_PASS`, `ANDROID_KEY_PASS`. Keystore: `C:\Users\helde\keystrokes\myvocalist-release.jks` (alias `myvocalist`), hardcoded for this machine (also in the `.csproj` Release PropertyGroup).
- rclone remote `googledrive`, folder `MyVocaList/releases`. `scope: drive.file` means Drive only shows files rclone itself created — intentional (least-privilege).
- Run `/sln-release` to tag the version **before** this command if the build should carry a release version number.
- On failure the script calls `pause` so a double-clicked window stays open. Under Claude Code that would hang: if the run times out, treat it as a failure.
