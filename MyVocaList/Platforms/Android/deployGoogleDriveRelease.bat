@echo off
setlocal
rem Builds a signed Release APK and uploads it to Google Drive (googledrive:MyVocaList/releases).
rem Needs the user environment variables ANDROID_KEYSTORE_PASS and ANDROID_KEY_PASS (keystore: C:\Users\helde\keystrokes\myvocalist-release.jks, alias myvocalist).
rem Paths are resolved from this file's location, so it works from a desktop shortcut regardless of "Start in".
set "PROJ=%~dp0..\.."
set "OUT=%PROJ%\bin\Release\net10.0-android"
set "DEST=googledrive:MyVocaList/releases"

if not defined ANDROID_KEYSTORE_PASS (echo ANDROID_KEYSTORE_PASS is not set. & pause & exit /b 1)
if not defined ANDROID_KEY_PASS (echo ANDROID_KEY_PASS is not set. & pause & exit /b 1)

rclone listremotes | findstr /i /b "googledrive:" >nul || (echo rclone remote "googledrive:" not found. & pause & exit /b 1)

dotnet publish "%PROJ%\MyVocaList.csproj" -f net10.0-android -c Release || (echo Publish failed. & pause & exit /b 1)

set "APK="
for %%f in ("%OUT%\publish\*-Signed.apk") do set "APK=%%~ff"
if not defined APK for %%f in ("%OUT%\*-Signed.apk") do set "APK=%%~ff"
if not defined APK (echo No signed APK found under %OUT%. & pause & exit /b 1)
echo Found APK: %APK%

rclone copy "%APK%" "%DEST%" --progress || (echo Upload failed. & pause & exit /b 1)

for %%f in ("%APK%") do echo APK uploaded: %DEST%/%%~nxf
