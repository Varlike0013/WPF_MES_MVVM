using System.Windows;

namespace WPF_MES_MVVM.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show("确定要退出程序吗？", "提示",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
            Application.Current.Shutdown();
    }
}