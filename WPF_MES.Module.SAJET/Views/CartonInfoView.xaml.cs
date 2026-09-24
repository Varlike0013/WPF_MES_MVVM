using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.ViewModels;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Views;

public partial class CartonInfoView : UserControl
{
    public CartonInfoView()
    {
        InitializeComponent();
        DataContext = new CartonInfoViewModel();
    }

    /// <summary>
    /// 顶部条件输入框：回车 → 添加条件
    /// </summary>
    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is CartonInfoViewModel vm &&
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

        if (DataContext is CartonInfoViewModel vm)
        {
            vm.RemoveConditionCommand.Execute(item);
        }
    }

    /// <summary>
    /// 箱号退出 QC 输入框：回车 → 触发 WQC 命令
    /// </summary>
    private void Wqc_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is CartonInfoViewModel vm &&
            vm.WqcCommand.CanExecute(null))
        {
            vm.WqcCommand.Execute(null);
        }
    }
}