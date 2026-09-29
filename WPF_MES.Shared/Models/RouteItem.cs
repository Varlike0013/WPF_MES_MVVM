using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Shared.Models;

/// <summary>
/// SN 走过的流程项（带复选框）
/// </summary>
public partial class RouteItem : ObservableObject
{
    /// <summary>路线 ID</summary>
    public int RouteId { get; set; }

    /// <summary>路线名称</summary>
    public string RouteName { get; set; } = string.Empty;

    /// <summary>是否选中</summary>
    [ObservableProperty] private bool _isChecked;
}