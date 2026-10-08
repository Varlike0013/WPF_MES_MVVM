using System.Windows;
using System.Windows.Controls;
using WPF_MES.Shared.Models;
using WPF_MES_MVVM.ViewModels;

namespace WPF_MES_MVVM.Views;

public partial class ShareInfoView : UserControl
{
    private readonly ShareInfoViewModel _vm = new();

    public ShareInfoView()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void Tree_SelectedItemChanged(object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        _vm.SelectedNode = e.NewValue as ShareNode;
    }
}