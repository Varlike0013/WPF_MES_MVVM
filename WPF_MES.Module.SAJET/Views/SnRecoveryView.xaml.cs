using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class SnRecoveryView : UserControl
{
    public SnRecoveryView()
    {
        InitializeComponent();
        DataContext = new SnRecoveryViewModel();
        Loaded += (_, _) => txtInput.Focus();
    }
    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is SnRecoveryViewModel vm &&
            vm.SearchCommand.CanExecute(null))
        {
            vm.SearchCommand.Execute(null);
        }
    }
}