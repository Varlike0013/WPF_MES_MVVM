using System.Windows;
using System.Windows.Controls;
using WPF_MES.Contracts;
using WPF_MES.Module.AOI.Services;
using WPF_MES.Module.AOI.Views;

namespace WPF_MES.Module.AOI;

public class AoiModule : IModule, IStandaloneWindowModule
{
    public string ModuleKey => "AOI";
    public string DisplayName => "AOI 自动光学检测";

    public IAuthService CreateAuthService() => new AoiAuthService();

    // 独立窗口模式：不返回 UserControl 主视图
    public UserControl? CreateMainView(string userName) => null;

    // 独立窗口入口
    public Window CreateMainWindow(string userName) => new AoiWindow(userName);

    // 非菜单式模块
    public IEnumerable<ModuleMenuNode> GetMenuItems()
        => Array.Empty<ModuleMenuNode>();
}