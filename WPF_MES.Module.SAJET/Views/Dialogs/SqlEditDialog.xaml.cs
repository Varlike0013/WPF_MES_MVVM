using System.Windows;

namespace WPF_MES.Module.SAJET.Views.Dialogs;

public partial class SqlEditDialog : Window
{
    public List<string> QueryTypes { get; } = new()
    {
        "查询", "修改", "删除", "增加", "其他",
    };

    public string QueryName => txtName.Text.Trim();
    public string QueryType => cboType.SelectedItem?.ToString() ?? "查询";
    public int TypeId => Math.Max(0, cboType.SelectedIndex);
    public string Description => txtDesc.Text.Trim();
    public string SqlText => txtSql.Text.Trim();

    public SqlEditDialog(string name, string type, string desc, string sql)
    {
        InitializeComponent();
        DataContext = this;

        txtName.Text = name;
        txtDesc.Text = desc;
        txtSql.Text = sql;

        int idx = QueryTypes.IndexOf(type);
        cboType.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (QueryName.Length == 0 || SqlText.Length == 0)
        {
            MessageBox.Show("名称和 SQL 语句不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}