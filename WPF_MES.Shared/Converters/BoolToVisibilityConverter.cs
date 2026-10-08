using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPF_MES.Shared.Converters;

/// <summary>
/// true → Visible，false → Collapsed
/// ConverterParameter="Invert" 时取反
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool flag = value is true;
        if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}