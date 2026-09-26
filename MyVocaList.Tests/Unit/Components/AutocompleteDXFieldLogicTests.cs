using MyVocaList.Contracts.Models;
using MyVocaList.UI.Components.TextFields;
using MyVocaList.UI.Models;

namespace MyVocaList.Tests.Unit.Components;

public class AutocompleteDXFieldLogicTests
{
    private static readonly IMenuOptionSource Source =
        new MenuOptionSource((_, _) => Task.FromResult<IReadOnlyList<AutocompleteSuggestion>>([]));

    private static readonly AutocompleteSuggestion Suggestion = new("John Doe", "JD", null!);

    // [AC] REQ-ACF-12b: a read-only field is always ReadOnly, regardless of lock state or text.
    [Theory]
    [InlineData(true, "Don't Cry")]
    [InlineData(true, "")]
    [InlineData(false, "Don't Cry")]
    [InlineData(false, "")]
    public void Mode_ReadOnly_ReturnsReadOnly_WhateverLockAndText(bool isLocked, string text)
    {
        var mode = AutocompleteDXFieldLogic.Mode(isReadOnly: true, isLocked, text);

        Assert.Equal(FieldMode.ReadOnly, mode);
    }

    // [AC] REQ-ACF-02: a locked field with text is Locked.
    [Fact]
    public void Mode_LockedWithText_ReturnsLocked()
    {
        var mode = AutocompleteDXFieldLogic.Mode(isReadOnly: false, isLocked: true, "Johnny Cash");

        Assert.Equal(FieldMode.Locked, mode);
    }

    // [AC] REQ-ACF-05a: a locked field with no text (null/empty/whitespace) falls back to Editable.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Mode_LockedWithEmptyOrWhitespaceText_ReturnsEditable(string? text)
    {
        var mode = AutocompleteDXFieldLogic.Mode(isReadOnly: false, isLocked: true, text);

        Assert.Equal(FieldMode.Editable, mode);
    }

    // [AC] REQ-ACF-03: an unlocked field with text is Editable.
    [Fact]
    public void Mode_NotLockedWithText_ReturnsEditable()
    {
        var mode = AutocompleteDXFieldLogic.Mode(isReadOnly: false, isLocked: false, "Johnny Cash");

        Assert.Equal(FieldMode.Editable, mode);
    }

    // [AC] REQ-ACF-12c: with no error, the supporting text is shown as-is.
    [Fact]
    public void BottomSupportingText_NoError_ReturnsSupportingText()
    {
        var result = AutocompleteDXFieldLogic.BottomSupportingText(hasError: false, "Online search unavailable");

        Assert.Equal("Online search unavailable", result);
    }

    // [AC] REQ-ACF-12c: with an error, the supporting text is suppressed.
    [Fact]
    public void BottomSupportingText_HasError_ReturnsEmpty()
    {
        var result = AutocompleteDXFieldLogic.BottomSupportingText(hasError: true, "Online search unavailable");

        Assert.Equal(string.Empty, result);
    }

    // [AC] REQ-ACF-12c: with no error and null supporting text, the result is empty (not null).
    [Fact]
    public void BottomSupportingText_NoErrorNullText_ReturnsEmpty()
    {
        var result = AutocompleteDXFieldLogic.BottomSupportingText(hasError: false, null);

        Assert.Equal(string.Empty, result);
    }

    // [AC] REQ-ACF-07: the clear icon is shown when editable with text.
    [Fact]
    public void ShowClearIcon_EditableWithText_ReturnsTrue()
    {
        var result = AutocompleteDXFieldLogic.ShowClearIcon(FieldMode.Editable, "jo");

        Assert.True(result);
    }

    // [AC] REQ-ACF-07: the clear icon is shown when locked with text.
    [Fact]
    public void ShowClearIcon_LockedWithText_ReturnsTrue()
    {
        var result = AutocompleteDXFieldLogic.ShowClearIcon(FieldMode.Locked, "Johnny Cash");

        Assert.True(result);
    }

    // [AC] REQ-ACF-07: the clear icon is hidden when there is no text (null/empty), in Editable mode.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShowClearIcon_EmptyText_ReturnsFalse(string? text)
    {
        var result = AutocompleteDXFieldLogic.ShowClearIcon(FieldMode.Editable, text);

        Assert.False(result);
    }

    // [AC] REQ-ACF-12b: the clear icon is never shown in ReadOnly mode, even with text.
    [Fact]
    public void ShowClearIcon_ReadOnlyWithText_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.ShowClearIcon(FieldMode.ReadOnly, "Don't Cry");

