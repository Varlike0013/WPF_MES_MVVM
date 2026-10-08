using System.Collections.Generic;
using System.Linq;
using WPF_MES.Contracts;                 // ModuleMenuNode
using WPF_MES_MVVM.Views;                // ShareInfoView

namespace WPF_MES_MVVM.Services;

/// <summary>
/// 主程序级全局菜单注入。
/// 每个模块的菜单在加载完成后都会经过这里，统一追加跨模块功能。
/// </summary>
public static class GlobalMenuProvider
{
    /// <summary>全局菜单默认挂载到的父分组标题</summary>
    private const string DefaultParentTitle = "P13程序执行";

    /// <summary>
    /// 对模块菜单列表做原地追加。
    /// 幂等：重复调用不会重复添加。
    /// </summary>
    public static void AttachTo(List<ModuleMenuNode> menus)
    {
        if (menus == null) return;

        // 父节点任意深度都能定位到
        AddToParent(menus, "P13程序执行", "共享信息中心",
            () => new ShareInfoView());
    }

    /// <summary>
    /// 在指定父分组下追加一个叶子节点。
    /// 父分组不存在时自动创建；同名叶子已存在时跳过。
    /// </summary>
    private static void AddToParent(
        List<ModuleMenuNode> menus,
        string parentTitle,
        string childTitle,
        Func<System.Windows.Controls.UserControl>? viewFactory)
    {
        var parent = FindNode(menus, parentTitle);

        if (parent == null)
        {
            parent = new ModuleMenuNode
            {
                Title = parentTitle,
                Children = { },
            };
            menus.Add(parent);
        }

        if (parent.Children.Any(c => c.Title == childTitle)) return;

        parent.Children.Add(new ModuleMenuNode
        {
            Title = childTitle,
            ViewFactory = viewFactory,
        });
    }
    private static ModuleMenuNode? FindNode(
        IEnumerable<ModuleMenuNode> nodes, string title)
    {
        foreach (var node in nodes)
        {
            if (node.Title == title) return node;

            if (node.Children.Count > 0)
            {
                var found = FindNode(node.Children, title);
                if (found != null) return found;
            }
        }
        return null;
    }
}