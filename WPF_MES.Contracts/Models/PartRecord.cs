namespace WPF_MES.Contracts;

public class PartRecord
{
    public string PartNo { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Spec { get; set; } = string.Empty;
    public string ItemPartSn { get; set; } = string.Empty;
    public string PartType { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string UpdateTime { get; set; } = string.Empty;
}