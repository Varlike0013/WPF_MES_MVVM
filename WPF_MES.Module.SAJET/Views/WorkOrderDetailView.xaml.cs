using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class WorkOrderDetailView : UserControl
{
    public WorkOrderDetailView()
    {
        InitializeComponent();
        DataContext = new WorkOrderDetailViewModel();
        Loaded += (_, _) => txtWorkOrder.Focus();
    }

    /// <summary>
    /// 工单号输入框回车 → 查询
    /// </summary>
    private void TxtWorkOrder_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is WorkOrderDetailViewModel vm &&
            vm.SearchCommand.CanExecute(null))
        {
            vm.SearchCommand.Execute(null);
        }
    }
}