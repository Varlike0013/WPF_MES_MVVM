using System.Windows.Controls;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class ExportTravelsView : UserControl
{
    public ExportTravelsView()
    {
        InitializeComponent();
        DataContext = new ExportTravelsViewModel();
    }
}