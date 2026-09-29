namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 重工问题追踪记录
/// </summary>
public class ReworkQuestionItem
{
    public int NumberIndex { get; set; }
    public string PdlineName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string QEmp { get; set; } = string.Empty;
    public string CreateDate { get; set; } = string.Empty;
    public string CEmp { get; set; } = string.Empty;
    public string Checked { get; set; } = string.Empty;
    public string EmpSfis { get; set; } = string.Empty;
    public string ReworkNo { get; set; } = string.Empty;
    public string DualTime { get; set; } = string.Empty;
}