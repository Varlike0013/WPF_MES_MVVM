using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF_MES.Contracts.Models;
using WPF_MES.Shared;
using WPF_MES.Module.SAJET.Services;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class WorkOrderMaintainViewModel : ObservableObject
{
    public WorkOrderMaintainViewModel()
    {
        // 顶部下拉框选项
        WoStatusList = new List<string>
        {
            "初始", "准备", "发布", "进行中", "搁置", "取消", "完成", "全部"
        };
        FilterTypeList = new List<string>
        {
            "工单", "料号", "流程", "线别", "序号"
        };
        StatusList = new List<string>
        {
            "初始", "准备", "发布", "进行中", "搁置", "取消", "完成"
        };

        _selectedWoStatusIndex = 7;   // 默认"全部"
        _selectedFilterTypeIndex = 0; // 默认"工单"

        // 初始化时加载产线
        LoadPDLines();
    }

    // ============ 下拉列表数据源 ============

    public List<string> WoStatusList { get; }
    public List<string> FilterTypeList { get; }
    public List<string> StatusList { get; }

    public ObservableCollection<ComboItem> StartProcessList { get; } = new();
    public ObservableCollection<ComboItem> EndProcessList { get; } = new();
    public ObservableCollection<ComboItem> LineList { get; } = new();

    // ============ 顶部筛选 ============

    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private int _selectedWoStatusIndex;
    [ObservableProperty] private int _selectedFilterTypeIndex;
    [ObservableProperty] private string _resultMessage = string.Empty;

    // ============ 查询结果 ============

    public ObservableCollection<WorkOrder> WorkOrders { get; } = new();

    [ObservableProperty] private WorkOrder? _selectedWorkOrder;

    /// <summary>选中行变化时，把数据填到表单</summary>
    partial void OnSelectedWorkOrderChanged(WorkOrder? value)
    {
        if (value == null)
        {
            ClearForm();
            return;
        }

        WorkOrderNo = value.WorkOrderNo;
        PartNo = value.PartNo;
        RouteId = value.RouteId;
        RouteName = value.RouteName;
        SelectedStatusCode = value.StatusCode;

        // 根据流程重新加载工序，再按 ID 选中
        LoadProcessesByRoute(value.RouteId);
        SelectedStartProcess = StartProcessList.FirstOrDefault(i => i.Id == value.StartProcessId);
        SelectedEndProcess = EndProcessList.FirstOrDefault(i => i.Id == value.EndProcessId);
        SelectedLine = LineList.FirstOrDefault(i => i.Id == value.PdlineId);

        ResultMessage = $"已选择工单：{value.WorkOrderNo}";
    }

    // ============ 表单字段 ============

    [ObservableProperty] private string _workOrderNo = string.Empty;
    [ObservableProperty] private string _partNo = string.Empty;
    [ObservableProperty] private int _selectedStatusCode = -1;

    [ObservableProperty] private int _routeId;
    [ObservableProperty] private string _routeName = string.Empty;

    [ObservableProperty] private ComboItem? _selectedStartProcess;
    [ObservableProperty] private ComboItem? _selectedEndProcess;
    [ObservableProperty] private ComboItem? _selectedLine;

    // ============ 命令 ============

    [RelayCommand]
    private void Search()
    {
        string input = FilterText.Trim();
        if (input.Length == 0)
        {
            MessageBox.Show("请输入查询内容。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var list = WorkOrderService.QueryWorkOrders(
                input, SelectedFilterTypeIndex, SelectedWoStatusIndex);

            WorkOrders.Clear();
            foreach (var wo in list)
                WorkOrders.Add(wo);

            ResultMessage = $"共查询到 {list.Count} 条工单";
            Logger.Info($"[WO] Query input={input}, type={SelectedFilterTypeIndex}, " +
                        $"status={SelectedWoStatusIndex}, count={list.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO] Query failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Update()
    {
        // 校验
        if (string.IsNullOrEmpty(WorkOrderNo))
        {
            MessageBox.Show("请先在右侧选择一个工单。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedStatusCode < 0)
        {
            MessageBox.Show("请选择状态。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedStartProcess == null ||
            SelectedEndProcess == null ||
            SelectedLine == null)
        {
            MessageBox.Show("请完整填写更新信息。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            int affected = WorkOrderService.UpdateWorkOrder(
                WorkOrderNo,
                SelectedStatusCode,
                RouteId,
                SelectedStartProcess.Id,
                SelectedEndProcess.Id,
                SelectedLine.Id);

            if (affected == 0)
            {
                MessageBox.Show($"未找到工单 {WorkOrderNo}，更新失败。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Logger.Info($"[WO] Update OK: {WorkOrderNo}, " +
                        $"status={SelectedStatusCode}, " +
                        $"route={SelectedStartProcess.Name} -> {SelectedEndProcess.Name}, " +
                        $"line={SelectedLine.Name}");

            MessageBox.Show("更新成功。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新当前查询（静默，不需要提示）
            RefreshSilent();
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO] Update failed: {ex.Message}");
            MessageBox.Show("更新失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 内部方法 ============

    /// <summary>
    /// 静默刷新：用当前条件重新查询，不弹提示
    /// </summary>
    private void RefreshSilent()
    {
        string input = FilterText.Trim();
        if (input.Length == 0) return;

        try
        {
            var list = WorkOrderService.QueryWorkOrders(
                input, SelectedFilterTypeIndex, SelectedWoStatusIndex);

            WorkOrders.Clear();
            foreach (var wo in list)
                WorkOrders.Add(wo);

            ResultMessage = $"共查询到 {list.Count} 条工单";
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO] Refresh failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载产线（初始化时一次）
    /// </summary>
    private void LoadPDLines()
    {
        try
        {
            LineList.Clear();
            foreach (var (id, name) in OracleHelper.GetPDLinesLikeBfloor())
            {
                LineList.Add(new ComboItem { Id = id, Name = name });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO] Load PDLines failed: {ex.Message}");
            MessageBox.Show("加载产线失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 根据流程 ID 加载工序列表
    /// </summary>
    private void LoadProcessesByRoute(int routeId)
    {
        StartProcessList.Clear();
        EndProcessList.Clear();
        SelectedStartProcess = null;
        SelectedEndProcess = null;

        if (routeId <= 0) return;

        try
        {
            foreach (var (id, name) in OracleHelper.GetRouteProcesses(routeId))
            {
                StartProcessList.Add(new ComboItem { Id = id, Name = name });
                EndProcessList.Add(new ComboItem { Id = id, Name = name });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO] Load processes failed: {ex.Message}");
            MessageBox.Show("加载工序失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 清空表单
    /// </summary>
    private void ClearForm()
    {
        WorkOrderNo = string.Empty;
        PartNo = string.Empty;
        SelectedStatusCode = -1;
        RouteId = 0;
        RouteName = string.Empty;

        StartProcessList.Clear();
        EndProcessList.Clear();
        SelectedStartProcess = null;
        SelectedEndProcess = null;
        SelectedLine = null;
    }
}