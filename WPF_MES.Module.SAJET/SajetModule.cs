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
                        ViewFactory = () => new CheckRouteView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "员工",
                        ViewFactory = () => new CheckEmpView(),
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
                    new ModuleMenuNode
                    {
                        Title = "过站流程表",
                        ViewFactory = () => new ExportTravelsView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "问题处理",
                        ViewFactory = () => new ReworkQuestionView(),
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
                Title = "P04料件管理",
                Children =
                {
                    new ModuleMenuNode
                    {
                        Title = "客户料号映射",
                        ViewFactory = () => new CustomerEcspartView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "ERP物料维护",
                        ViewFactory = () => new MaterialErpView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "SMT料盘查询",
                        ViewFactory = () => new SmtReelInfoView(),
                    },
                }
            },
            new ModuleMenuNode
            {
                Title = "P05重工作业",
                Children =
                {
                    new ModuleMenuNode
                    {
                        Title = "重工执行",
                        ViewFactory = () => new ReworkView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "重工还原",
                        ViewFactory = () => new SnRecoveryView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "重工流程",
                        ViewFactory = () => new FindRouteView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "清除料件",
                        ViewFactory = () => new ClearKeyPartsView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "PCB二维码管理",
                        ViewFactory = () => new PcbQrCodeView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "清除卡号",
                        ViewFactory = () => new CheckMacView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "箱号管理",
                        ViewFactory = () => new CartonInfoView(),
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
                        Title = "服务器IP",
                        ViewFactory = () => new ServerIpView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "TGS行为查看",
                        ViewFactory = () => new TgsGroupView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "TGS行为测试",
                        ViewFactory = () => new TgsGroupTestView(),
                    },
                    new ModuleMenuNode
                    {
                        Title = "SQL封装",
                        ViewFactory = () => new QueryFormView(),
                    }
                }
            }
        };
    }
}