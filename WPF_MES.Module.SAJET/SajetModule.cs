using System.Windows.Controls;
using WPF_MES.Contracts;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Module.SAJET.Views;

namespace WPF_MES.Module.SAJET;

public class SajetModule : IModule
{
    public string ModuleKey => "SAJET";
    public string DisplayName => "SAJET 制造执行系统";

    public IAuthService CreateAuthService() => new SajetAuthService();

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
                        ViewFactory = () => new StatusTagView(),
                    },
                }
            },
            new ModuleMenuNode
            {
                Title = "P03工单管理",
                Children =
                {
                    new ModuleMenuNode
                    {
                        Title = "工单维护",
                        ViewFactory = () => new WorkOrderMaintainView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "工单详情",
                        ViewFactory = () => new WorkOrderDetailView(),
                    }
                }
            },
            new ModuleMenuNode
            {
                Title = "P05重工管理",
                Children =
                {
                    new ModuleMenuNode
                    {
                        Title = "重工执行",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "重工还原",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "清除料件",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "PCB二维码管理",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "清除卡号",
                        ViewFactory = null,
                    }
                }
            },
            new ModuleMenuNode
            {
                Title = "P13程序执行",
                Children =
                {
                    new ModuleMenuNode
                    {
                        Title = "TCP网络通信",
                        ViewFactory = () => new NetworkChatView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "程序2",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "程序3",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "程序4",
                        ViewFactory = null,
                    },
                    new ModuleMenuNode
                    {
                        Title = "程序5",
                        ViewFactory = null,
                    }
                }
            }
        };
    }
}