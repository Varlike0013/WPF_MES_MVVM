using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private void ImgPreview_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
        imgScale.ScaleX = Math.Max(0.1, imgScale.ScaleX * factor);
        imgScale.ScaleY = Math.Max(0.1, imgScale.ScaleY * factor);
        e.Handled = true;
    }
}