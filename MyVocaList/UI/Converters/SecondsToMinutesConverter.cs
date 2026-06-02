namespace MyVocaList.UI.Converters;

public sealed class SecondsToMinutesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not int seconds)
            return string.Empty;

        if (seconds <= 0)
            return string.Empty;

        int totalMinutes = seconds / 60;
        int remainingSeconds = seconds % 60;

        return $"{totalMinutes}:{remainingSeconds:D2}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
