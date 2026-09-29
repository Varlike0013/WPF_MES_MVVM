namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 终端下拉项
/// </summary>
public class TgsTerminalItem
{
    public string TerminalId { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public string PdlineId { get; set; } = string.Empty;
    public string ProcessId { get; set; } = string.Empty;
    public string StageId { get; set; } = string.Empty;

    public override string ToString() => TerminalName;
}