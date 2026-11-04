using System.Windows;
using WPF_MES.Contracts;
using WPF_MES.Module.ReelUp.Services;
using WPF_MES.Module.ReelUp.Views;

namespace WPF_MES.Module.ReelUp;

public class ReelUpModule : IModule, IStandaloneWindowModule
{
    public string ModuleKey => "ReelUp";
    public string DisplayName => "ReelUp 上料与投入产出";

    public IAuthService CreateAuthService() => new ReelUpAuthService();

    public System.Windows.Controls.UserControl? CreateMainView(string userName) => null;

    public Window CreateMainWindow(string userName) => new ReelUpMainWindow(userName);

    public IEnumerable<ModuleMenuNode> GetMenuItems() => Array.Empty<ModuleMenuNode>();
}