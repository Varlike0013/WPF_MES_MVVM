namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 状态下拉项
/// </summary>
public class CartonStatusOption
{
    /// <summary>显示文本：打开/关闭/缴库</summary>
    public string Display { get; set; } = string.Empty;

    /// <summary>实际值：N/Y/K</summary>
    public string Value { get; set; } = string.Empty;

    public CartonStatusOption() { }

    public CartonStatusOption(string display, string value)
    {
        Display = display;
        Value = value;
    }

    public override string ToString() => Display;
}