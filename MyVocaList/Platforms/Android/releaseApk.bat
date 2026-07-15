dotnet publish ..\..\MyVocaList.csproj -f net10.0-android -c Release && adb install -r  ..\..\bin\Release\net10.0-android\publish\com.myvocalist-Signed.apk
