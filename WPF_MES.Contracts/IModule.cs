using System.Windows.Controls;
using WPF_MES.Contracts;

public interface IModule
{
    string ModuleKey { get; }
    string DisplayName { get; }
    IAuthService CreateAuthService();
    UserControl? CreateMainView(string userName);
    /// <summary>返回菜单树（支持任意层级）</summary>
    IEnumerable<ModuleMenuNode> GetMenuItems();
}