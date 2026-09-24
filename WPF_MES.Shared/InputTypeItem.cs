namespace WPF_MES.Shared;

/// <summary>
/// 输入类型下拉框中的一项
/// </summary>
public class InputTypeItem
{
    /// <summary>显示文本（如"序号"、"箱号"）</summary>
    public string Display { get; set; } = string.Empty;

    /// <summary>对应的条件类型</summary>
    public ConditionType Type { get; set; }

    public InputTypeItem(string display, ConditionType type)
    {
        Display = display;
        Type = type;
    }

    public override string ToString() => Display;
}