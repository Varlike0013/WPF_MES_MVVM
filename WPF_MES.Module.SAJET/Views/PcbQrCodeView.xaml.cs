using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Views;

public partial class PcbQrCodeView : UserControl
{
    public PcbQrCodeView()
    {
        InitializeComponent();
        DataContext = new PcbQrCodeViewModel();
        Loaded += (_, _) => txtInput.Focus();
    }

    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is PcbQrCodeViewModel vm &&
            vm.AddConditionCommand.CanExecute(null))
        {
            vm.AddConditionCommand.Execute(null);
        }
    }

    private void RemoveCondition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not ConditionItem item) return;

        if (DataContext is PcbQrCodeViewModel vm)
        {
            vm.RemoveConditionCommand.Execute(item);
        }
    }
}