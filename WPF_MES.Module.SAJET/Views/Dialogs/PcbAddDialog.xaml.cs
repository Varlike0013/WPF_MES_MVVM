using System;
using System.Windows;
using System.Windows.Media;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Views.Dialogs;

public partial class PcbAddDialog : Window
{
    /// <summary>最终确定的创建时间</summary>
    public DateTime CreateTime { get; private set; }

    // 输出字段
    public string EcsPartNo => txtEcsPartNo.Text.Trim();
    public string PcbCustPn => txtPcbCustPn.Text.Trim();
    public string PcbSn => txtPcbSn.Text.Trim();
    public string StrSmtsn => txtStrSmtsn.Text.Trim();
    public string PcbQrcode => txtPcbQrcode.Text.Trim();

    public PcbAddDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// STR SMTSN 变化 → 查创建时间
    /// </summary>
    private void TxtStrSmtsn_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        string sn = txtStrSmtsn.Text.Trim();

        if (sn.Length == 0)
        {
            lblCreateTime.Text = "等待输入序列号...";
            lblCreateTime.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));
            return;
        }

        try
        {
            var dt = PcbQrCodeService.GetCreateTimeBySn(sn);

            if (dt.HasValue)
            {
                lblCreateTime.Text = dt.Value.ToString("yyyy-MM-dd HH:mm:ss");
                lblCreateTime.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));   // 绿
            }
            else
            {
                lblCreateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                lblCreateTime.Foreground = new SolidColorBrush(Color.FromRgb(0xD9, 0x53, 0x4F));   // 红
            }
        }
        catch (Exception ex)
        {
            lblCreateTime.Text = "查询失败：" + ex.Message;
            lblCreateTime.Foreground = new SolidColorBrush(Color.FromRgb(0xD9, 0x53, 0x4F));
        }
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        // 校验
        if (string.IsNullOrEmpty(StrSmtsn) && string.IsNullOrEmpty(PcbQrcode))
        {
            MessageBox.Show("SN和二维码不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 确认创建时间
        string sn = StrSmtsn;
        if (sn.Length > 0)
        {
            try
            {
                var dt = PcbQrCodeService.GetCreateTimeBySn(sn);

                if (!dt.HasValue)
                {
                    MessageBox.Show("创建时间无效，请检查序列号。", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CreateTime = dt.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show("查询创建时间失败：" + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        else
        {
            // 只有二维码时，用当前时间
            CreateTime = DateTime.Now;
        }

        DialogResult = true;
        Close();
    }
}