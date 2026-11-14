using Microsoft.Maui.Storage;

namespace MyVocaList.UI.Services;

/// <summary>
/// <see cref="IMapPickerPreferences"/> over MAUI <see cref="IPreferences"/> (registered as <c>Preferences.Default</c>).
/// Latitude and longitude are stored under two keys; both present means a place, otherwise none.
/// </summary>
public sealed class MauiMapPickerPreferences : IMapPickerPreferences
{
    private const string LatitudeKey = "map_picker_last_lat";
    private const string LongitudeKey = "map_picker_last_lon";

    private readonly IPreferences _preferences;

    public MauiMapPickerPreferences(IPreferences preferences) => _preferences = preferences;

    /// <inheritdoc />
    public (double latitude, double longitude)? LastPlace
    {
        get
        {
            if (!_preferences.ContainsKey(LatitudeKey) || !_preferences.ContainsKey(LongitudeKey))
                return null;

            return (_preferences.Get(LatitudeKey, 0d), _preferences.Get(LongitudeKey, 0d));
        }
        set
        {
            if (value is { } place)
            {
                _preferences.Set(LatitudeKey, place.latitude);
                _preferences.Set(LongitudeKey, place.longitude);
            }
            else
            {
                _preferences.Remove(LatitudeKey);
                _preferences.Remove(LongitudeKey);
            }
        }
    }
}
