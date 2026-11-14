namespace MyVocaList.UI.Services;

/// <summary>The place the user last confirmed on the map picker on this device (REQ-VEN-24, case 3).</summary>
public interface IMapPickerPreferences
{
    /// <summary>Latitude and longitude of the last confirmed place; null when none was stored.</summary>
    (double latitude, double longitude)? LastPlace { get; set; }
}
