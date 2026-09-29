using System.Windows;
using System.Windows.Controls;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class ServerIpView : UserControl
{
    public ServerIpView()
    {
        InitializeComponent();
        DataContext = new ServerIpViewModel();
    }

    /// <summary>
    /// TreeView 选中项变化
    /// </summary>
    private void TreeServers_SelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not TreeNodeViewModel node) return;
        if (DataContext is not ServerIpViewModel vm) return;

        vm.OnNodeSelected(node);
    }

    /// <summary>
    /// TreeViewItem 展开 → 通知 ViewModel
    /// </summary>
    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem tvi) return;
        if (tvi.DataContext is not TreeNodeViewModel node) return;
        if (DataContext is not ServerIpViewModel vm) return;

        vm.OnNodeExpanded(node);
    }
}