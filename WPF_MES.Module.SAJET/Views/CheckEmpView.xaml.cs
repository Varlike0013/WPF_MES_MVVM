using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class CheckEmpView : UserControl
{
    public CheckEmpView()
    {
        InitializeComponent();
        DataContext = new CheckEmpViewModel();
        Loaded += (_, _) => txtInput.Focus();
    }

    /// <summary>
    /// 员工查询输入框：回车 → 查询
    /// </summary>
    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is CheckEmpViewModel vm &&
            vm.SearchCommand.CanExecute(null))
        {
            vm.SearchCommand.Execute(null);
        }
    }

    /// <summary>
    /// 复制权限输入框：回车 → 复制权限
    /// </summary>
    private void CopyEmp_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is CheckEmpViewModel vm &&
            vm.CopyRolesCommand.CanExecute(null))
        {
            vm.CopyRolesCommand.Execute(null);
        }
    }
}