namespace WPF_MES.Contracts.Models;

/// <summary>
/// 工单实体。跨模块共享。
/// </summary>
public class WorkOrder
{
    public string WorkOrderNo { get; set; } = string.Empty;
    public string PartNo { get; set; } = string.Empty;
    public string PartDesc { get; set; } = string.Empty;
    public string WoRule { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public int StatusCode { get; set; }
    public string StatusDesc { get; set; } = string.Empty;
    public decimal TargetQty { get; set; }
    public decimal InputQty { get; set; }
    public decimal OutputQty { get; set; }
    // 流程
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    // 起始工序
    public int StartProcessId { get; set; }
    public string StartProcess { get; set; } = string.Empty;
    // 结束工序
    public int EndProcessId { get; set; }
    public string EndProcess { get; set; } = string.Empty;
    // 预定产线
    public int PdlineId { get; set; }
    public string PdlineName { get; set; } = string.Empty;

    public int SnCount { get; set; }
}