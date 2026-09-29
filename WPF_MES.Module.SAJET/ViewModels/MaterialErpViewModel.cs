using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class MaterialErpViewModel : ObservableObject
{
    // ============ 查询 ============

    [ObservableProperty] private string _searchText = string.Empty;

    // ============ 结果表格 ============

    public ObservableCollection<ErpMaterialItem> ResultItems { get; } = new();

    [ObservableProperty] private ErpMaterialItem? _selectedItem;

    /// <summary>选中行 → 填充右侧编辑区</summary>
    partial void OnSelectedItemChanged(ErpMaterialItem? value)
    {
        if (value == null)
        {
            ClearEdit();
            return;
        }

        EditWorkOrder = value.WorkOrder;
        EditEcsPn = value.EcsPn;
        EditKpartsNo = value.KpartsNo;

        // 类型下拉选中
        SelectedType = value.EcsPnDesc;
    }

    // ============ 编辑字段 ============

    [ObservableProperty] private string _editWorkOrder = string.Empty;
    [ObservableProperty] private string _editEcsPn = string.Empty;
    [ObservableProperty] private string _editKpartsNo = string.Empty;

    public List<string> TypeOptions { get; } = new()
    {
        "FAN SET",
        "HEAT PIPE",
        "PLATE",
    };

    [ObservableProperty] private string? _selectedType;

    // ============ 命令：查询 ============

    [RelayCommand]
    private void Search()
    {
        string wo = SearchText.Trim();
        if (wo.Length == 0)
        {
            MessageBox.Show("请输入工单号或者料号。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var list = MaterialErpService.Query(wo);

            ResultItems.Clear();
            foreach (var item in list)
                ResultItems.Add(item);

            Logger.Info($"[ERP] Search: wo={wo}, count={list.Count}");

            if (list.Count == 0)
            {
                MessageBox.Show("未找到该物料信息。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[ERP] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：添加 ============

    [RelayCommand]
    private void Add()
    {
        // 校验
        string wo = EditWorkOrder.Trim();
        string ecsPn = EditEcsPn.Trim();
        string desc = SelectedType ?? string.Empty;
        string kpart = EditKpartsNo.Trim();

        if (wo.Length == 0 || desc.Length == 0 || kpart.Length == 0)
        {
            MessageBox.Show("工单/料号、描述、关键件号均不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 确认
        var confirm = MessageBox.Show(
            $"确定要添加以下物料信息吗？\n\n" +
            $"工单：{wo}\n" +
            $"ECS料号：{ecsPn}\n" +
            $"类型：{desc}\n" +
            $"料件号：{kpart}",
            "确认添加",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            string result = MaterialErpService.Add(wo, ecsPn, desc, kpart);

            Logger.Info($"[ERP] Add: wo={wo}, ecs={ecsPn}, desc={desc}, " +
                        $"kpart={kpart}, result={result}");

            if (result == "OK")
            {
                MessageBox.Show("物料信息已添加。", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // 清空编辑区
                ClearEdit();

                // 刷新
                string currentSearch = SearchText.Trim();
                if (currentSearch.Length > 0)
                    Search();
            }
            else
            {
                MessageBox.Show($"添加失败：{result}", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[ERP] Add failed: {ex.Message}");
            MessageBox.Show("添加失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：删除 ============

    [RelayCommand]
    private void Delete()
    {
        if (SelectedItem == null)
        {
            MessageBox.Show("请先选择要删除的记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string wo = SelectedItem.WorkOrder;
        string ecsPn = SelectedItem.EcsPn;
        string kpart = SelectedItem.KpartsNo;

        if (wo.Length == 0)
        {
            MessageBox.Show("工单号为空，无法删除。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 确认
        var confirm = MessageBox.Show(
            $"确定要删除工单 {wo} 的物料记录吗？\n\n" +
            $"ECS料号：{ecsPn}\n" +
            $"料件号：{kpart}",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = MaterialErpService.Delete(wo, ecsPn, kpart);

            Logger.Info($"[ERP] Delete: wo={wo}, ecs={ecsPn}, kpart={kpart}, " +
                        $"affected={affected}");

            if (affected == 0)
            {
                MessageBox.Show("未找到匹配的记录，可能已被删除。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"已删除 {affected} 条记录。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 清空编辑区
            ClearEdit();

            // 刷新
            if (!string.IsNullOrEmpty(SearchText))
                Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[ERP] Delete failed: {ex.Message}");
            MessageBox.Show("删除失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 内部：清空编辑 ============

    private void ClearEdit()
    {
        EditWorkOrder = string.Empty;
        EditEcsPn = string.Empty;
        EditKpartsNo = string.Empty;
        SelectedType = null;
    }
}