using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using static System.Windows.Forms.AxHost;

namespace WPF_MES.Module.ReelUp.Models;

// 新增：设备节点的三种状态
public enum DeviceNodeState
{
    NotStarted,    // 未启动（红灯）
    Disconnected,  // 已断开/连接中（灰灯）
    Connected      // 正常连接（绿灯）
}

public partial class TreeNode : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    // 绑定的业务数据（比如终端节点绑定的 IDInfo）
    [ObservableProperty]
    private object? _tag;

    // 新增：连接状态。使用 [ObservableProperty] 让它变化时能通知 UI
    [ObservableProperty]
    private DeviceNodeState _state = DeviceNodeState.NotStarted;

    public ObservableCollection<TreeNode> Children { get; } = new();

    // 新增：根据状态自动返回颜色（红、灰、绿）
    public System.Windows.Media.Brush StatusColor => State switch
    {
        DeviceNodeState.Connected => System.Windows.Media.Brushes.LimeGreen,
        DeviceNodeState.Disconnected => System.Windows.Media.Brushes.Gray,
        _ => System.Windows.Media.Brushes.Red // NotStarted
    };

    // 当 State 发生改变时，通知 StatusColor 也更新
    partial void OnStateChanged(DeviceNodeState value)
    {
        OnPropertyChanged(nameof(StatusColor));
    }
}