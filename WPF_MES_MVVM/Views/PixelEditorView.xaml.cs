using System.Windows.Controls;
using WPF_MES_MVVM.ViewModels;

namespace WPF_MES_MVVM.Views;

public partial class PixelEditorView : UserControl
{
    public PixelEditorView()
    {
        InitializeComponent();
        DataContext = new PixelEditorViewModel();
    }
}