using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Shared;

/// <summary>
/// 条件列表中的一行（重工页 DataGrid 用）
/// </summary>
public partial class ConditionItem : ObservableObject
{
    /// <summary>条件类型（内部用，构建 SQL 时用）</summary>
    [ObservableProperty]
    private ConditionType _type;

    /// <summary>条件类型显示名（如"序号"、"工单"）</summary>
    [ObservableProperty]
    private string _typeName = string.Empty;

    /// <summary>条件值（如 SN001、WO001）</summary>
    [ObservableProperty]
    private string _value = string.Empty;

    public ConditionItem(ConditionType type, string typeName, string value)
    {
        _type = type;
        _typeName = typeName;
        _value = value;
    }
}