using WPF_MES.Module.ReelUp.ViewModels;

namespace WPF_MES.Module.ReelUp.Views;

public partial class ReelUpMainView : System.Windows.Controls.UserControl
{
    public ReelUpMainView()
    {
        InitializeComponent();
        DataContext = new ReelUpMainViewModel();
    }
}