using System.Windows;
using System.Windows.Input;

namespace WPF_MES_MVVM.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.LoginViewModel vm) return;

        if (!string.IsNullOrEmpty(vm.UserNo))
            pwdBox.Focus();
        else
            cboUserNo.Focus();
    }

    private void CboUserNo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        pwdBox.Focus();
    }

    private void PwdBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        DoLogin();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        DoLogin();
    }

    private void DoLogin()
    {
        if (DataContext is ViewModels.LoginViewModel vm)
        {
            vm.DoLogin(pwdBox.Password);
        }
    }
}