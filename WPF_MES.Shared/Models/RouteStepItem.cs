namespace WPF_MES.Shared.Models;

/// <summary>
/// 流程步骤项
/// </summary>
public class RouteStepItem
{
    /// <summary>工序名称</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>是否必要：Y / N</summary>
    public string Necessary { get; set; } = string.Empty;

    /// <summary>显示文本（用于列表右侧标签）</summary>
    public string NecessaryDisplay => Necessary switch
    {
        "Y" => "(必过)",
        "N" => "(必不过)",
        _ => string.Empty,
    };
}