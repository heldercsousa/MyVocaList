using System.Globalization;
using MyVocaList.UI.Migration;
using DxSheetState = DevExpress.Maui.Controls.BottomSheetState;
using SfSheetState = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState;

namespace MyVocaList.Tests.Unit.Migration;

public class DxSheetStateConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private readonly DxSheetStateConverter _state = new();
    private readonly DxSheetStateToIsOpenConverter _isOpen = new();

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void Convert_Hidden_IsClosed()
        => Assert.Equal(SfSheetState.Hidden, _state.Convert(DxSheetState.Hidden, typeof(SfSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void Convert_HalfExpanded_IsHalfExpanded()
        => Assert.Equal(SfSheetState.HalfExpanded, _state.Convert(DxSheetState.HalfExpanded, typeof(SfSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void Convert_FullExpanded_IsFullExpanded()
        => Assert.Equal(SfSheetState.FullExpanded, _state.Convert(DxSheetState.FullExpanded, typeof(SfSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_ClosedByUser_IsHidden()
        => Assert.Equal(DxSheetState.Hidden, _state.ConvertBack(SfSheetState.Hidden, typeof(DxSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_DraggedToFull_IsFullExpanded()
        => Assert.Equal(DxSheetState.FullExpanded, _state.ConvertBack(SfSheetState.FullExpanded, typeof(DxSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_DraggedToHalf_IsHalfExpanded()
        => Assert.Equal(DxSheetState.HalfExpanded, _state.ConvertBack(SfSheetState.HalfExpanded, typeof(DxSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_Collapsed_IsHidden()
        => Assert.Equal(DxSheetState.Hidden, _state.ConvertBack(SfSheetState.Collapsed, typeof(DxSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpen_Hidden_IsFalse()
        => Assert.False((bool)_isOpen.Convert(DxSheetState.Hidden, typeof(bool), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpen_HalfExpanded_IsTrue()
        => Assert.True((bool)_isOpen.Convert(DxSheetState.HalfExpanded, typeof(bool), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpenBack_False_IsHidden()
        => Assert.Equal(DxSheetState.Hidden, _isOpen.ConvertBack(false, typeof(DxSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpen_FullExpanded_IsTrue()
        => Assert.True((bool)_isOpen.Convert(DxSheetState.FullExpanded, typeof(bool), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpenBack_True_DoesNothing()
        => Assert.Same(Binding.DoNothing, _isOpen.ConvertBack(true, typeof(DxSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Theory]
    [InlineData(null)]
    [InlineData("Hidden")]
    [InlineData(1)]
    public void Convert_UnknownInput_DoesNothing(object input)
    {
        Assert.Same(Binding.DoNothing, _state.Convert(input, typeof(SfSheetState), null, Culture));
        Assert.Same(Binding.DoNothing, _state.ConvertBack(input, typeof(DxSheetState), null, Culture));
        Assert.Same(Binding.DoNothing, _isOpen.Convert(input, typeof(bool), null, Culture));
    }

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void Convert_UndefinedDxValue_DoesNothing()
    {
        Assert.Same(Binding.DoNothing, _state.Convert((DxSheetState)99, typeof(SfSheetState), null, Culture));
        Assert.True((bool)_isOpen.Convert((DxSheetState)99, typeof(bool), null, Culture));
    }

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_UndefinedSfValue_DoesNothing()
        => Assert.Same(Binding.DoNothing, _state.ConvertBack((SfSheetState)99, typeof(DxSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpenBack_NonBool_DoesNothing()
        => Assert.Same(Binding.DoNothing, _isOpen.ConvertBack("x", typeof(DxSheetState), null, Culture));
}
