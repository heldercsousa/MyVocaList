using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.UI.Services;

/// <summary>iOS/Windows implementation — overlay is Android-only. All methods are no-ops.</summary>
public class NoOpOverlayService : IOverlayService
{
    public bool IsPermissionGranted => false;

    /// <inheritdoc />
    public void RequestPermission() { }

    /// <inheritdoc />
    public void Show(string singerName, string songTitle, OverlayStage stage) { }

    /// <inheritdoc />
    public void UpdateStage(OverlayStage stage) { }

    /// <inheritdoc />
    public void Dismiss() { }
}
