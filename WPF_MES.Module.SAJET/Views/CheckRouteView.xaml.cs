using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class CheckRouteView : UserControl
{
    public CheckRouteView()
    {
        InitializeComponent();
        DataContext = new CheckRouteViewModel();
        Loaded += (_, _) => txtFilter.Focus();
    }
}