namespace WPF_MES.Contracts;

public class TravelRecord
{
    public string SerialNumber { get; set; } = string.Empty;
    public string WorkOrder { get; set; } = string.Empty;
    public string PartNo { get; set; } = string.Empty;
    public string PdlineName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string OutProcessTime { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerSN { get; set; } = string.Empty;
    public string QcNo { get; set; } = string.Empty;
    public string ReworkNo { get; set; } = string.Empty;
    public string PanelNo { get; set; } = string.Empty;
}