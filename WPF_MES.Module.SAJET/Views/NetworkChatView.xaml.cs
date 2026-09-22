using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class NetworkChatView : UserControl
{
    public NetworkChatView()
    {
        InitializeComponent();
        DataContext = new NetworkChatViewModel();
    }

    /// <summary>输入框回车发送</summary>
    private void InputText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is NetworkChatViewModel vm &&
            vm.SendCommand.CanExecute(null))
        {
            vm.SendCommand.Execute(null);
        }
    }
}