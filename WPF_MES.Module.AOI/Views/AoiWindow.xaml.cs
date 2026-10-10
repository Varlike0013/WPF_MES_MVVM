using System.Windows;

namespace WPF_MES.Module.AOI.Views;

public partial class AoiWindow : Window
{
    public AoiWindow(string userName)
    {
        InitializeComponent();
        Title = $"AOI 自动光学检测 - {userName}";
    }
}