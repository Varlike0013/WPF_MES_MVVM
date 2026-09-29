namespace WPF_MES.Contracts.Models.SnItem;
/// <summary>
/// SN 列表项（工单详情页 DataGrid 用）
/// </summary>
public class SnListItem
{
    public string SerialNumber { get; set; } = string.Empty;
    public string WorkOrder { get; set; } = string.Empty;
    public string PartNo { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string WorkFlag { get; set; } = string.Empty;
    public string CustomerSN { get; set; } = string.Empty;
    public string CartonNo { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string QcNo { get; set; } = string.Empty;
    public string ReworkNo { get; set; } = string.Empty;
    public string UpdateTime { get; set; } = string.Empty;

    // ============ 解析属性 ============

    /// <summary>作业标志码：0=Good, 1=Fail, 2=Hold</summary>
    public int WorkFlagCode
        => int.TryParse(WorkFlag, out var v) ? v : 0;

    /// <summary>作业标志描述</summary>
    public string WorkFlagText => WorkFlagCode switch
    {
        0 => "Good",
        1 => "Repair",
        2 => "Hold",
        _ => WorkFlag,
    };
}
