using System.Windows;

namespace WPF_MES_MVVM.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
    }
    /// <summary>
    /// 退出程序
    /// </summary>
    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要退出程序吗？", "提示",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }
}