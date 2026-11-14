using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Architecture;

/// <summary>
/// REQ-VEN-35 — the venue map picker SHALL NOT request the device location. Taps on the map are the only input,
/// so neither platform manifest asks for a location permission and no app source file calls the Geolocation API.
/// </summary>
public class NoLocationPermissionTests
{
    private static readonly string[] AndroidLocationPermissions =
    [
        "ACCESS_FINE_LOCATION",
        "ACCESS_COARSE_LOCATION",
        "ACCESS_BACKGROUND_LOCATION"
    ];

    private static readonly string[] LocationApiMarkers = ["Geolocation", "LocationWhenInUse", "LocationAlways"];

    // [AC] REQ-VEN-35: the Android manifest requests no location permission.
    [Fact]
    public void AndroidManifest_RequestsNoLocationPermission()
    {
        var manifest = File.ReadAllText(TestSourcePaths.LocateSource("MyVocaList/Platforms/Android/AndroidManifest.xml"));

        foreach (var permission in AndroidLocationPermissions)
            Assert.DoesNotContain(permission, manifest);
    }

    // [AC] REQ-VEN-35: the iOS Info.plist declares no location usage description.
    [Fact]
    public void iOSInfoPlist_DeclaresNoLocationUsage()
    {
        var plist = File.ReadAllText(TestSourcePaths.LocateSource("MyVocaList/Platforms/iOS/Info.plist"));

        Assert.DoesNotContain("NSLocation", plist);
    }

    // [AC] REQ-VEN-35: no source file under MyVocaList/ uses the Geolocation API.
    [Fact]
    public void NoSourceFile_UsesGeolocationApi()
    {
        var appDir = TestSourcePaths.LocateDirectory("MyVocaList");
        var separator = Path.DirectorySeparatorChar;

        var offenders = Directory.GetFiles(appDir, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}"))
            .Where(p =>
            {
                var text = File.ReadAllText(p);
                return LocationApiMarkers.Any(text.Contains);
            })
            .Select(p => Path.GetRelativePath(appDir, p))
            .ToList();

        Assert.Empty(offenders);
    }
}
