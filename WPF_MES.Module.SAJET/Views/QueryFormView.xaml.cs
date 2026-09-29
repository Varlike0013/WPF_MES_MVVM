using System.Windows.Controls;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class QueryFormView : UserControl
{
    public QueryFormView()
    {
        InitializeComponent();
        DataContext = new QueryFormViewModel();
    }
}