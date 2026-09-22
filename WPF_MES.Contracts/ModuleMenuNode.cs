using System.Windows.Controls;

namespace WPF_MES.Contracts;

/// <summary>
/// 菜单节点。递归结构，支持任意层级。
/// - 有 Children 的是分组节点
/// - 无 Children 且 ViewFactory 不为空的是叶子节点
/// </summary>
public class ModuleMenuNode
{
    /// <summary>菜单标题</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>叶子节点的视图工厂。分组节点为 null</summary>
    public Func<UserControl>? ViewFactory { get; set; }
    /// <summary>子节点。分组节点用，叶子节点为空</summary>
    public List<ModuleMenuNode> Children { get; set; } = new();
    /// <summary>是否是叶子节点（有 ViewFactory）</summary>
    public bool IsLeaf => ViewFactory != null;
    /// <summary>是否是分组节点（有子节点）</summary>
    public bool IsGroup => Children.Count > 0;
}