using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace WPF_MES.Shared.Models;

public partial class ColorGroup : ObservableObject
{
    [ObservableProperty] private Color _color;
    [ObservableProperty] private string _hexCode = string.Empty;
    [ObservableProperty] private int _count;
    [ObservableProperty] private SolidColorBrush _brush;

    public ColorGroup(Color color, int count)
    {
        _brush = new SolidColorBrush(color);
        Color = color;
        Count = count;
    }

    partial void OnColorChanged(Color value)
    {
        // 如果全不透明，只显示 #RRGGBB；如果有透明度，显示 #AARRGGBB
        HexCode = value.A == 255
            ? $"#{value.R:X2}{value.G:X2}{value.B:X2}"
            : $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";

        Brush.Color = value;
    }
}