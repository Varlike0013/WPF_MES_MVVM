using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Shared.Models;

/// <summary>
/// 角色项（带勾选）
/// </summary>
public partial class RoleItem : ObservableObject
{
    /// <summary>角色 ID</summary>
    public int RoleId { get; set; }

    /// <summary>角色名</summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string RoleDesc { get; set; } = string.Empty;

    /// <summary>是否勾选</summary>
    [ObservableProperty] private bool _isChecked;
}
