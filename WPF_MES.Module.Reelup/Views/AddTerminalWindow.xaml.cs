using System.Windows;
using WPF_MES.Module.ReelUp.Models;
using WPF_MES.Module.ReelUp.Services;
using MessageBox = System.Windows.MessageBox;

namespace WPF_MES.Module.ReelUp.Views;

public partial class AddTerminalWindow : Window
{
    private readonly ReelUpDao _dao = new();

    public AddTerminalWindow()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var info = new IDInfo
            {
                Group = (CboGroup.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "",
                Line = TxtLine.Text.Trim(),
                Ip = TxtIp.Text.Trim(),
                Id = TxtId.Text.Trim(),
                GunType = (CboGunType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? ""
            };

            if (string.IsNullOrEmpty(info.Group) || string.IsNullOrEmpty(info.Line) || string.IsNullOrEmpty(info.Ip))
            {
                MessageBox.Show("请填写完整信息", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _dao.AddTerminal(info); // 已在 S2 中实现了事务修复
            MessageBox.Show("终端添加成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"添加失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}