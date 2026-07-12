using Microsoft.Maui.Devices;

namespace MyVocaList.UI.Components.AutocompleteField;

/// <summary>
/// Pure idiom-branch check — extracted for unit testability (no MAUI runtime dependency),
/// mirroring <see cref="AutocompleteDebouncer"/>'s extraction rationale.
/// </summary>
internal static class AutocompleteWindowClass
{
    internal static bool IsCompactWindow(IDeviceInfo deviceInfo) =>
        deviceInfo?.Idiom == DeviceIdiom.Phone;
}
