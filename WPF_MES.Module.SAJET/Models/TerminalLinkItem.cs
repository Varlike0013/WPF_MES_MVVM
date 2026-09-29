namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 终端链接行
/// </summary>
public class TerminalLinkItem
{
    public string StageName { get; set; } = string.Empty;
    public string PdlineName { get; set; } = string.Empty;
    public string GatewayDesc { get; set; } = string.Empty;
    public int TerminalId { get; set; }
    public string TerminalName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string Enabled { get; set; } = string.Empty;
}