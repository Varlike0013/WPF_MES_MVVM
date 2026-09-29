using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Module.SAJET.Models;

/// <summary>树节点类型</summary>
public enum TreeNodeType
{
    Server,
    Gateway,
    Ip,
}

/// <summary>
/// 服务器/网关/IP 树节点
/// </summary>
public partial class TreeNodeViewModel : ObservableObject
{
    public TreeNodeType Type { get; set; }

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private bool _isExpanded = false;

    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();

    // ============ Server 节点 ============
    public string ServerId { get; set; } = string.Empty;
    public string ServerDesc { get; set; } = string.Empty;

    // ============ Gateway 节点 ============
    public string GatewayId { get; set; } = string.Empty;
    public string GatewayDesc { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public int ConnectNumber { get; set; }

    /// <summary>网关的子节点（IP）是否已加载</summary>
    public bool ChildrenLoaded { get; set; }

    // ============ IP 节点 ============
    public string Ip { get; set; } = string.Empty;
    public int DeviceIndex { get; set; }   // 1-based
}