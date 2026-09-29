namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// MAC 信息行
/// </summary>
public class MacInfoItem
{
    public string WorkOrder { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Mac { get; set; } = string.Empty;
    public string CurrentProcess { get; set; } = string.Empty;
    public string UpdateEmp { get; set; } = string.Empty;
    public string UpdateTime { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public string CustomerSN { get; set; } = string.Empty;
}