using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Views;

public partial class TgsGroupView : UserControl
{
    public TgsGroupView()
    {
        InitializeComponent();

        var vm = new TgsGroupViewModel();
        DataContext = vm;

        // 直接赋值 RichTextBox 引用
        vm.SourceRichTextBox = rtbSource;
    }

    /// <summary>行为组查询框：回车 → 查询</summary>
    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is TgsGroupViewModel vm &&
            vm.SearchCommand.CanExecute(null))
        {
            vm.SearchCommand.Execute(null);
        }
    }

    /// <summary>源码搜索框：回车 → 高亮</summary>
    private void Query_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is TgsGroupViewModel vm &&
            vm.HighlightCommand.CanExecute(null))
        {
            vm.HighlightCommand.Execute(null);
        }
    }

    /// <summary>存储过程名输入框：回车 → 构建参数表单</summary>
    private void Proc_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is TgsGroupViewModel vm &&
            vm.BuildCommand.CanExecute(null))
        {
            vm.BuildCommand.Execute(null);
        }
    }
}