using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class CheckMacViewModel : ObservableObject
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

    // ============ 顶部输入 ============

    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 结果表格 ============

    public ObservableCollection<MacInfoItem> ResultItems { get; } = new();

    // ============ 构造 ============

    public CheckMacViewModel()
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
                MessageBox.Show($"{SelectedInputType.Display}不存在。", "输入错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[MAC] Check exists failed: {ex.Message}");
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
            var serials = _conditions.GetValues(ConditionType.SerialNumber);
            var reworks = _conditions.GetValues(ConditionType.Rework);

            var list = CheckMacService.QueryMacs(serials, reworks);

            ResultItems.Clear();
            foreach (var item in list)
                ResultItems.Add(item);

            StatusMessage = $"共查询到 {list.Count} 条记录";
        }
        catch (Exception ex)
        {
            Logger.Error($"[MAC] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ResultItems.Clear();
            StatusMessage = "查询失败";
        }
    }
    // ============ 命令：删除查询列表记录（备份 + 删除） ============

    [RelayCommand]
    private void DeleteAll()
    {
        if (_conditions.IsEmpty)
        {
            MessageBox.Show("请先添加查询条件。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var serials = _conditions.GetValues(ConditionType.SerialNumber);
        var reworks = _conditions.GetValues(ConditionType.Rework);

        // 条件描述
        var condDesc = new List<string>();
        if (serials.Count > 0)
            condDesc.Add($"序号：{string.Join(", ", serials)}");
        if (reworks.Count > 0)
            condDesc.Add($"重工号：{string.Join(", ", reworks)}");

        // 确认
        var confirm = MessageBox.Show(
            $"确定要删除符合条件的 MAC 记录吗？\n\n" +
            string.Join("\n", condDesc) + "\n\n" +
            "删除前会自动备份到历史表。\n\n" +
            "注意：只删除在制工序符合条件且状态为F1Test的记录。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        // ============ 记录操作参数 ============
        string op = CurrentUser.UserNo;
        string serialsStr = serials.Count > 0 ? string.Join(",", serials) : "(无)";
        string reworksStr = reworks.Count > 0 ? string.Join(",", reworks) : "(无)";

        Logger.Info($"[MAC] ========== DeleteAll Start ==========");
        Logger.Info($"[MAC] Operator   : {op}");
        Logger.Info($"[MAC] Serials    : {serialsStr}");
        Logger.Info($"[MAC] Reworks    : {reworksStr}");
        Logger.Info($"[MAC] SerialsCnt : {serials.Count}");
        Logger.Info($"[MAC] ReworksCnt : {reworks.Count}");

        try
        {
            int affected = CheckMacService.BackupAndDelete(serials, reworks);

            // ============ 记录成功结果 ============
            if (affected == 0)
            {
                Logger.Warn($"[MAC] DeleteAll Result: 0 records affected (no match)");
                Logger.Info($"[MAC] ========== DeleteAll End ==========");

                MessageBox.Show("没有符合条件的记录被删除。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Logger.Info($"[MAC] DeleteAll Result: {affected} records affected (success)");
            Logger.Info($"[MAC] ========== DeleteAll End ==========");

            MessageBox.Show($"删除成功。\n\n共影响 {affected} 条记录。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 重新查询刷新
            Search();
        }
        catch (Exception ex)
        {
            // ============ 记录失败结果 ============
            Logger.Error($"[MAC] DeleteAll Result: FAILED");
            Logger.Error($"[MAC] Error Message: {ex.Message}");
            Logger.Error($"[MAC] ========== DeleteAll End ==========");

            MessageBox.Show("删除失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}