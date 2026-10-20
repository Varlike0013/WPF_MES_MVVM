using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Views;

public partial class ReworkProblemView : UserControl
{
    public ReworkProblemView()
    {
        InitializeComponent();

        DataContext = new ReworkProblemViewModel();
    }


    /// <summary>
    /// 条件输入框回车 → 添加条件
    /// </summary>
    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is ReworkProblemViewModel vm &&
            vm.AddConditionCommand.CanExecute(null))
        {
            vm.AddConditionCommand.Execute(null);
        }
    }

    /// <summary>
    /// 点击条件列表的 × 按钮 → 删除对应的条件
    /// </summary>
    private void RemoveCondition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not ConditionItem item) return;

        if (DataContext is ReworkProblemViewModel vm)
        {
            vm.RemoveConditionCommand.Execute(item);
        }
    }
}