using System.Windows;

namespace WPF_MES_MVVM.Views;

public partial class NetworkLoginWindow : Window
{
    public string UserName { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;

    public NetworkLoginWindow(string path)
    {
        InitializeComponent();
        txtPath.Text = path;
    }

    private void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        UserName = txtUser.Text.Trim();
        Password = txtPwd.Password;
        DialogResult = true;
    }
}