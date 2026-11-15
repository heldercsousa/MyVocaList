@echo off
setlocal
rem Builds a Debug APK and uploads it to Google Drive (googledrive:MyVocaList/releases) as
rem com.myvocalist-debug-<yyyyMMdd-HHmmss>-<commit>.apk. The date-time makes every upload unique, so an older
rem upload is never overwritten; the commit tells which code the build came from.
rem Paths are resolved from this file's location, so it works from a desktop shortcut regardless of "Start in".
set "PROJ=%~dp0..\.."
set "OUT=%PROJ%\bin\Debug\net10.0-android"

rclone listremotes | findstr /i /b "googledrive:" >nul || (echo rclone remote "googledrive:" not found. & pause & exit /b 1)

dotnet build "%PROJ%\MyVocaList.csproj" -f net10.0-android -c Debug || (echo Build failed. & pause & exit /b 1)

set "APK="
for %%f in ("%OUT%\*-Signed.apk") do set "APK=%%~ff"
if not defined APK (echo No signed APK found under %OUT%. & pause & exit /b 1)
echo Found APK: %APK%

set "STAMP="
for /f %%t in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "STAMP=%%t"
if not defined STAMP (echo Could not read the date and time. & pause & exit /b 1)
set "SHA=nogit"
for /f %%c in ('git -C "%PROJ%" rev-parse --short HEAD 2^>nul') do set "SHA=%%c"
set "DEST=googledrive:MyVocaList/releases/com.myvocalist-debug-%STAMP%-%SHA%.apk"

rclone copyto "%APK%" "%DEST%" --progress || (echo Upload failed. & pause & exit /b 1)

echo Debug APK uploaded: %DEST%
