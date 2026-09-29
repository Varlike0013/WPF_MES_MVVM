namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 客户代码下拉项
/// </summary>
public class CusCodeOption
{
    public string Display { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public CusCodeOption() { }

    public CusCodeOption(string display, string value)
    {
        Display = display;
        Value = value;
    }

    public override string ToString() => Display;
}