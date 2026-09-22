using System.Windows.Controls;
using WPF_MES.Contracts;
using WPF_MES.Module.GEDTA.Services;

namespace WPF_MES.Module.GEDTA;

public class DtModule : IModule
{
    public string ModuleKey => "GEDTA";
    public string DisplayName => "GEDTA 制造执行系统";

    public IAuthService CreateAuthService () => new DtAuthService();

    public UserControl? CreateMainView(string userName)
    {
        // 暂时用 Shell 通用主界面，返回 null
        return null;
    }

    public IEnumerable<ModuleMenuNode> GetMenuItems()
    {
        return new List<ModuleMenuNode>
        {
            new ModuleMenuNode
            {
                Title = "P01系统基础信息",
                Children =
                {
                    new ModuleMenuNode
                    {
                        Title = "工作站",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "生产看板设定",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "料号设定",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "不良原因",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "途程",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "员工",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "权限",
                        ViewFactory = null,
                    },
                }
            },
            new ModuleMenuNode
            {
                Title = "P02报表管理",
                Children =
                {
                    new ModuleMenuNode
                    {
                        Title = "状态表",
                        ViewFactory = null,
                    },
                }
            },
        };
    }
}