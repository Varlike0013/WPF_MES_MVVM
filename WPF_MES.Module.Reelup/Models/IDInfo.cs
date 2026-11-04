// Models/IDInfo.cs
namespace WPF_MES.Module.ReelUp.Models;

/// <summary>
/// 对应数据库视图 V_WLS_ALLID_INFO / 终端配置
/// </summary>
public class IDInfo
{
    public string Group { get; set; } = string.Empty;   // ASSY, DIP, INPUT, OUTPUT
    public string Line { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string GunType { get; set; } = string.Empty; // M101, M131, PM9300
}