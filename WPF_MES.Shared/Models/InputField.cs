namespace WPF_MES.Shared.Models;

/// <summary>
/// 输入对话框中的一个字段
/// </summary>
public class InputField
{
    /// <summary>唯一标识（用于按 Key 取值）</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>显示标签</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>当前值（用户输入或默认值）</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>占位提示（可选）</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>是否只读（可选）</summary>
    public bool IsReadOnly { get; set; } = false;

    public InputField() { }

    public InputField(string key, string label, string defaultValue = "")
    {
        Key = key;
        Label = label;
        Value = defaultValue;
    }

    public InputField(string key, string label, string defaultValue, string placeholder)
    {
        Key = key;
        Label = label;
        Value = defaultValue;
        Placeholder = placeholder;
    }
}