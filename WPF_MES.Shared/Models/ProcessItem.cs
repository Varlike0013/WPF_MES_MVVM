using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Shared.Models;

/// <summary>
/// 工序筛选项（三态勾选）
/// </summary>
public partial class ProcessItem : ObservableObject
{
    /// <summary>站位段名称 (SYS_STAGE.STAGE_NAME)</summary>
    public string StageName { get; set; } = string.Empty;

    /// <summary>工序名称 (SYS_PROCESS.PROCESS_NAME)</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>工序 ID (SYS_PROCESS.PROCESS_ID)</summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// 勾选状态：
    /// true  = 必过（Checked）
    /// false = 忽略（Unchecked）
    /// null  = 必不过（Indeterminate）
    /// </summary>
    [ObservableProperty] private bool? _state = false;
}