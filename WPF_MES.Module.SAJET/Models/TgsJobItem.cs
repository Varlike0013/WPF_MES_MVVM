namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// TGS 行为组里的一个流程项
/// </summary>
public class TgsJobItem
{
    public string JobId { get; set; } = string.Empty;
    public string TypeNameE { get; set; } = string.Empty;
    public string ProcCallName { get; set; } = string.Empty;
    public string SprocName { get; set; } = string.Empty;
}