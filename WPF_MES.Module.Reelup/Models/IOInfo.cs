// Models/IOInfo.cs
namespace WPF_MES.Module.ReelUp.Models;

/// <summary>
/// 投入产出信息
/// </summary>
public class IOInfo
{
    public string Group { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Sn { get; set; } = string.Empty;
}