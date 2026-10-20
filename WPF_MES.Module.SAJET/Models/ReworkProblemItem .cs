using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Module.SAJET.Models;

public partial class ReworkProblemItem : ObservableObject
{
    public int NumberIndex { get; set; }
    public string PdLineName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ReasonQuery { get; set; } = string.Empty;

    public string SubmitterName { get; set; } = string.Empty; // EQ.EMP_NAME 提交人
    public string ConfirmerName { get; set; } = string.Empty; // EC.EMP_NAME 确认人

    public string EmpSfis { get; set; } = string.Empty; // 处理人 (待填)
    public string ReworkNo { get; set; } = string.Empty; // 处理结果 (待填)
    public DateTime? DualTime { get; set; }                 // 处理时间 (待填)

    [ObservableProperty] private bool _isNew = true;
}