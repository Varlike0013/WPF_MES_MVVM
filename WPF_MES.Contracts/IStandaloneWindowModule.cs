using System.Windows;

namespace WPF_MES.Contracts;

/// <summary>
/// 独立窗口模块。实现了该接口的模块在主程序登录后
/// 会以独立 Window 打开，不进入 Shell 主界面。
/// </summary>
public interface IStandaloneWindowModule
{
    /// <summary>
    /// 创建模块的主窗口。
    /// </summary>
    Window CreateMainWindow(string userName);
}