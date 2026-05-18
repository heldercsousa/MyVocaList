namespace MyVocaList.Domain.ServicesInterfaces;

public enum OverlayStage { Stage1, Stage2 }

public interface IOverlayService
{
    bool IsPermissionGranted { get; }

    /// <summary>Opens Android Settings → "Display over other apps". No-op on iOS.</summary>
    void RequestPermission();

    /// <summary>Shows or updates the floating label. No-op on iOS.</summary>
    void Show(string singerName, string songTitle, OverlayStage stage);

    void UpdateStage(OverlayStage stage);

    /// <summary>Hides and destroys the overlay view.</summary>
    void Dismiss();
}
