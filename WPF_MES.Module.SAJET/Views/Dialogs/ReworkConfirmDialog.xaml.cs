using System.Windows;

namespace WPF_MES.Module.SAJET.Views;

public partial class ReworkConfirmDialog : Window
{
    // ============ 输出 ============

    public string Condition => txtCondition.Text.Trim();
    public string Remark => txtRemark.Text.Trim();

    // ============ 构造 ============

    public ReworkConfirmDialog(
        string? newWorkOrder,
        string reworkNo,
        int snCount,
        string routeName,
        string processName,
        bool clearCustomerSN,
        bool clearPack,
        bool clearQc,
        bool clearMac,
        bool clearParts)
    {
        InitializeComponent();

        // 只读展示
        lblNewWorkOrder.Text = string.IsNullOrWhiteSpace(newWorkOrder) ? "(不变)" : newWorkOrder;
        lblReworkNo.Text = string.IsNullOrEmpty(reworkNo) ? "(自动生成)" : reworkNo;
        lblSnCount.Text = snCount.ToString();
        lblRoute.Text = routeName;
        lblProcess.Text = processName;

        // 重工条件：用 CheckBox 展示
        chkClearCustomerSN.IsChecked = clearCustomerSN;
        chkClearPack.IsChecked = clearPack;
        chkClearQc.IsChecked = clearQc;
        chkClearMac.IsChecked = clearMac;
        chkClearParts.IsChecked = clearParts;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}