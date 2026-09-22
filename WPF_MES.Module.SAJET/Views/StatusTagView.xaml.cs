using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPF_MES.Contracts;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class StatusTagView : UserControl
{
    public StatusTagView()
    {
        InitializeComponent();
        DataContext = new StatusTagViewModel();
        Loaded += (_, _) => txtInput.Focus();
    }

    /// <summary>
    /// 输入框回车 → 查询
    /// </summary>
    private void TxtInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is StatusTagViewModel vm &&
            vm.SearchCommand.CanExecute(null))
        {
            vm.SearchCommand.Execute(null);
        }
    }
}