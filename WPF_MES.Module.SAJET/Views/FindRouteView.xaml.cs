using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class FindRouteView : UserControl
{
    public FindRouteView()
    {
        InitializeComponent();
        DataContext = new FindRouteViewModel();
    }

    /// <summary>
    /// SN 输入框：回车 → 查询走过的流程
    /// </summary>
    private void Sn_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is FindRouteViewModel vm &&
            vm.SearchSnCommand.CanExecute(null))
        {
            vm.SearchSnCommand.Execute(null);
        }
    }
}