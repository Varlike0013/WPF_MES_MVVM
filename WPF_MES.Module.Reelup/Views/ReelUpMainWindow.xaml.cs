using System.ComponentModel;
using System.Windows;
using WPF_MES.Module.ReelUp.Services;

namespace WPF_MES.Module.ReelUp.Views;

public partial class ReelUpMainWindow : Window
{
    private TrayService? _trayService;

    public ReelUpMainWindow(string userName)
    {
        InitializeComponent();
        var role = UserSession.Instance.IsDeveloper ? "Developer" : "Operator";
        Title = $"ReelUp 上料与投入产出 - {userName} ({role})";

        // 初始化托盘
        _trayService = new TrayService(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // 如果用户点击关闭，且不是因为托盘选择“退出”触发的
        if (_trayService != null && !_trayService.IsExitRequested)
        {
            e.Cancel = true;  // 取消关闭
            this.Hide();      // 隐藏窗口
        }
        else
        {
            // 真正退出时，清理后台通讯
            (DataContext as IDisposable)?.Dispose();
        }

        base.OnClosing(e);
    }
}