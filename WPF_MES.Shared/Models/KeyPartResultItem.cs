using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Shared.Models;

/// <summary>
/// 右侧结果表格的行
/// </summary>
public partial class KeyPartResultItem : ObservableObject
{
    public string WorkOrder { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>是否勾选（要删除）</summary>
    [ObservableProperty] private bool _isChecked;
}