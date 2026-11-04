using Microsoft.Maui.Networking;

namespace MyVocaList.UI.Services;

/// <summary>
/// MAUI adapter for <see cref="INetworkStatus"/> (REQ-SFT-53). Reads <see cref="IConnectivity"/> live
/// on every call; the only MAUI-aware piece of the offline check.
/// </summary>
public sealed class MauiNetworkStatus(IConnectivity connectivity) : INetworkStatus
{
    public bool HasInternetAccess => IsInternetReachable(connectivity.NetworkAccess);

    /// <summary>
    /// Pure mapping (REQ-SFT-53 / Q19): None and Local mean no internet route; the other values attempt the call.
    /// </summary>
    public static bool IsInternetReachable(NetworkAccess access) =>
        access is not (NetworkAccess.None or NetworkAccess.Local);
}
