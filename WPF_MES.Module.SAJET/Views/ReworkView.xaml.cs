using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Views;

public partial class ReworkView : UserControl
{
    public ReworkView()
    {
        InitializeComponent();
        DataContext = new ReworkViewModel();
        Loaded += (_, _) => txtInput.Focus();
    }

    /// <summary>输入框回车 → 添加条件</summary>
    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is ReworkViewModel vm &&
            vm.AddConditionCommand.CanExecute(null))
        {
            vm.AddConditionCommand.Execute(null);
        }
    }

    /// <summary>新工单框回车 → 查流程</summary>
    private void NewWorkOrder_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is ReworkViewModel vm &&
            vm.LoadRouteByWorkOrderCommand.CanExecute(null))
        {
            vm.LoadRouteByWorkOrderCommand.Execute(null);
        }
    }

    /// <summary>流程名回车 → 加载工序</summary>
    private void RouteName_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is ReworkViewModel vm &&
            vm.ReloadProcessesCommand.CanExecute(null))
        {
            vm.ReloadProcessesCommand.Execute(null);
        }
    }

    /// <summary>条件表 × 按钮 → 删除条件</summary>
    private void RemoveCondition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not ConditionItem item) return;

        if (DataContext is ReworkViewModel vm)
        {
            vm.RemoveConditionCommand.Execute(item);
        }
    }
}