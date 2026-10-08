using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Shared.Models;

/// <summary>
/// 共享信息树节点。同时作为 WPF 绑定对象和 JSON 序列化对象。
/// </summary>
public partial class ShareNode : ObservableObject
{
    [ObservableProperty] private string _id = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string _title = "新节点";
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private string _imageBase64 = string.Empty;
    [ObservableProperty] private string _author = string.Empty;
    [ObservableProperty] private DateTime _updateTime = DateTime.Now;
    [ObservableProperty] private ObservableCollection<ShareNode> _children = new();

    // 运行时状态，不写入 JSON
    [ObservableProperty]
    [property: JsonIgnore]
    private bool _isExpanded;

    [ObservableProperty]
    [property: JsonIgnore]
    private bool _isSelected;
}