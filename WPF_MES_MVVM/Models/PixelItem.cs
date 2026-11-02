using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace WPF_MES_MVVM.Models;

public partial class PixelItem : ObservableObject
{
    public int X { get; }
    public int Y { get; }

    // 界面直接绑定这个 Brush
    public SolidColorBrush Brush { get; }

    [ObservableProperty] private Color _color;

    public PixelItem(int x, int y, Color color)
    {
        X = x;
        Y = y;
        _color = color;
        Brush = new SolidColorBrush(color);
    }

    partial void OnColorChanged(Color value)
    {
        Brush.Color = value; // 颜色改变时，更新Brush
    }
}