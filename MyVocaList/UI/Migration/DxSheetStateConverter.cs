using System.Globalization;
using DxSheetState = DevExpress.Maui.Controls.BottomSheetState;
using SfSheetState = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState;

namespace MyVocaList.UI.Migration;

/// <summary>
/// Migration-only bridge (deleted in Phase 5): maps the DevExpress <c>BottomSheetState</c> exposed by the
/// ViewModels to <c>SfBottomSheet.State</c>. Bound with resource key <c>DxSheetStateToSf</c>.
/// </summary>
public sealed class DxSheetStateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            DxSheetState.Hidden => SfSheetState.Hidden,
            DxSheetState.HalfExpanded => SfSheetState.HalfExpanded,
            DxSheetState.FullExpanded => SfSheetState.FullExpanded,
            _ => Binding.DoNothing,
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            SfSheetState.Hidden or SfSheetState.Collapsed => DxSheetState.Hidden,
            SfSheetState.HalfExpanded => DxSheetState.HalfExpanded,
            SfSheetState.FullExpanded => DxSheetState.FullExpanded,
            _ => Binding.DoNothing,
        };
}

/// <summary>
/// Migration-only bridge (deleted in Phase 5): maps the DevExpress <c>BottomSheetState</c> to
/// <c>SfBottomSheet.IsOpen</c>. Bound with resource key <c>DxSheetStateToSfIsOpen</c>.
/// </summary>
public sealed class DxSheetStateToIsOpenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DxSheetState state ? state != DxSheetState.Hidden : Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is false ? DxSheetState.Hidden : Binding.DoNothing;
}
