namespace WPF_MES.Shared.Models;

/// <summary>
/// 关键件记录（单 SN 详情用）
/// </summary>
public class KeyPartRecord
{
    public string WorkOrder { get; set; } = string.Empty;     // 工单
    public string SerialNumber { get; set; } = string.Empty;  // 序列号
    public int ProcessId { get; set; }                        // 工序 ID
    public string ProcessName { get; set; } = string.Empty;   // 工序名
    public string ItemPartSn { get; set; } = string.Empty;    // 关键件 SN
    public int ItemPartId { get; set; }                       // 关键件 ID
    public string PartNo { get; set; } = string.Empty;        // 料号
}