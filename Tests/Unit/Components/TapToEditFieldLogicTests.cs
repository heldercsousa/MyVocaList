using MyVocaList.UI.Components.TextFields;

namespace MyVocaList.Tests.Unit.Components;

public class TapToEditFieldLogicTests
{
    // [AC] REQ-PIC-23: a non-blank value, not being edited and with no error, shows as text.
    [Theory]
    [InlineData("Bohemian Rhapsody")]
    [InlineData(" Queen ")]
    [InlineData("x")]
    public void ShowsText_DefinedValueNotEditingNoError_ReturnsTrue(string text)
    {
        var showsText = TapToEditFieldLogic.ShowsText(text, isEditing: false, hasError: false);

        Assert.True(showsText);
    }

    // [AC] REQ-PIC-24: a blank value (null, empty or whitespace) shows the input box.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void ShowsText_BlankValue_ReturnsFalse(string? text)
    {
        var showsText = TapToEditFieldLogic.ShowsText(text, isEditing: false, hasError: false);

        Assert.False(showsText);
    }

    // [AC] REQ-PIC-24: a field being edited shows the input box, even with a defined value.
    [Fact]
    public void ShowsText_Editing_ReturnsFalse()
    {
        var showsText = TapToEditFieldLogic.ShowsText("Bohemian Rhapsody", isEditing: true, hasError: false);

        Assert.False(showsText);
    }

    // [AC] REQ-PIC-24: a field that shows an error shows the input box, even with a defined value.
    [Fact]
    public void ShowsText_HasError_ReturnsFalse()
    {
        var showsText = TapToEditFieldLogic.ShowsText("Bohemian Rhapsody", isEditing: false, hasError: true);

        Assert.False(showsText);
    }

    // [AC] REQ-PIC-24: every combination with editing or an error shows the input box, whatever the text.
    [Theory]
    [InlineData(null, true, true)]
    [InlineData("", true, false)]
    [InlineData("", false, true)]
    [InlineData("Queen", true, true)]
    [InlineData("   ", true, true)]
    public void ShowsText_EditingOrError_ReturnsFalse_WhateverText(string? text, bool isEditing, bool hasError)
    {
        var showsText = TapToEditFieldLogic.ShowsText(text, isEditing, hasError);

        Assert.False(showsText);
    }

    // [AC] REQ-PIC-26: a tap on a picked value releases the picked state when the field is set to release it.
    [Fact]
    public void ShouldReleasePick_ReleasesOnEditAndLocked_ReturnsTrue()
    {
        var release = TapToEditFieldLogic.ShouldReleasePick(releasesPickOnEdit: true, isLocked: true);

        Assert.True(release);
    }

    // [AC] REQ-PIC-30: with release turned off, or with no picked value, a tap leaves the lock as it is.
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ShouldReleasePick_NotReleasingOrNotLocked_ReturnsFalse(bool releasesPickOnEdit, bool isLocked)
    {
        var release = TapToEditFieldLogic.ShouldReleasePick(releasesPickOnEdit, isLocked);

        Assert.False(release);
    }

    // [AC] REQ-PIC-25: a blur ends the editor state only when the inner editor has not taken focus back.
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ShouldEndEditingAfterBlur_ReturnsTrueOnlyWhenEditorStillUnfocused(bool isEditorFocused, bool expected)
    {
        var endEditing = TapToEditFieldLogic.ShouldEndEditingAfterBlur(isEditorFocused);

        Assert.Equal(expected, endEditing);
    }
}
