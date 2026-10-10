namespace WPF_MES.Module.AOI.Models;

/// <summary>表格一行</summary>
public class AoiResultItem
{
    public string LineName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string OutputTime { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public int AbnormalCount { get; set; }

    /// <summary>OK / REPAIR / ERROR，用于行色提示</summary>
    public string StatusKind { get; set; } = string.Empty;
}