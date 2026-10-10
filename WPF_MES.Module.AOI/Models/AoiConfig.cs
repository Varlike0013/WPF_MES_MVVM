namespace WPF_MES.Module.AOI.Models;

public class AoiConfig
{
    public string DirPath { get; set; } = string.Empty;
    public string BackupDir { get; set; } = string.Empty;
    public string ApiCheck { get; set; } = "SJ_CHK_SVI_VAR";
    public string ApiTravel { get; set; } = "SJ_SVI_TRANSFER_VAR";
    public string Type { get; set; } = "TXT_BGA";
    public string Line { get; set; } = string.Empty;
    public int LineId { get; set; }
    public string Terminal { get; set; } = string.Empty;
    public int TerminalId { get; set; }
}