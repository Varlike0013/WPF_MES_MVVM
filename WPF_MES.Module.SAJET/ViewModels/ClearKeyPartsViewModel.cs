using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Printing;
using System.Windows;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class ClearKeyPartsViewModel : ObservableObject
{
    // ============ 条件管理 ============

    private readonly ConditionManager _conditions = new();

    public ObservableCollection<ConditionItem> ConditionItems { get; } = new();

    // ============ 输入类型下拉 ============

    public List<InputTypeItem> InputTypes { get; } = new()
    {
        new("序号",   ConditionType.SerialNumber),
        new("重工号", ConditionType.Rework),
    };

    [ObservableProperty] private InputTypeItem? _selectedInputType;

    // ============ 顶部条件输入 ============

    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 状态提示 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 右侧结果表格 ============

    public ObservableCollection<KeyPartResultItem> ResultItems { get; } = new();

    // ============ 单 SN 详情 ============

    [ObservableProperty] private string _inputSn = string.Empty;

    /// <summary>该 SN 下的工序列表（含其他信息）</summary>
    public ObservableCollection<KeyPartRecord> ProcessList { get; } = new();

    [ObservableProperty] private KeyPartRecord? _selectedProcess;

    [ObservableProperty] private string _editWorkOrder = string.Empty;
    [ObservableProperty] private string _editPartNo = string.Empty;
    [ObservableProperty] private string _editItemPartSn = string.Empty;

    /// <summary>选中工序变化 → 填充只读字段</summary>
    partial void OnSelectedProcessChanged(KeyPartRecord? value)
    {
        if (value == null)
        {
            EditWorkOrder = string.Empty;
            EditPartNo = string.Empty;
            EditItemPartSn = string.Empty;
            return;
        }

        EditWorkOrder = value.WorkOrder;
        EditPartNo = value.PartNo;
        EditItemPartSn = value.ItemPartSn;
    }

    public ClearKeyPartsViewModel()
    {
        SelectedInputType = InputTypes[0];
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
                ConditionType.SerialNumber => SajetCommonService.ExistsSerialNumber(text),
                ConditionType.Rework => SajetCommonService.ExistsRework(text),
                _ => false,
            };

            if (!exists)
            {
                MessageBox.Show($"{SelectedInputType.Display} 不存在。", "输入错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[CLEAR-KP] Check exists failed: {ex.Message}");
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
            var (where, ps) = BuildWhere();
            if (string.IsNullOrEmpty(where))
            {
                ResultItems.Clear();
                StatusMessage = "没有有效的查询条件";
                return;
            }

            var list = ClearKeyPartsService.QueryResultItems(where, ps);

            ResultItems.Clear();
            foreach (var item in list)
                ResultItems.Add(item);

            StatusMessage = $"共查询到 {list.Count} 条工单-工序组合";
        }
        catch (Exception ex)
        {
            Logger.Error($"[CLEAR-KP] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ResultItems.Clear();
            StatusMessage = "查询失败";
        }
    }

    // ============ 命令：加载单 SN 详情 ============

    [RelayCommand]
    private void LoadSnDetail()
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
            var list = ClearKeyPartsService.GetKeyPartRecords(sn);

            ProcessList.Clear();
            SelectedProcess = null;

            foreach (var rec in list)
                ProcessList.Add(rec);

            if (ProcessList.Count == 0)
            {
                MessageBox.Show($"未找到 SN [{sn}] 的关键件记录。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 自动选中第一个
            SelectedProcess = ProcessList[0];
        }
        catch (Exception ex)
        {
            Logger.Error($"[CLEAR-KP] Load sn detail failed: {ex.Message}");
            MessageBox.Show("加载失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：修改关键件 SN ============

    [RelayCommand]
    private void Update()
    {
        if (SelectedProcess == null)
        {
            MessageBox.Show("请先选择一个工序。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string newItemSn = EditItemPartSn.Trim();
        if (newItemSn.Length == 0)
        {
            MessageBox.Show("关键件序列号不能为空。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (newItemSn == SelectedProcess.ItemPartSn)
        {
            MessageBox.Show("内容未修改。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 确认
        var confirm = MessageBox.Show(
            $"确定要修改关键件序列号吗？\n\n" +
            $"序列号：{SelectedProcess.SerialNumber}\n" +
            $"关键件SN：{SelectedProcess.ItemPartSn} → {newItemSn}",
            "确认修改", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = ClearKeyPartsService.UpdateItemPartSn(
                SelectedProcess.SerialNumber,
                SelectedProcess.ItemPartSn,
                newItemSn);

            if (affected == 0)
            {
                MessageBox.Show("未找到匹配记录，更新失败（可能已被他人修改）。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Logger.Info($"[CLEAR-KP] Update OK: {SelectedProcess.SerialNumber}, " +
                        $"{SelectedProcess.ItemPartSn} -> {newItemSn}");

            MessageBox.Show($"修改成功。\n\n影响 {affected} 条记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 更新本地对象 + UI
            SelectedProcess.ItemPartSn = newItemSn;
            EditItemPartSn = newItemSn;

            // 刷新列表（重新从数据库读）
            LoadSnDetail();
        }
        catch (Exception ex)
        {
            Logger.Error($"[CLEAR-KP] Update failed: {ex.Message}");
            MessageBox.Show("修改失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：全部删除（备份 + 删除） ============

    [RelayCommand]
    private void DeleteAll()
    {
        if (_conditions.IsEmpty)
        {
            MessageBox.Show("请先添加查询条件。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 取勾选的行
        var selected = ResultItems.Where(x => x.IsChecked).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("请勾选要删除的行。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 确认
        var confirm = MessageBox.Show(
            $"确定要删除 {selected.Count} 条工单-工序组合下的关键件吗？\n\n" +
            $"删除前会自动备份。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var (where, ps) = BuildWhere();

            int affected = ClearKeyPartsService.BackupAndDelete(where, ps, selected);

            Logger.Info($"[CLEAR-KP] DeleteAll OK: {selected.Count} items, affected={affected}");

            MessageBox.Show($"删除成功。\n\n共影响 {affected} 条记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 重新查询刷新
            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[CLEAR-KP] DeleteAll failed: {ex.Message}");
            MessageBox.Show("删除失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 内部：构建 WHERE ============

    /// <summary>
    /// 用当前条件构建 WHERE 子句。
    /// 只支持 SN 和 Rework 两种条件。
    /// </summary>
    private (string Where, Dictionary<string, object> Ps) BuildWhere()
    {
        var parts = new List<string>();
        var ps = new Dictionary<string, object>();
        int idx = 0;

        foreach (var kv in _conditions.GetAll())
        {
            if (kv.Value.Count == 0) continue;

            string col = kv.Key switch
            {
                ConditionType.SerialNumber => "S.SERIAL_NUMBER",
                ConditionType.Rework => "S.REWORK_NO",
                _ => null!,
            };

            if (col == null) continue;

            var paramNames = new List<string>();
            foreach (var v in kv.Value)
            {
                string p = $"c{idx++}";
                paramNames.Add(":" + p);
                ps[p] = v;
            }

            parts.Add($"{col} IN ({string.Join(",", paramNames)})");
        }

        return (string.Join(" OR ", parts), ps);
    }
}