namespace MyVocaList.UI.Components.TextFields;

/// <summary>
/// Pure decision rules for <c>TapToEditField</c> (design.md § 2.5 of the form-pictures change).
/// No DevExpress or MAUI types are referenced here, so every rule is unit-tested.
/// </summary>
internal static class TapToEditFieldLogic
{
    /// <summary>
    /// Wait after a blur before the editor state may end, so a pick, the X icon and blur validation
    /// (which may set <c>HasError</c>) finish first (design.md § 2.5).
    /// </summary>
    public const int BlurSettleMilliseconds = 250;

    /// <summary>
    /// The text state (REQ-PIC-23) shows WHILE the value is not blank, the field is not being edited and
    /// shows no error; otherwise the editor state shows (REQ-PIC-24).
    /// </summary>
    public static bool ShowsText(string? text, bool isEditing, bool hasError)
        => !isEditing && !hasError && !string.IsNullOrWhiteSpace(text);

    /// <summary>
    /// A tap on the text releases the picked state of the wrapped field when the wrapper is set to
    /// release it and the field is locked (REQ-PIC-26, REQ-PIC-30). The text is kept.
    /// </summary>
    public static bool ShouldReleasePick(bool releasesPickOnEdit, bool isLocked)
        => releasesPickOnEdit && isLocked;

    /// <summary>
    /// After a blur and the settle delay, the editor state ends only when the inner editor has not taken
    /// focus back in the meantime (REQ-PIC-25).
    /// </summary>
    public static bool ShouldEndEditingAfterBlur(bool isEditorFocused) => !isEditorFocused;
}
