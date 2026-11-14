# Deploy Debug Build to Google Drive Command (`/sln-deploy-googledrive-debug`)

Build a **Debug** APK and upload it to Google Drive. Same steps as `/sln-deploy-googledrive`, with the Debug configuration.

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

The rclone remote is `googledrive`, destination folder: `MyVocaList/releases`.

## Steps

### 1. Guard — confirm before building

Print exactly:
```
About to build a Debug APK and upload to Google Drive (MyVocaList/releases). Confirm? (yes/no)
```

Stop if the answer is not `yes`.

### 2. Guard — verify rclone remote exists

```powershell
rclone listremotes | Select-String "googledrive"
```

Stop with a clear message if `googledrive:` is not listed.

### 3. Build the Debug APK

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug
```

This must exit with code 0. Stop on any build error — do not proceed to upload.

The Debug configuration embeds the assemblies into the APK (`EmbedAssembliesIntoApk` in the `.csproj`), so the file installs and runs on its own, without Visual Studio.

### 4. Find the APK

```powershell
$apk = Get-ChildItem -Path "MyVocaList/bin/Debug/net10.0-android" -Filter "*-Signed.apk" | Select-Object -First 1
```

Stop with a clear message if it is not found. Report the full path found.

### 5. Upload to Google Drive

The Debug and Release builds produce the same file name, so upload under a distinct name. Never overwrite the release APK.

```powershell
rclone copyto "$($apk.FullName)" "googledrive:MyVocaList/releases/com.myvocalist-debug.apk" --progress
```

### 6. Report

Print:
```
Debug APK uploaded: googledrive:MyVocaList/releases/com.myvocalist-debug.apk
Build version: <version from the build output or `git describe --tags --always`>
```

## Notes

- `scope: drive.file` on the rclone remote means Drive only shows files rclone itself created — this is intentional (least-privilege).
- The debug key belongs to this machine (`%LOCALAPPDATA%\Xamarin\Mono for Android\debug.keystore`). A debug APK built on another machine has another signature.
