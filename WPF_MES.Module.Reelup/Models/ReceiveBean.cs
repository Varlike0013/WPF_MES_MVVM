// Models/ReceiveBean.cs
namespace WPF_MES.Module.ReelUp.Models;

/// <summary>
/// 通讯接收记录，用于 UI 展示（对应 Java ReceiveBean）
/// </summary>
public class ReceiveBean
{
    public string StrData { get; set; } = string.Empty;
    public string StrId { get; set; } = string.Empty;
    public string StrIp { get; set; } = string.Empty;
    public string StrLine { get; set; } = string.Empty;
    public string StrTime { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
}