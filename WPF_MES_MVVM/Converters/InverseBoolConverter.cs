using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace WPF_MES_MVVM.Converters;

/// <summary>
/// 使得两个RadioButton 互斥，且一个属性 IsServerMode 就够
/// <RadioButton Content = "服务端" IsChecked="{Binding IsServerMode}"/>
/// <RadioButton Content = "客户端" IsChecked="{Binding IsServerMode, Converter={StaticResource InverseBool}}"/>
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}