@echo off
setlocal
rem Builds a Debug APK and uploads it to Google Drive as com.myvocalist-debug.apk (googledrive:MyVocaList/releases).
rem Debug and Release builds produce the same file name, so the upload is renamed - the release APK is never overwritten.
rem Paths are resolved from this file's location, so it works from a desktop shortcut regardless of "Start in".
set "PROJ=%~dp0..\.."
set "OUT=%PROJ%\bin\Debug\net10.0-android"
set "DEST=googledrive:MyVocaList/releases/com.myvocalist-debug.apk"

rclone listremotes | findstr /i /b "googledrive:" >nul || (echo rclone remote "googledrive:" not found. & pause & exit /b 1)

dotnet build "%PROJ%\MyVocaList.csproj" -f net10.0-android -c Debug || (echo Build failed. & pause & exit /b 1)

set "APK="
for %%f in ("%OUT%\*-Signed.apk") do set "APK=%%~ff"
if not defined APK (echo No signed APK found under %OUT%. & pause & exit /b 1)
echo Found APK: %APK%

rclone copyto "%APK%" "%DEST%" --progress || (echo Upload failed. & pause & exit /b 1)

echo Debug APK uploaded: %DEST%
