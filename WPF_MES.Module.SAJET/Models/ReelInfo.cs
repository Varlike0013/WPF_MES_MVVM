namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// SMT 料盘上料信息（对应 TBL_SMT_REELUPINFO@smt）
/// </summary>
public class ReelInfo
{
    public int NumIndex { get; set; }
    public string Site { get; set; } = string.Empty;
    public string LineId { get; set; } = string.Empty;
    public string ReelUpSn { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public int? RemainQty { get; set; }
    public DateTime? LoadReelDate { get; set; }
    public DateTime? OverDate { get; set; }
    public string OldReelUpSn { get; set; } = string.Empty;
    public int? Qty { get; set; }
    public string Active { get; set; } = string.Empty;
}