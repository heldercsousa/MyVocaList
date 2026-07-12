using MyVocaList.UI.Components.AutocompleteField;

namespace MyVocaList.Tests.Unit.Components;

public class MobileFieldReopenGuardTests
{
    [Fact]
    // [AC] AC-1 (Phone render): the first focus while compact shows the Search View.
    public void RequestShowOnFocus_FirstFocus_ReturnsTrueAndMarksShowing()
    {
        var guard = new MobileFieldReopenGuard();

        var show = guard.RequestShowOnFocus();

        Assert.True(show);
        Assert.True(guard.IsShowing);
    }

    [Fact]
    // [AC] BUG-041: while the Search View is already showing, a second focus must NOT push a
    // duplicate instance.
    public void RequestShowOnFocus_WhileShowing_ReturnsFalse()
    {
        var guard = new MobileFieldReopenGuard();
        guard.RequestShowOnFocus();

        var secondShow = guard.RequestShowOnFocus();

        Assert.False(secondShow);
    }

    [Fact]
    // [AC] BUG-041/042: after dismissal the underlying field regains focus; that immediate refocus
    // must be swallowed so the Search View does not instantly reappear (the dismissal loop).
    public void RequestShowOnFocus_ImmediatelyAfterDismiss_IsSuppressed()
    {
        var guard = new MobileFieldReopenGuard();
        guard.RequestShowOnFocus();

        guard.NotifyDismissed();
        var reopen = guard.RequestShowOnFocus();

        Assert.False(reopen);
        Assert.False(guard.IsShowing);
    }

    [Fact]
    // [AC] BUG-042: after the suppressed refocus is consumed, a genuine later focus reopens normally.
    public void RequestShowOnFocus_SecondFocusAfterDismiss_ReopensNormally()
    {
        var guard = new MobileFieldReopenGuard();
        guard.RequestShowOnFocus();
        guard.NotifyDismissed();
        guard.RequestShowOnFocus(); // suppressed refocus consumes the guard

        var genuineReopen = guard.RequestShowOnFocus();

        Assert.True(genuineReopen);
        Assert.True(guard.IsShowing);
    }
}
