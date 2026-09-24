using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Views;

public partial class ClearKeyPartsView : UserControl
{
    public ClearKeyPartsView()
    {
        InitializeComponent();
        DataContext = new ClearKeyPartsViewModel();
    }

    /// <summary>
    /// 顶部条件输入框：回车 → 添加条件
    /// </summary>
    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is ClearKeyPartsViewModel vm &&
            vm.AddConditionCommand.CanExecute(null))
        {
            vm.AddConditionCommand.Execute(null);
        }
    }

    /// <summary>
    /// 条件列表 × 按钮 → 删除条件
    /// </summary>
    private void RemoveCondition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not ConditionItem item) return;

        if (DataContext is ClearKeyPartsViewModel vm)
        {
            vm.RemoveConditionCommand.Execute(item);
        }
    }

    /// <summary>
    /// 单 SN 输入框：回车 → 查询该 SN 的关键件
    /// </summary>
    private void Sn_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is ClearKeyPartsViewModel vm &&
            vm.LoadSnDetailCommand.CanExecute(null))
        {
            vm.LoadSnDetailCommand.Execute(null);
        }
    }
}