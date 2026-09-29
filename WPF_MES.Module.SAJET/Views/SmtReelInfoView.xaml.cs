using System.Windows.Controls;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class SmtReelInfoView : UserControl
{
    public SmtReelInfoView()
    {
        InitializeComponent();
        DataContext = new SmtReelInfoViewModel();
    }
}