using System.Globalization;
using MyVocaList.UI.ViewModels;
using SfSheetState = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState;

namespace MyVocaList.UI.Converters;

/// <summary>
/// Maps the ViewModel <c>SheetState</c> exposed by the
/// ViewModels to <c>SfBottomSheet.State</c>. Bound with resource key <c>SheetStateToSf</c>.
/// </summary>
public sealed class SheetStateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            SheetState.Hidden => SfSheetState.Hidden,
            SheetState.HalfExpanded => SfSheetState.HalfExpanded,
            SheetState.FullExpanded => SfSheetState.FullExpanded,
            _ => Binding.DoNothing,
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            SfSheetState.Hidden or SfSheetState.Collapsed => SheetState.Hidden,
            SfSheetState.HalfExpanded => SheetState.HalfExpanded,
            SfSheetState.FullExpanded => SheetState.FullExpanded,
            _ => Binding.DoNothing,
        };
}

/// <summary>
/// Maps the ViewModel <c>SheetState</c> to
/// <c>SfBottomSheet.IsOpen</c>. Bound with resource key <c>SheetStateToSfIsOpen</c>.
/// </summary>
public sealed class SheetStateToIsOpenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            SheetState.Hidden => false,
            SheetState.HalfExpanded or SheetState.FullExpanded => true,
            _ => Binding.DoNothing,
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is false ? SheetState.Hidden : Binding.DoNothing;
}
