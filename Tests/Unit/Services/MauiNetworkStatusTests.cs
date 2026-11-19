using Microsoft.Maui.Networking;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 4.1c — <see cref="MauiNetworkStatus.IsInternetReachable"/> maps the platform
/// <see cref="NetworkAccess"/> value to the "device has internet" flag (REQ-SFT-53, design § 3.6).
/// </summary>
public class MauiNetworkStatusTests
{
    // [AC] REQ-SFT-53: no internet (None or Local) means Deezer is not called; Unknown, ConstrainedInternet and Internet attempt the call
    [Theory]
    [InlineData(NetworkAccess.None, false)]
    [InlineData(NetworkAccess.Local, false)]
    [InlineData(NetworkAccess.Unknown, true)]
    [InlineData(NetworkAccess.ConstrainedInternet, true)]
    [InlineData(NetworkAccess.Internet, true)]
    public void IsInternetReachable_MapsEachNetworkAccessValue(NetworkAccess access, bool expected)
    {
        var actual = MauiNetworkStatus.IsInternetReachable(access);

        Assert.Equal(expected, actual);
    }
}
