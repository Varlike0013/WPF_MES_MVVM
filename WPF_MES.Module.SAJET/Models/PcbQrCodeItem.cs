namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// PCB QR Code 记录
/// </summary>
public class PcbQrCodeItem
{
    public string EcsPartNo { get; set; } = string.Empty;   // ECS零件号
    public string PcbCustPn { get; set; } = string.Empty;   // PCB客户PN
    public string PcbSn { get; set; } = string.Empty;       // PCB SN
    public string StrSmtsn { get; set; } = string.Empty;    // STR SMTSN
    public string PcbQrcode { get; set; } = string.Empty;   // PCB二维码
    public string CreateTime { get; set; } = string.Empty;  // 创建时间
}