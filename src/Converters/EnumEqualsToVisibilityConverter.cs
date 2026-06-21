using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Game_Daily_Routine_Launcher;

public sealed class EnumEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is null)
            return Visibility.Collapsed;

        var expected = parameter.ToString() ?? string.Empty;
        var actual = value?.ToString() ?? string.Empty;
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
