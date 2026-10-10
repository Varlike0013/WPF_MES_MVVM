namespace WPF_MES.Module.AOI.Models;

public class AoiRecord
{
    public string PartNo { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public int Field1 { get; set; }
    public string Field2 { get; set; } = string.Empty;
    public int Field3 { get; set; }
    public DateTime InputTime { get; set; }
    public DateTime OutputTime { get; set; }
    public string PassStatus { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public int Number { get; set; }
    public string BgaSerial { get; set; } = string.Empty;
    public int AbnormalCount { get; set; }
    public List<PointRecord> Points { get; set; } = new();
}