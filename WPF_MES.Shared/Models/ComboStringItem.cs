namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 通用字符串下拉项
/// </summary>
public class ComboStringItem
{
    public string Display { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public ComboStringItem() { }

    public ComboStringItem(string display, string value)
    {
        Display = display;
        Value = value;
    }

    public override string ToString() => Display;
}