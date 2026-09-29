using System.Windows.Controls;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class CustomerEcspartView : UserControl
{
    public CustomerEcspartView()
    {
        InitializeComponent();
        DataContext = new CustomerEcspartViewModel();
    }
}