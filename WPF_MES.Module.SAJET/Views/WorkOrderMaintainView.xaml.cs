using System.Windows.Controls;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class WorkOrderMaintainView : UserControl
{
    public WorkOrderMaintainView()
    {
        InitializeComponent();
        DataContext = new WorkOrderMaintainViewModel();
    }
}