        Assert.False(result);
    }

    // [AC] REQ-ACF-13: a trimmed query at or above the minimum local length requests options.
    [Theory]
    [InlineData("ab")]
    [InlineData(" ab ")]
    public void ShouldRequestOptions_EditableSearchOnTrimmedTwoChars_ReturnsTrue(string query)
    {
        var result = AutocompleteDXFieldLogic.ShouldRequestOptions(FieldMode.Editable, isSearchEnabled: true, Source, query);

        Assert.True(result);
    }

    // [AC] REQ-ACF-13: a trimmed query below the minimum local length never requests options.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    [InlineData(" a")]
    [InlineData("a ")]
    [InlineData("   ")]
    public void ShouldRequestOptions_BelowTrimmedThreshold_ReturnsFalse(string? query)
    {
        var result = AutocompleteDXFieldLogic.ShouldRequestOptions(FieldMode.Editable, isSearchEnabled: true, Source, query);

        Assert.False(result);
    }

    // [AC] REQ-ACF-04: a locked field never requests options.
    [Fact]
    public void ShouldRequestOptions_Locked_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.ShouldRequestOptions(FieldMode.Locked, isSearchEnabled: true, Source, "Johnny Cash");

        Assert.False(result);
    }

    // [AC] REQ-ACF-12a: with search disabled, no options are requested.
    [Fact]
    public void ShouldRequestOptions_SearchDisabled_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.ShouldRequestOptions(FieldMode.Editable, isSearchEnabled: false, Source, "ab");

        Assert.False(result);
    }

    // [AC] REQ-ACF-12b: a read-only field never requests options.
    [Fact]
    public void ShouldRequestOptions_ReadOnly_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.ShouldRequestOptions(FieldMode.ReadOnly, isSearchEnabled: true, Source, "ab");

        Assert.False(result);
    }

    // [AC] REQ-ACF-11: with no option source configured, no options are requested.
    [Fact]
    public void ShouldRequestOptions_NullSource_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.ShouldRequestOptions(FieldMode.Editable, isSearchEnabled: true, source: null, "ab");

        Assert.False(result);
    }

    // [AC] REQ-ACF-11: selecting an AutocompleteSuggestion routes to Select.
    [Fact]
    public void RoutePick_AutocompleteSuggestion_ReturnsSelect()
    {
        var route = AutocompleteDXFieldLogic.RoutePick(Suggestion);

        Assert.Equal(PickRoute.Select, route);
    }

    // [AC] REQ-ACF-11 (b): pick routing — null or a non-suggestion value routes to None.
    [Theory]
    [InlineData(null)]
    [InlineData("text")]
    [InlineData(42)]
    public void RoutePick_NullOrNonSuggestion_ReturnsNone(object? selectedItem)
    {
        var route = AutocompleteDXFieldLogic.RoutePick(selectedItem);

        Assert.Equal(PickRoute.None, route);
    }

    // [AC] REQ-ACF-01: a Select route locks the field.
    [Fact]
    public void LockAfterPick_Select_ReturnsTrue()
    {
        var result = AutocompleteDXFieldLogic.LockAfterPick(PickRoute.Select);

        Assert.True(result);
    }

    // [AC] REQ-ACF-01: a None route does not lock the field.
    [Fact]
    public void LockAfterPick_None_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.LockAfterPick(PickRoute.None);

        Assert.False(result);
    }

    // [AC] REQ-ACF-17: with no pick and an editable field, blur is forwarded.
    [Fact]
    public void ShouldForwardBlur_NoPickEditable_ReturnsTrue()
    {
        var result = AutocompleteDXFieldLogic.ShouldForwardBlur(null, FieldMode.Editable);

        Assert.True(result);
    }

    // [AC] REQ-ACF-11: with a selected item, blur is not forwarded.
    [Fact]
    public void ShouldForwardBlur_WithSelectedItem_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.ShouldForwardBlur(Suggestion, FieldMode.Editable);

        Assert.False(result);
    }

    // [AC] REQ-ACF-11: a locked field never forwards blur.
    [Fact]
    public void ShouldForwardBlur_Locked_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.ShouldForwardBlur(null, FieldMode.Locked);

        Assert.False(result);
    }

    // [AC] REQ-ACF-12b: a read-only field never forwards blur.
    [Fact]
    public void ShouldForwardBlur_ReadOnly_ReturnsFalse()
    {
        var result = AutocompleteDXFieldLogic.ShouldForwardBlur(null, FieldMode.ReadOnly);

        Assert.False(result);
    }
}
