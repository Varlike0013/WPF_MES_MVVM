using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Printing;
using System.Windows;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class FindRouteViewModel : ObservableObject
{
    // ============ 左列：流程列表 ============

    private List<string> _allRoutes = new();

    public ObservableCollection<string> FilteredRoutes { get; } = new();

    [ObservableProperty] private string? _selectedRoute;
    [ObservableProperty] private string _filterText = string.Empty;

    /// <summary>选中左侧流程 → 右侧显示步骤</summary>
    partial void OnSelectedRouteChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            NewRouteDisplay = string.Empty;
            NewSteps.Clear();
            return;
        }

        LoadNewSteps(value);
    }

    /// <summary>过滤文本变化 → 实时过滤</summary>
    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter(value);
    }

    // ============ 中列：SN 输入 + 表格 ============

    [ObservableProperty] private string _inputSn = string.Empty;

    /// <summary>上表：SN 途经的流程（可勾选）</summary>
    public ObservableCollection<RouteItem> SnTravelRoutes { get; } = new();

    /// <summary>下表：重工流程（查询结果）</summary>
    public ObservableCollection<RouteItem> ReworkRoutes { get; } = new();
    [ObservableProperty] private RouteItem? _selectedReworkRoute;

    // ============ 右二列：旧混合流程 ============

    [ObservableProperty] private string _oldRouteDisplay = "(未选择 DIP/PACK)";

    public ObservableCollection<RouteStepItem> OldSteps { get; } = new();

    /// <summary>当前的 DIP 流程名</summary>
    private string _dip = string.Empty;

    /// <summary>当前的 PACK 流程名</summary>
    private string _pack = string.Empty;

    // ============ 最右列：当前查看流程 ============

    [ObservableProperty] private string _newRouteDisplay = string.Empty;

    public ObservableCollection<RouteStepItem> NewSteps { get; } = new();

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 构造 ============

    public FindRouteViewModel()
    {
        LoadAllRoutes();
    }

    // ============ 加载 ============

    private void LoadAllRoutes()
    {
        try
        {
            _allRoutes = FindRouteService.LoadAllRoutes();
            ApplyFilter(FilterText);
        }
        catch (Exception ex)
        {
            Logger.Error($"[FIND-ROUTE] Load routes failed: {ex.Message}");
            MessageBox.Show("加载流程列表失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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

        if (!string.IsNullOrEmpty(SelectedRoute) &&
            !FilteredRoutes.Contains(SelectedRoute))
        {
            SelectedRoute = null;
        }
    }

    private void LoadNewSteps(string routeName)
    {
        try
        {
            NewSteps.Clear();
            foreach (var step in FindRouteService.GetRouteSteps(routeName))
                NewSteps.Add(step);

            NewRouteDisplay = routeName;
        }
        catch (Exception ex)
        {
            Logger.Error($"[FIND-ROUTE] Load steps failed: {ex.Message}");
            MessageBox.Show("加载流程步骤失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：SN 回车 → 查走过的流程 ============

    [RelayCommand]
    private void SearchSn()
    {
        string sn = InputSn.Trim();
        if (sn.Length == 0)
        {
            MessageBox.Show("请输入序列号。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var list = FindRouteService.GetRoutesBySn(sn);

            SnTravelRoutes.Clear();
            foreach (var item in list)
                SnTravelRoutes.Add(item);

            // 清空重工结果表
            ReworkRoutes.Clear();
            SelectedReworkRoute = null;

            if (list.Count == 0)
            {
                MessageBox.Show($"SN [{sn}] 未找到相关流程记录。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"SN [{sn}]：无记录";
            }
            else
            {
                StatusMessage = $"SN [{sn}]：{list.Count} 个途经流程";
            }

            Logger.Info($"[FIND-ROUTE] SN={sn}, routes={list.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[FIND-ROUTE] Search SN failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：查询重工流程 ============

    [RelayCommand]
    private void SearchRework()
    {
        // 从"途经流程"里收集勾选
        var checkedItems = SnTravelRoutes.Where(x => x.IsChecked).ToList();

        if (checkedItems.Count != 2)
        {
            MessageBox.Show("必须且只能选择两个流程。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string pack = string.Empty;
        string dip = string.Empty;

        foreach (var item in checkedItems)
        {
            if (item.RouteName.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                pack = item.RouteName;
            else
                dip = item.RouteName;
        }

        if (string.IsNullOrEmpty(pack) || string.IsNullOrEmpty(dip))
        {
            MessageBox.Show(
                "未找到以 P 开头的流程，请确保选择了一个 Pack 流程和一个 Dip 流程。",
                "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var matched = FindRouteService.QueryCombinedRoutes(dip, pack);

            // 结果填到"重工流程"表
            ReworkRoutes.Clear();
            SelectedReworkRoute = null;
            int idx = 1;
            foreach (var name in matched)
            {
                ReworkRoutes.Add(new RouteItem
                {
                    RouteId = idx++,
                    RouteName = name,
                    IsChecked = false,
                });
            }

            _dip = dip;
            _pack = pack;

            // 更新右二列"旧混合流程"
            OldRouteDisplay = $"旧组合流程：{dip} + {pack}";
            OldSteps.Clear();

            foreach (var step in FindRouteService.GetRouteSteps(dip))
                OldSteps.Add(step);

            foreach (var step in FindRouteService.GetRouteSteps(pack))
                OldSteps.Add(step);

            StatusMessage = $"找到 {matched.Count} 个重工流程";

            Logger.Info($"[FIND-ROUTE] dip={dip}, pack={pack}, matched={matched.Count}");

            if (matched.Count == 0)
            {
                MessageBox.Show("未找到符合条件的重工流程。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[FIND-ROUTE] Search rework failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 查看流程（选中中列一行 → 显示步骤） ============

    partial void OnSelectedReworkRouteChanged(RouteItem? value)
    {
        if (value == null)
        {
            NewRouteDisplay = string.Empty;
            NewSteps.Clear();
            return;
        }

        LoadNewSteps(value.RouteName);
    }

    // ============ 命令：添加为新流程 ============

    [RelayCommand]
    private void AddAsNewRoute()
    {
        // 校验 dip/pack
        if (string.IsNullOrEmpty(_dip) || string.IsNullOrEmpty(_pack))
        {
            MessageBox.Show("请先查询并选择有效的 DIP 和 PACK 流程。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 弹输入框
        var dialog = new WPF_MES.Shared.Views.InputDialog(
            "输入新流程名称",
            "新流程名称",
            "")
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
            string result = FindRouteService.AddAsNewRoute(_dip, _pack, newRoute, empNo);

            Logger.Info($"[FIND-ROUTE] AddAsNewRoute: dip={_dip}, pack={_pack}, " +
                        $"new={newRoute}, emp={empNo}, result={result}");

            if (string.IsNullOrEmpty(result))
            {
                MessageBox.Show("操作成功。", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(result, "执行结果",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // 刷新流程列表
            LoadAllRoutes();
        }
        catch (Exception ex)
        {
            Logger.Error($"[FIND-ROUTE] AddAsNewRoute failed: {ex.Message}");
            MessageBox.Show("添加失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}