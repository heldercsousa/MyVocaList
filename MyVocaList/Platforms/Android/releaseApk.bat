@echo off
rem Publishes a signed Release APK, opens its folder in Explorer, then installs it on the connected device.
rem Paths are resolved from this file's location, so it works from a desktop shortcut regardless of "Start in".
set "PROJ=%~dp0..\.."
set "OUT=%PROJ%\bin\Release\net10.0-android\publish"
dotnet publish "%PROJ%\MyVocaList.csproj" -f net10.0-android -c Release || (echo Publish failed. & pause & exit /b 1)
start "" explorer "%OUT%"
adb install -r "%OUT%\com.myvocalist-Signed.apk" || (echo Install failed - is the device connected with USB debugging on? & pause & exit /b 1)
