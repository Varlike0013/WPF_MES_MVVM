using System.Windows;

namespace WPF_MES_MVVM.Views;

public partial class NetworkLoginWindow : Window
{
    public string UserName { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;

    /// <summary>无参构造：Designer 预览 / 反射用</summary>
    public NetworkLoginWindow()
    {
        InitializeComponent();
    }

    /// <summary>带参构造：正常业务入口</summary>
    public NetworkLoginWindow(string path) : this()
    {
        txtPath.Text = path;
    }

    private void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        UserName = txtUser.Text.Trim();
        Password = txtPwd.Password;
        DialogResult = true;
    }
}