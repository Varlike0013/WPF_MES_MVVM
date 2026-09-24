using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using WPF_MES.Contracts.Models;
using WPF_MES.Contracts.Models.SnItem;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Module.SAJET.Views;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class ReworkViewModel : ObservableObject
{
    // ============ 条件管理 ============

    private readonly ConditionManager _conditions = new();

    public ObservableCollection<ConditionItem> ConditionItems { get; } = new();
    public ObservableCollection<ReworkSnItem> SnItems { get; } = new();

    // ============ 输入类型 ============

    public List<InputTypeItem> InputTypes { get; } = new()
    {
        new("序号",   ConditionType.SerialNumber),
        new("箱号",   ConditionType.Carton),
        new("重工号", ConditionType.Rework),
        new("工单",   ConditionType.WorkOrder),
        new("抽验号", ConditionType.QcNo),
    };

    [ObservableProperty] private InputTypeItem? _selectedInputType;
    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 重工单号 ============

    [ObservableProperty] private string _reworkNo = string.Empty;

    // ============ 数量 ============

    [ObservableProperty] private int _snCount;

    // ============ 重工流程 ============

    [ObservableProperty] private string _routeName = string.Empty;

    public ObservableCollection<ProcessInfo> ProcessList { get; } = new();
    [ObservableProperty] private ProcessInfo? _selectedProcess;

    // ============ 新工单模式 ============

    [ObservableProperty] private bool _isNewWorkOrder;
    [ObservableProperty] private string _newWorkOrder = string.Empty;
    /// <summary>流程名是否可编辑（新工单模式下禁用）</summary>
    public bool IsRouteEditable => !IsNewWorkOrder;

    // ============ 清除选项 ============

    [ObservableProperty] private bool _clearCustomerSN;
    [ObservableProperty] private bool _clearMac;
    [ObservableProperty] private bool _clearPack;
    [ObservableProperty] private bool _clearQc;
    [ObservableProperty] private bool _clearParts;
    
    public ReworkViewModel()
    {
        SelectedInputType = InputTypes[0];
    }

    // ============ 属性联动 ============

    partial void OnIsNewWorkOrderChanged(bool value)
    {
        // 通知 IsRouteEditable 也变了
        OnPropertyChanged(nameof(IsRouteEditable));

        if (!value)
        {
            NewWorkOrder = string.Empty;
            RouteName = string.Empty;
            ProcessList.Clear();
            SelectedProcess = null;
        }
    }

    // ============ 生成新重工号 ============

    [RelayCommand]
    private void GenerateReworkNo()
    {
        try
        {
            ReworkNo = SajetCommonService.GenerateNewReworkNo();
        }
        catch (Exception ex)
        {
            Logger.Error($"[REWORK] Generate rework no failed: {ex.Message}");
            MessageBox.Show("生成重工号失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
   
    // ============ 添加条件 ============

    [RelayCommand]
    private void AddCondition()
    {
        if (SelectedInputType == null)
        {
            MessageBox.Show("请选择输入类型。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string raw = InputText.Trim();
        if (raw.Length == 0)
        {
            MessageBox.Show("不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 按空格拆分，自动去掉空项
        // "A B C"   → ["A", "B", "C"]
        // "A  B "   → ["A", "B"]        （多个空格 + 末尾空格）
        // "A "      → ["A"]             （末尾空格）
        // " A"      → ["A"]             （前导空格）
        var values = raw.Split(
            new[] { ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries);

        if (values.Length == 0) return;

        // 逐个校验 + 去重 + 添加
        var invalid = new List<string>();
        var duplicated = new List<string>();
        int added = 0;

        foreach (var value in values)
        {
            // 1. 校验存在性
            bool exists;
            try
            {
                exists = SelectedInputType.Type switch
                {
                    ConditionType.SerialNumber => SajetCommonService.ExistsSerialNumber(value),
                    ConditionType.Carton => SajetCommonService.ExistsCarton(value),
                    ConditionType.Rework => SajetCommonService.ExistsRework(value),
                    ConditionType.WorkOrder => SajetCommonService.ExistsWorkOrder(value),
                    ConditionType.QcNo => SajetCommonService.ExistsQcNo(value),
                    _ => false,
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"[REWORK] Check exists failed: {ex.Message}");
                MessageBox.Show("校验失败：" + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!exists)
            {
                invalid.Add(value);
                continue;
            }

            // 2. 去重
            if (_conditions.Exists(SelectedInputType.Type, value))
            {
                duplicated.Add(value);
                continue;
            }

            // 3. 添加
            _conditions.Add(SelectedInputType.Type, value);
            ConditionItems.Add(new ConditionItem(
                SelectedInputType.Type, SelectedInputType.Display, value));
            added++;
        }

        // 4. 提示
        if (invalid.Count > 0)
        {
            MessageBox.Show(
                $"以下值不存在，未添加：\n{string.Join("\n", invalid)}",
                "部分无效", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else if (duplicated.Count > 0 && added == 0)
        {
            // 全部重复
            MessageBox.Show(
                $"以下值已存在：\n{string.Join("\n", duplicated)}",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 5. 清空输入 + 刷新
        InputText = string.Empty;

        if (added > 0)
            UpdateTable();
    }

    // ============ 移除条件 ============

    [RelayCommand]
    private void RemoveCondition(ConditionItem? item)
    {
        if (item == null) return;

        _conditions.Remove(item.Type, item.Value);
        ConditionItems.Remove(item);

        UpdateTable();
    }

    // ============ 清空条件 ============

    [RelayCommand]
    private void ClearConditions()
    {
        _conditions.Clear();
        ConditionItems.Clear();
        SnItems.Clear();
        SnCount = 0;
    }

    // ============ 查询表格 ============

    private void UpdateTable()
    {
        if (_conditions.IsEmpty)
        {
            SnItems.Clear();
            SnCount = 0;
            RouteName = string.Empty;
            ProcessList.Clear();
            SelectedProcess = null;
            return;
        }

        try
        {
            var (where, ps) = ReworkService.BuildWhere(_conditions);
            if (string.IsNullOrEmpty(where))
            {
                SnItems.Clear();
                SnCount = 0;
                return;
            }

            var list = ReworkService.QuerySnItems(where, ps);

            SnItems.Clear();
            foreach (var item in list)
                SnItems.Add(item);

            SnCount = list.Count;

            // 流程唯一性判断
            var uniqueRoutes = list
                .Select(x => x.RouteName)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            if (uniqueRoutes.Count == 1)
            {
                RouteName = uniqueRoutes[0];
                LoadProcessesByRoute(RouteName);
            }
            else if (uniqueRoutes.Count > 1)
            {
                MessageBox.Show("查询结果的流程不唯一，请检查条件。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RouteName = string.Empty;
                ProcessList.Clear();
                SelectedProcess = null;
            }
            else
            {
                RouteName = string.Empty;
                ProcessList.Clear();
                SelectedProcess = null;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[REWORK] Query failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 流程 → 工序 ============

    private void LoadProcessesByRoute(string routeName)
    {
        ProcessList.Clear();
        SelectedProcess = null;

        if (string.IsNullOrWhiteSpace(routeName)) return;

        try
        {
            foreach (var p in SajetCommonService.GetProcessesByRoute(routeName))
                ProcessList.Add(p);

            if (ProcessList.Count == 0)
            {
                MessageBox.Show("未找到该流程的工序步骤。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // 自动选中第一个工序
                SelectedProcess = ProcessList[0];
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[REWORK] Load processes failed: {ex.Message}");
            MessageBox.Show("加载工序失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 手动加载工序 ============

    [RelayCommand]
    private void ReloadProcesses()
    {
        LoadProcessesByRoute(RouteName.Trim());
    }

    // ============ 新工单回车：查流程 ============

    [RelayCommand]
    private void LoadRouteByWorkOrder()
    {
        string wo = NewWorkOrder.Trim();
        if (wo.Length == 0)
        {
            MessageBox.Show("工单号不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string route = SajetCommonService.GetRouteByWorkOrder(wo);
            if (string.IsNullOrEmpty(route))
            {
                MessageBox.Show("未找到该工单或工单未分配流程。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RouteName = string.Empty;
                ProcessList.Clear();
                return;
            }

            RouteName = route;
            LoadProcessesByRoute(route);
        }
        catch (Exception ex)
        {
            Logger.Error($"[REWORK] Load route failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 执行重工 ============

    [RelayCommand]
    private void Execute()
    {
        // 1. 检查条件
        if (_conditions.IsEmpty)
        {
            MessageBox.Show("请先添加查询条件。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 2. 校验流程和工序，并获取 ID
        string route = RouteName.Trim();
        string process = SelectedProcess?.ProcessName ?? string.Empty;

        if (route.Length == 0 || process.Length == 0)
        {
            MessageBox.Show("流程或工序不能为空。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 根据名称查 ID
        int routeId = SajetCommonService.GetRouteIdByName(route);
        int processId = SajetCommonService.GetProcessIdByName(process);

        if (routeId == 0)
        {
            MessageBox.Show($"流程 [{route}] 不存在。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (processId == 0)
        {
            MessageBox.Show($"工序 [{process}] 不存在。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 3. 新工单模式校验
        string? newWo = null;
        if (IsNewWorkOrder)
        {
            newWo = NewWorkOrder.Trim();
            if (newWo.Length == 0)
            {
                MessageBox.Show("请输入工单号。", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        if (string.IsNullOrWhiteSpace(ReworkNo))
        {
            try
            {
                ReworkNo = SajetCommonService.GenerateNewReworkNo();
                Logger.Info($"[REWORK] Auto-generated rework no: {ReworkNo}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[REWORK] Generate rework no failed: {ex.Message}");
                MessageBox.Show("生成重工号失败：" + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        // 4. 确认
        var dialog = new ReworkConfirmDialog(
            newWo,
            ReworkNo,
            SnItems.Count,
            route,
            process,
            ClearCustomerSN,
            ClearPack,
            ClearQc,
            ClearMac,
            ClearParts)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true) return;

        string condition = dialog.Condition;
        string remark = dialog.Remark;

        try
        {
            var (where, ps) = ReworkService.BuildWhere(_conditions);

            int affected = ReworkService.ExecuteRework(
                where, ps,
                routeId, processId,
                newWo,
                ReworkNo,
                CurrentUser.UserNo,
                condition,
                remark,
                ClearCustomerSN,
                ClearMac,
                ClearPack,
                ClearQc,
                ClearParts);

            MessageBox.Show($"执行成功。\n\n共影响 {affected} 条记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"[REWORK] Execute failed: {ex.Message}");
            MessageBox.Show("执行失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 6. 执行后：更新工单数量
        string? syncMessage = null;
        if (newWo != null)
        {
            try
            {
                int newCount = SajetCommonService.SyncInputQty(newWo);
                syncMessage = $"新工单 {newWo} 投入数量已更新为 {newCount}。";
            }
            catch (Exception ex)
            {
                Logger.Error($"[REWORK] Sync input qty failed: {ex.Message}");
                syncMessage = $"⚠ 工单数量更新失败：{ex.Message}";
            }
        }

        // 7. 提示
        string msg = "执行成功。";
        if (syncMessage != null)
            msg += "\n\n" + syncMessage;

        MessageBox.Show(msg, "提示",
            MessageBoxButton.OK, MessageBoxImage.Information);

        // 8. 清空
        ClearConditions();
        ReworkNo = string.Empty;
        RouteName = string.Empty;
        ProcessList.Clear();
        SelectedProcess = null;
        NewWorkOrder = string.Empty;
        ClearCustomerSN = false;
        ClearMac = false;
        ClearPack = false;
        ClearQc = false;
        ClearParts = false;
    }
}