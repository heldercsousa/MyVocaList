# Deploy to Google Drive Command (`/sln-deploy-googledrive`)

Build a signed release APK and upload it to Google Drive.

## Prerequisites (one-time setup)

Two environment variables must be set in Windows (System → Advanced → Environment Variables):

| Variable | Value |
|---|---|
| `ANDROID_KEYSTORE_PASS` | Keystore password (set when keytool ran) |
| `ANDROID_KEY_PASS` | Key password (same as keystore password if you pressed Enter) |

The keystore is at `C:\Users\helde\keystrokes\myvocalist-release.jks` (alias: `myvocalist`).
The rclone remote is `googledrive`, destination folder: `MyVocaList/releases`.

## Steps

### 1. Guard — confirm before building

Print exactly:
```
About to build a signed release APK and upload to Google Drive (MyVocaList/releases). Confirm? (yes/no)
```

Stop if the answer is not `yes`.

### 2. Guard — verify env vars are set

```powershell
if (-not $env:ANDROID_KEYSTORE_PASS) { Write-Error "ANDROID_KEYSTORE_PASS is not set. See /sln-deploy-android prerequisites." }
if (-not $env:ANDROID_KEY_PASS)      { Write-Error "ANDROID_KEY_PASS is not set. See /sln-deploy-android prerequisites." }
```

Stop if either is missing.

### 3. Guard — verify rclone remote exists

```powershell
rclone listremotes | Select-String "googledrive"
```

Stop with a clear message if `googledrive:` is not listed.

### 4. Build release APK

```powershell
dotnet publish MyVocaList/MyVocaList.csproj -f net10.0-android -c Release
```

This must exit with code 0. Stop on any build error — do not proceed to upload.

### 5. Find the APK

Look for `*-Signed.apk` under `MyVocaList/bin/Release/net10.0-android/publish/`.

```powershell
$apk = Get-ChildItem -Path "MyVocaList/bin/Release/net10.0-android/publish" -Filter "*-Signed.apk" | Select-Object -First 1
```

If not found, also check `MyVocaList/bin/Release/net10.0-android/` (unsigned builds land here without `-Signed` suffix). Report the full path found.

### 6. Upload to Google Drive

```powershell
rclone copy "$($apk.FullName)" "googledrive:MyVocaList/releases" --progress
```

Report success with the Drive path: `googledrive:MyVocaList/releases/<filename>`.

### 7. Report

Print:
```
APK uploaded: googledrive:MyVocaList/releases/<filename>
Build version: <dotnet publish output version if available>
```

## Notes

- The keystore path and alias are hardcoded for this machine (`C:\Users\helde\keystrokes\myvocalist-release.jks`, alias `myvocalist`). If the machine changes, update Step 2 of the `.csproj` Release PropertyGroup and this file.
- `scope: drive.file` on the rclone remote means Drive only shows files rclone itself created — this is intentional (least-privilege).
- Run `/sln-release` to tag the version **before** running this command if the build should carry a release version number.
