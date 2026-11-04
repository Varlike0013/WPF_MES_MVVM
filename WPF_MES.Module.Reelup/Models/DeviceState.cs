using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace WPF_MES.Module.ReelUp.Models;

public enum DeviceConnectionState
{
    Offline, Connecting, Online, Reconnecting, Error
}

public class DeviceStatusItem
{
    public string Id { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public string GunType { get; set; } = string.Empty;
    public DeviceConnectionState State { get; set; } = DeviceConnectionState.Offline;

    public string DisplayName => $"{Line} ({Ip}) - {GunType}";

    // 使用完全限定名或确认 using System.Windows.Media;
    public Brush StatusBrush => State switch
    {
        DeviceConnectionState.Online => Brushes.Green,
        DeviceConnectionState.Connecting => Brushes.Orange,
        DeviceConnectionState.Reconnecting => Brushes.Goldenrod,
        DeviceConnectionState.Error => Brushes.Red,
        _ => Brushes.Gray
    };
}