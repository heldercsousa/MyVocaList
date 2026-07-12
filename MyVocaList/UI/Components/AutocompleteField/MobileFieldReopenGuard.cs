namespace MyVocaList.UI.Components.AutocompleteField;

/// <summary>
/// State machine that governs when the phone Search View (<see cref="AutocompleteMobileField"/>)
/// may be pushed from <see cref="AutocompleteField"/>'s focus handler.
///
/// <para>Extracted as a pure, MAUI-free type for unit testability (mirrors
/// <see cref="AutocompleteWindowClass"/> / <see cref="AutocompleteDebouncer"/>).</para>
///
/// <para>It closes two defects: pushing a second Search View while one is already open
/// (BUG-041 duplication), and the dismissal loop where popping the modal returns focus to the
/// underlying field, whose refocus immediately re-pushes the Search View (BUG-041/042). After a
/// dismissal the next focus is swallowed once, so the modal stays closed; a genuine later focus
/// then reopens normally.</para>
/// </summary>
internal sealed class MobileFieldReopenGuard
{
    private bool _suppressReopen;

    /// <summary>True while a Search View is currently presented.</summary>
    internal bool IsShowing { get; private set; }

    /// <summary>
    /// Called from the field's focus handler. Returns <c>true</c> when the Search View should be
    /// pushed now; <c>false</c> when it must be suppressed (already showing, or the one-shot
    /// post-dismiss refocus).
    /// </summary>
    internal bool RequestShowOnFocus()
    {
        if (_suppressReopen)
        {
            // Consume the single automatic refocus that follows a modal dismissal.
            _suppressReopen = false;
            return false;
        }

        if (IsShowing)
            return false; // never push a duplicate Search View

        IsShowing = true;
        return true;
    }

    /// <summary>
    /// Called when the Search View is dismissed (back button, hardware back, or selection). Marks
    /// the guard closed and arms the one-shot suppression for the refocus that dismissal triggers
    /// on the underlying field.
    /// </summary>
    internal void NotifyDismissed()
    {
        IsShowing = false;
        _suppressReopen = true;
    }
}
