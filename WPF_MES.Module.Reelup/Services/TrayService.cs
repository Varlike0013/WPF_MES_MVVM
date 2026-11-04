using System.Drawing;
using System.Windows;
using Application = System.Windows.Application;
using WinForms = System.Windows.Forms;

namespace WPF_MES.Module.ReelUp.Services;

public class TrayService : IDisposable
{
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly Window _mainWindow;
    private bool _isExitRequested = false;

    public TrayService(Window mainWindow)
    {
        _mainWindow = mainWindow;

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = SystemIcons.Application, // 生产环境可替换为本地 .ico
            Text = "ReelUp 上料与投入产出",
            Visible = true
        };

        // 双击托盘恢复窗口
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        // 右键菜单
        var contextMenu = new WinForms.ContextMenuStrip();
        var showItem = new WinForms.ToolStripMenuItem("显示主界面", null, (s, e) => ShowMainWindow());
        var exitItem = new WinForms.ToolStripMenuItem("退出系统", null, (s, e) => ExitApplication());
        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void RequestExit()
    {
        _isExitRequested = true;
    }

    public bool IsExitRequested => _isExitRequested;

    private void ExitApplication()
    {
        _isExitRequested = true;
        _notifyIcon.Visible = false;
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}