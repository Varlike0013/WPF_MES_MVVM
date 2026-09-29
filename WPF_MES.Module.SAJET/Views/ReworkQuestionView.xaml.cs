using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;
using WPF_MES.Shared.Views;

namespace WPF_MES.Module.SAJET.Views;

public partial class ReworkQuestionView : UserControl
{
    public ReworkQuestionView()
    {
        InitializeComponent();
        DataContext = new ReworkQuestionViewModel();
    }

    /// <summary>
    /// 回复输入框：回车 → 触发回复
    /// </summary>
    private void Reply_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is ReworkQuestionViewModel vm &&
            vm.ReplyCommand.CanExecute(null))
        {
            vm.ReplyCommand.Execute(null);
        }
    }

    /// <summary>
    /// 双击单元格 → 弹窗显示完整内容
    /// </summary>
    private void DgvResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 用 IsValid 判断，不用 == null
        if (!dgvResult.CurrentCell.IsValid) return;

        // 取单元格文本
        var cellContent = dgvResult.CurrentCell.Column
            .GetCellContent(dgvResult.CurrentCell.Item);

        string content = cellContent is TextBlock tb ? tb.Text : string.Empty;
        if (content.Length == 0) return;

        // 弹窗
        var dialog = new TextDetailDialog("详细内容", content)
        {
            Owner = Window.GetWindow(this),
        };
        dialog.ShowDialog();
    }
}