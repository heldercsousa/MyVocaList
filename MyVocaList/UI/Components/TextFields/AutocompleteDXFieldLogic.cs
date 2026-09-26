using MyVocaList.UI.Models;

namespace MyVocaList.UI.Components.TextFields;

/// <summary>
/// The current interaction mode of a shared autocomplete field. See `design.md § 4`.
/// </summary>
internal enum FieldMode
{
    /// <summary>The field accepts free text input and triggers search.</summary>
    Editable,

    /// <summary>A value is picked and locked; only a clear action re-opens the field.</summary>
    Locked,

    /// <summary>The field is read-only regardless of lock state.</summary>
    ReadOnly
}

/// <summary>
/// The routing decision made when the underlying selection changes. See `design.md § 4`.
/// </summary>
internal enum PickRoute
{
    /// <summary>No pick occurred; no lock transition.</summary>
    None,

    /// <summary>A suggestion was picked; the field should lock.</summary>
    Select
}

/// <summary>
/// Pure decision rules for the shared autocomplete field (`AutocompleteDXField`).
/// No DevExpress or MAUI types are referenced here — see `design.md § 4`.
/// </summary>
internal static class AutocompleteDXFieldLogic
{
    /// <summary>Computes the current <see cref="FieldMode"/>. REQ-ACF-02, -03, -05a, -12b.</summary>
    internal static FieldMode Mode(bool isReadOnly, bool isLocked, string? text) =>
        throw new NotImplementedException();

    /// <summary>Computes the supporting text shown below the field. REQ-ACF-12c.</summary>
    internal static string BottomSupportingText(bool hasError, string? supportingText) =>
        throw new NotImplementedException();

    /// <summary>Decides whether the clear icon is shown. REQ-ACF-07, -12b.</summary>
    internal static bool ShowClearIcon(FieldMode mode, string? text) =>
        throw new NotImplementedException();

    /// <summary>Decides whether a search request should be issued. REQ-ACF-04, -11, -12a, -12b, -13.</summary>
    internal static bool ShouldRequestOptions(FieldMode mode, bool isSearchEnabled, IMenuOptionSource? source, string? query) =>
        throw new NotImplementedException();

    /// <summary>Routes a selection-changed value to a pick decision. REQ-ACF-11.</summary>
    internal static PickRoute RoutePick(object? selectedItem) =>
        throw new NotImplementedException();

    /// <summary>Decides whether a pick should lock the field. REQ-ACF-01.</summary>
    internal static bool LockAfterPick(PickRoute route) =>
        throw new NotImplementedException();

    /// <summary>Decides whether a blur event should be forwarded. REQ-ACF-11, -12b, -17.</summary>
    internal static bool ShouldForwardBlur(object? selectedItem, FieldMode mode) =>
        throw new NotImplementedException();
}
