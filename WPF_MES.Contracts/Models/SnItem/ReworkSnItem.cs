using System.Data;

namespace WPF_MES.Contracts.Models.SnItem;

/// <summary>
/// 重工页 SN 列表项
/// </summary>
public class ReworkSnItem
{
    // ============ 基本 ============
    public string SerialNumber { get; set; } = string.Empty;
    public string WorkOrder { get; set; } = string.Empty;
    public string PartNo { get; set; } = string.Empty;

    // ============ 工序 ============
    /// <summary>产线名</summary>
    public string PdlineName { get; set; } = string.Empty;

    /// <summary>在制工序 (WIP_PROCESS)</summary>
    public string WipProcess { get; set; } = string.Empty;

    /// <summary>当前工序 (PROCESS_ID)</summary>
    public string CurrentProcess { get; set; } = string.Empty;

    /// <summary>终端名</summary>
    public string TerminalName { get; set; } = string.Empty;

    // ============ 包装 ============
    public string CustomerSN { get; set; } = string.Empty;
    public string PalletNo { get; set; } = string.Empty;
    public string CartonNo { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;

    // ============ 其他 ============
    public string OutPdlineTime { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string ReworkNo { get; set; } = string.Empty;
}