using System.Windows;
using System.Windows.Input;
using WPF_MES_MVVM.ViewModels;

namespace WPF_MES_MVVM.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private LoginViewModel? Vm => DataContext as LoginViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm == null) return;

        if (!string.IsNullOrEmpty(Vm.UserNo))
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

    private void BtnLogin_Click(object sender, RoutedEventArgs e) => DoLogin();

    private void DoLogin()
    {
        Vm?.DoLogin(pwdBox.Password);
    }
}