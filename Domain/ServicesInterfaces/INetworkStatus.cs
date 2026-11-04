namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Reports whether the device currently has an internet route (REQ-SFT-53). No platform types here;
/// the MAUI adapter lives in the app project.
/// </summary>
public interface INetworkStatus
{
    /// <summary>
    /// False only when the platform reports no internet route (NetworkAccess None or Local).
    /// Unknown and ConstrainedInternet return true: the remote call is attempted, and a failure
    /// yields RemoteSearchStatus.Unavailable (REQ-SFT-53). Read live on every call; never cached.
    /// </summary>
    bool HasInternetAccess { get; }
}
