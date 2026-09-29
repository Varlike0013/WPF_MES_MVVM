using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class TgsGroupTestView : UserControl
{
    public TgsGroupTestView()
    {
        InitializeComponent();
        DataContext = new TgsGroupTestViewModel();
    }

    /// <summary>行为ID输入框：回车 → 查询行为组</summary>
    private void GroupId_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is TgsGroupTestViewModel vm &&
            vm.LoadGroupCommand.CanExecute(null))
        {
            vm.LoadGroupCommand.Execute(null);
        }
    }

    /// <summary>TREV输入框：回车 → 执行当前步骤</summary>
    private void Trev_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is TgsGroupTestViewModel vm &&
            vm.ExecuteStepCommand.CanExecute(null))
        {
            vm.ExecuteStepCommand.Execute(null);
        }
    }
}