using System.Collections.ObjectModel;

namespace WPF_MES.Shared.Models;

/// <summary>
/// 共享信息文档，是整个 JSON 文件的根。
/// </summary>
public class ShareTreeDocument
{
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = "团队共享信息树";
    public DateTime UpdateTime { get; set; } = DateTime.Now;
    public ObservableCollection<ShareNode> RootNodes { get; set; } = new();
}
