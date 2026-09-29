using System.Windows.Controls;
using System.Windows.Input;
using WPF_MES.Module.SAJET.ViewModels;

namespace WPF_MES.Module.SAJET.Views;

public partial class MaterialErpView : UserControl
{
    public MaterialErpView()
    {
        InitializeComponent();
        DataContext = new MaterialErpViewModel();
        Loaded += (_, _) => txtSearch.Focus();
    }

    /// <summary>
    /// 查询输入框：回车 → 查询
    /// </summary>
    private void Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (DataContext is MaterialErpViewModel vm &&
            vm.SearchCommand.CanExecute(null))
        {
            vm.SearchCommand.Execute(null);
        }
    }
}