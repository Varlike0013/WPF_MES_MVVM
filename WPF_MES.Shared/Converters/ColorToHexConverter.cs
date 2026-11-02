using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF_MES.Shared.Converters;

public class ColorToHexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return color.A == 255
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            hex = hex.TrimStart('#');
            try
            {
                if (hex.Length == 6) // RRGGBB
                {
                    byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromRgb(r, g, b);
                }
                else if (hex.Length == 8) // AARRGGBB
                {
                    byte a = System.Convert.ToByte(hex.Substring(0, 2), 16);
                    byte r = System.Convert.ToByte(hex.Substring(2, 2), 16);
                    byte g = System.Convert.ToByte(hex.Substring(4, 2), 16);
                    byte b = System.Convert.ToByte(hex.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
            }
            catch
            {
                // 输入格式错误时返回原值
                return Binding.DoNothing;
            }
        }
        return Binding.DoNothing;
    }
}