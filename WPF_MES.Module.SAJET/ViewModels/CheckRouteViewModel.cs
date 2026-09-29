using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;
using WPF_MES.Shared.Views;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class CheckRouteViewModel : ObservableObject
{
    // ============ 流程列表（全部） ============

    private List<string> _allRoutes = new();

    /// <summary>过滤后的流程列表（绑 UI）</summary>
    public ObservableCollection<string> FilteredRoutes { get; } = new();

    [ObservableProperty] private string? _selectedRoute;

    /// <summary>选中流程变化 → 加载步骤</summary>
    partial void OnSelectedRouteChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            RouteSteps.Clear();
            return;
        }

        LoadRouteSteps(value);
    }

    // ============ 搜索框 ============

    [ObservableProperty] private string _filterText = string.Empty;

    /// <summary>过滤文本变化 → 实时过滤流程列表</summary>
    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter(value);
    }

    // ============ 工序列表（三态勾选） ============

    public ObservableCollection<ProcessItem> ProcessItems { get; } = new();

    // ============ 流程步骤 ============

    public ObservableCollection<RouteStepItem> RouteSteps { get; } = new();

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 构造 ============

    public CheckRouteViewModel()
    {
        LoadProcessList();
        LoadAllRoutes();
    }

    // ============ 加载 ============

    private void LoadProcessList()
    {
        try
        {
            ProcessItems.Clear();
            foreach (var item in CheckRouteService.LoadProcessList())
                ProcessItems.Add(item);

            StatusMessage = $"已加载 {ProcessItems.Count} 个工序";
        }
        catch (Exception ex)
        {
            Logger.Error($"[ROUTE] Load process list failed: {ex.Message}");
            MessageBox.Show("加载工序列表失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadAllRoutes()
    {
        try
        {
            _allRoutes = CheckRouteService.LoadAllRoutes();
            ApplyFilter(FilterText);
        }
        catch (Exception ex)
        {
            Logger.Error($"[ROUTE] Load routes failed: {ex.Message}");
            MessageBox.Show("加载流程列表失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadRouteSteps(string routeName)
    {
        try
        {
            RouteSteps.Clear();
            foreach (var step in CheckRouteService.GetRouteSteps(routeName))
                RouteSteps.Add(step);

            StatusMessage = $"流程 [{routeName}]：{RouteSteps.Count} 个步骤";
        }
        catch (Exception ex)
        {
            Logger.Error($"[ROUTE] Load steps failed: {ex.Message}");
            MessageBox.Show("加载流程步骤失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 过滤 ============

    private void ApplyFilter(string filter)
    {
        FilteredRoutes.Clear();

        if (string.IsNullOrWhiteSpace(filter))
        {
            foreach (var r in _allRoutes)
                FilteredRoutes.Add(r);
        }
        else
        {
            foreach (var r in _allRoutes)
            {
                if (r.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    FilteredRoutes.Add(r);
            }
        }

        // 如果选中项被过滤掉了，清空选中
        if (!string.IsNullOrEmpty(SelectedRoute) &&
            !FilteredRoutes.Contains(SelectedRoute))
        {
            SelectedRoute = null;
        }
    }

    // ============ 命令：刷新流程列表 ============

    [RelayCommand]
    private void Refresh()
    {
        FilterText = string.Empty;
        LoadAllRoutes();
        StatusMessage = $"已刷新，共 {_allRoutes.Count} 个流程";
    }

    // ============ 命令：按工序查询流程 ============

    [RelayCommand]
    private void Search()
    {
        // 收集必过 / 必不过
        var mustHave = new List<string>();
        var mustNot = new List<string>();

        foreach (var p in ProcessItems)
        {
            if (p.State == true) mustHave.Add(p.ProcessName);
            else if (p.State == null) mustNot.Add(p.ProcessName);
        }

        if (mustHave.Count == 0 && mustNot.Count == 0)
        {
            MessageBox.Show("请至少选择一个工序（必过或必不过）。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (mustHave.Count == 0)
        {
            MessageBox.Show("请至少选择一个必过工序。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var matched = CheckRouteService.QueryRoutesByProcess(mustHave, mustNot);

            // 更新流程列表（覆盖 _allRoutes）
            _allRoutes = matched;
            FilterText = string.Empty;   // 清空过滤，触发 ApplyFilter
            ApplyFilter(string.Empty);

            // 清空选中 + 步骤
            SelectedRoute = null;
            RouteSteps.Clear();

            if (matched.Count == 0)
            {
                StatusMessage = "未找到符合条件的流程";
                MessageBox.Show("未找到符合条件的流程。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = $"找到 {matched.Count} 个符合条件的流程";
            }

            Logger.Info($"[ROUTE] Search OK: mustHave={mustHave.Count}, " +
                        $"mustNot={mustNot.Count}, matched={matched.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[ROUTE] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：复制流程 ============

    [RelayCommand]
    private void CopyRoute()
    {
        if (string.IsNullOrEmpty(SelectedRoute))
        {
            MessageBox.Show("请先在左侧选择一个流程。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 弹输入框取新流程名
        var dialog = new InputDialog(
            "复制流程",
            "新流程名称",
            SelectedRoute + "_copy")
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true) return;

        string newRoute = dialog.GetValue("value").Trim();
        if (newRoute.Length == 0)
        {
            MessageBox.Show("新流程名称不能为空。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (newRoute == SelectedRoute)
        {
            MessageBox.Show("新流程名称不能与原流程相同。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 询问是否覆盖
        var overwriteResult = MessageBox.Show(
            $"如果新流程 [{newRoute}] 已存在，是否覆盖？\n\n" +
            "是：覆盖已有流程\n" +
            "否：不覆盖（若已存在会失败）",
            "覆盖确认",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (overwriteResult == MessageBoxResult.Cancel) return;

        string overwrite = overwriteResult == MessageBoxResult.Yes ? "Y" : "N";

        // 操作员
        string empNo = CurrentUser.UserNo;
        if (string.IsNullOrEmpty(empNo))
        {
            MessageBox.Show("未获取到当前用户信息。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string result = CheckRouteService.CopyRoute(
                SelectedRoute, newRoute, empNo, overwrite);

            Logger.Info($"[ROUTE] CopyRoute: {SelectedRoute} -> {newRoute}, " +
                        $"overwrite={overwrite}, result={result}");

            if (string.IsNullOrEmpty(result))
            {
                MessageBox.Show("操作未返回结果，请重试。", "未知结果",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 按返回内容判断成功/失败
            if (result.StartsWith("OK", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("成功") ||
                result.Contains("Success"))
            {
                MessageBox.Show(result, "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新流程列表
                LoadAllRoutes();
                StatusMessage = result;
            }
            else
            {
                MessageBox.Show(result, "执行结果",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[ROUTE] CopyRoute failed: {ex.Message}");
            MessageBox.Show("复制流程失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}