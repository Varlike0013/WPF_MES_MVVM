using System.Windows.Controls;
using WPF_MES.Module.AOI.ViewModels;

namespace WPF_MES.Module.AOI.Views;

public partial class AoiMainView : UserControl
{
    public AoiMainView()
    {
        InitializeComponent();
        DataContext = new AoiViewModel();
    }
}