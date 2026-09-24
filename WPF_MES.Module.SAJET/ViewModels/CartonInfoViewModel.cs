using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class CartonInfoViewModel : ObservableObject
{
    // ============ 条件管理 ============

    private readonly ConditionManager _conditions = new();

    public ObservableCollection<ConditionItem> ConditionItems { get; } = new();

    // ============ 输入类型下拉 ============

    public List<InputTypeItem> InputTypes { get; } = new()
    {
        new("箱号", ConditionType.Carton),
        new("工单", ConditionType.WorkOrder),
    };

    [ObservableProperty] private InputTypeItem? _selectedInputType;

    // ============ 顶部输入 ============

    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 结果表格 ============

    public ObservableCollection<CartonInfoItem> ResultItems { get; } = new();

    [ObservableProperty] private CartonInfoItem? _selectedResultItem;

    // ============ 编辑字段 ============

    [ObservableProperty] private string _editCartonNo = string.Empty;
    [ObservableProperty] private string _editWorkOrder = string.Empty;
    [ObservableProperty] private string _editPackWorkOrder = string.Empty;
    [ObservableProperty] private int _editQty;
    [ObservableProperty] private CartonStatusOption? _selectedStatus;

    public List<CartonStatusOption> StatusOptions { get; } = new()
    {
        new("打开", "N"),
        new("关闭", "Y"),
        new("缴库", "K"),
    };

    // 编辑前的原始值（用于判断变化）
    private string _oldWorkOrder = string.Empty;
    private string _oldPackWorkOrder = string.Empty;
    private int _oldQty;
    private string _oldStatus = string.Empty;

    // ============ 退出 QC ============

    [ObservableProperty] private string _wqcCartonNo = string.Empty;

    // ============ 构造 ============

    public CartonInfoViewModel()
    {
        SelectedInputType = InputTypes[0];
    }

    // ============ 选中结果行 → 填充编辑 ============

    partial void OnSelectedResultItemChanged(CartonInfoItem? value)
    {
        if (value == null)
        {
            ClearEdit();
            return;
        }

        // 填充编辑字段
        EditCartonNo = value.CartonNo;
        EditWorkOrder = value.WorkOrder;
        EditPackWorkOrder = value.PackWorkOrder;
        EditQty = value.Qty;

        // 按 CloseFlag 找状态下拉项
        SelectedStatus = StatusOptions.FirstOrDefault(s => s.Value == value.CloseFlag);

        // 记录原始值
        _oldWorkOrder = value.WorkOrder;
        _oldPackWorkOrder = value.PackWorkOrder;
        _oldQty = value.Qty;
        _oldStatus = value.CloseFlag;

        StatusMessage = $"选中：{value.CartonNo}";
    }

    // ============ 命令：添加条件 ============

    [RelayCommand]
    private void AddCondition()
    {
        if (SelectedInputType == null)
        {
            MessageBox.Show("请选择输入类型。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string text = InputText.Trim();
        if (text.Length == 0)
        {
            MessageBox.Show("不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 校验存在性
        try
        {
            bool exists = SelectedInputType.Type switch
            {
                ConditionType.Carton => SajetCommonService.ExistsCarton(text),
                ConditionType.WorkOrder => SajetCommonService.ExistsWorkOrder(text),
                _ => false,
            };

            if (!exists)
            {
                MessageBox.Show($"{SelectedInputType.Display}不存在。", "输入错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[CARTON] Check exists failed: {ex.Message}");
            MessageBox.Show("校验失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 去重
        if (_conditions.Exists(SelectedInputType.Type, text))
        {
            MessageBox.Show("该条件已存在。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            InputText = string.Empty;
            return;
        }

        // 添加
        _conditions.Add(SelectedInputType.Type, text);
        ConditionItems.Add(new ConditionItem(
            SelectedInputType.Type, SelectedInputType.Display, text));

        InputText = string.Empty;

        // 自动查询
        Search();
    }

    // ============ 命令：删除条件 ============

    [RelayCommand]
    private void RemoveCondition(ConditionItem? item)
    {
        if (item == null) return;

        _conditions.Remove(item.Type, item.Value);
        ConditionItems.Remove(item);

        Search();
    }

    // ============ 命令：清空条件 ============

    [RelayCommand]
    private void ClearConditions()
    {
        _conditions.Clear();
        ConditionItems.Clear();
        ResultItems.Clear();
        ClearEdit();
        StatusMessage = string.Empty;
    }

    // ============ 命令：查询 ============

    [RelayCommand]
    private void Search()
    {
        if (_conditions.IsEmpty)
        {
            ResultItems.Clear();
            StatusMessage = "请先添加查询条件";
            return;
        }

        try
        {
            var cartons = _conditions.GetValues(ConditionType.Carton);
            var workOrders = _conditions.GetValues(ConditionType.WorkOrder);

            var list = CartonInfoService.QueryCartons(cartons, workOrders);

            ResultItems.Clear();
            foreach (var item in list)
                ResultItems.Add(item);

            StatusMessage = $"共查询到 {list.Count} 条记录";
        }
        catch (Exception ex)
        {
            Logger.Error($"[CARTON] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ResultItems.Clear();
            StatusMessage = "查询失败";
        }
    }

    // ============ 命令：修改选中记录 ============

    [RelayCommand]
    private void Update()
    {
        if (string.IsNullOrEmpty(EditCartonNo))
        {
            MessageBox.Show("请先在表格中选择一条记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(EditWorkOrder))
        {
            MessageBox.Show("工单不能为空。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (EditQty < 0)
        {
            MessageBox.Show("数量必须为非负整数。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedStatus == null)
        {
            MessageBox.Show("请选择状态。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 判断是否有变化
        bool woChanged = EditWorkOrder != _oldWorkOrder;
        bool packWoChanged = EditPackWorkOrder != _oldPackWorkOrder;
        bool qtyChanged = EditQty != _oldQty;
        bool statusChanged = SelectedStatus.Value != _oldStatus;

        if (!woChanged && !packWoChanged && !qtyChanged && !statusChanged)
        {
            MessageBox.Show("没有需要更新的内容。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 组织变更描述
        var changes = new List<string>();
        if (woChanged)
            changes.Add($"工单：{_oldWorkOrder} → {EditWorkOrder}");
        if (packWoChanged)
            changes.Add($"包装工单：{_oldPackWorkOrder} → {EditPackWorkOrder}");
        if (qtyChanged)
            changes.Add($"数量：{_oldQty} → {EditQty}");
        if (statusChanged)
            changes.Add($"状态：{_oldStatus} → {SelectedStatus.Value}");

        // 确认
        var confirm = MessageBox.Show(
            $"箱号：{EditCartonNo}\n\n" +
            string.Join("\n", changes) + "\n\n" +
            "确定要更新吗？",
            "确认更新", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        // 执行
        try
        {
            int affected = CartonInfoService.UpdateCarton(
                EditCartonNo,
                EditWorkOrder,
                EditPackWorkOrder,
                EditQty,
                SelectedStatus.Value,
                _oldWorkOrder,
                _oldPackWorkOrder,
                _oldQty,
                _oldStatus);

            Logger.Info($"[CARTON] Update OK: {EditCartonNo}, affected={affected}");

            MessageBox.Show($"更新成功（共影响 {affected} 条记录）。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新列表
            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[CARTON] Update failed: {ex.Message}");
            MessageBox.Show("更新失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：全部打开 ============

    [RelayCommand]
    private void OpenAll()
    {
        BatchUpdateStatus("N", "打开");
    }

    // ============ 命令：全部出库 ============

    [RelayCommand]
    private void OutAll()
    {
        BatchUpdateStatus("Y", "出库");
    }

    // ============ 命令：箱号退出 QC ============

    [RelayCommand]
    private void Wqc()
    {
        string cartonNo = WqcCartonNo.Trim();
        if (cartonNo.Length == 0)
        {
            MessageBox.Show("请输入箱号。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string result = CartonInfoService.ExitQcCarton(cartonNo);

            Logger.Info($"[CARTON] WQC result: carton={cartonNo}, result={result}");

            if (result == "OK")
            {
                MessageBox.Show($"箱号 {cartonNo} 已退出 QC。", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                WqcCartonNo = string.Empty;

                // 刷新列表
                Search();
            }
            else if (result == "DELETE FAILED")
            {
                MessageBox.Show("退出失败，未影响任何记录。", "操作失败",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (result.StartsWith("NO_DATA_FOUND"))
            {
                MessageBox.Show($"未找到匹配的箱号记录：{cartonNo}。", "未找到数据",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (result.StartsWith("ERROR:"))
            {
                MessageBox.Show(result.Substring(6).Trim(), "执行错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (string.IsNullOrEmpty(result))
            {
                MessageBox.Show("操作未返回结果，请重试。", "未知结果",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(result, "执行结果",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[CARTON] WQC failed: {ex.Message}");
            MessageBox.Show("操作失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 内部：批量更新状态 ============

    private void BatchUpdateStatus(string status, string statusDisplay)
    {
        if (_conditions.IsEmpty)
        {
            MessageBox.Show("请先添加箱号或工单条件。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 条件描述
        var condDesc = new List<string>();
        var cartons = _conditions.GetValues(ConditionType.Carton);
        var workOrders = _conditions.GetValues(ConditionType.WorkOrder);

        if (cartons.Count > 0)
            condDesc.Add($"箱号：{string.Join(", ", cartons)}");
        if (workOrders.Count > 0)
            condDesc.Add($"工单：{string.Join(", ", workOrders)}");

        // 确认
        var confirm = MessageBox.Show(
            $"确定要将以下条件的记录状态更新为「{statusDisplay}」吗？\n\n" +
            string.Join("\n", condDesc) + "\n\n" +
            "此操作将影响所有匹配的记录。",
            $"确认{statusDisplay}",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        // 执行
        try
        {
            int affected = CartonInfoService.BatchUpdateStatus(
                cartons, workOrders, status);

            Logger.Info($"[CARTON] BatchUpdateStatus {statusDisplay}, affected={affected}");

            if (affected == 0)
            {
                MessageBox.Show("没有匹配的记录需要更新。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"已将 {affected} 条记录更新为「{statusDisplay}」。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新
            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[CARTON] BatchUpdateStatus failed: {ex.Message}");
            MessageBox.Show("操作失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 内部：清空编辑 ============

    private void ClearEdit()
    {
        EditCartonNo = string.Empty;
        EditWorkOrder = string.Empty;
        EditPackWorkOrder = string.Empty;
        EditQty = 0;
        SelectedStatus = null;

        _oldWorkOrder = string.Empty;
        _oldPackWorkOrder = string.Empty;
        _oldQty = 0;
        _oldStatus = string.Empty;
    }
}