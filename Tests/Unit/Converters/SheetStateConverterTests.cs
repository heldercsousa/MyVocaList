using System.Globalization;
using MyVocaList.UI.Converters;
using MyVocaList.UI.ViewModels;
using SfSheetState = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState;

namespace MyVocaList.Tests.Unit.Converters;

public class SheetStateConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private readonly SheetStateConverter _state = new();
    private readonly SheetStateToIsOpenConverter _isOpen = new();

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void Convert_Hidden_IsClosed()
        => Assert.Equal(SfSheetState.Hidden, _state.Convert(SheetState.Hidden, typeof(SfSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void Convert_HalfExpanded_IsHalfExpanded()
        => Assert.Equal(SfSheetState.HalfExpanded, _state.Convert(SheetState.HalfExpanded, typeof(SfSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void Convert_FullExpanded_IsFullExpanded()
        => Assert.Equal(SfSheetState.FullExpanded, _state.Convert(SheetState.FullExpanded, typeof(SfSheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_ClosedByUser_IsHidden()
        => Assert.Equal(SheetState.Hidden, _state.ConvertBack(SfSheetState.Hidden, typeof(SheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_DraggedToFull_IsFullExpanded()
        => Assert.Equal(SheetState.FullExpanded, _state.ConvertBack(SfSheetState.FullExpanded, typeof(SheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_DraggedToHalf_IsHalfExpanded()
        => Assert.Equal(SheetState.HalfExpanded, _state.ConvertBack(SfSheetState.HalfExpanded, typeof(SheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_Collapsed_IsHidden()
        => Assert.Equal(SheetState.Hidden, _state.ConvertBack(SfSheetState.Collapsed, typeof(SheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpen_Hidden_IsFalse()
        => Assert.False((bool)_isOpen.Convert(SheetState.Hidden, typeof(bool), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpen_HalfExpanded_IsTrue()
        => Assert.True((bool)_isOpen.Convert(SheetState.HalfExpanded, typeof(bool), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpenBack_False_IsHidden()
        => Assert.Equal(SheetState.Hidden, _isOpen.ConvertBack(false, typeof(SheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpen_FullExpanded_IsTrue()
        => Assert.True((bool)_isOpen.Convert(SheetState.FullExpanded, typeof(bool), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpenBack_True_DoesNothing()
        => Assert.Same(Binding.DoNothing, _isOpen.ConvertBack(true, typeof(SheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Theory]
    [InlineData(null)]
    [InlineData("Hidden")]
    [InlineData(1)]
    public void Convert_UnknownInput_DoesNothing(object input)
    {
        Assert.Same(Binding.DoNothing, _state.Convert(input, typeof(SfSheetState), null, Culture));
        Assert.Same(Binding.DoNothing, _state.ConvertBack(input, typeof(SheetState), null, Culture));
        Assert.Same(Binding.DoNothing, _isOpen.Convert(input, typeof(bool), null, Culture));
    }

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void Convert_UndefinedValue_DoesNothing()
    {
        Assert.Same(Binding.DoNothing, _state.Convert((SheetState)99, typeof(SfSheetState), null, Culture));
        Assert.Same(Binding.DoNothing, _isOpen.Convert((SheetState)99, typeof(bool), null, Culture));
    }

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void ConvertBack_UndefinedSfValue_DoesNothing()
        => Assert.Same(Binding.DoNothing, _state.ConvertBack((SfSheetState)99, typeof(SheetState), null, Culture));

    // [AC] REQ-UIMIG-07: with a bottom sheet open, back closes it
    [Fact]
    public void IsOpenBack_NonBool_DoesNothing()
        => Assert.Same(Binding.DoNothing, _isOpen.ConvertBack("x", typeof(SheetState), null, Culture));
}
