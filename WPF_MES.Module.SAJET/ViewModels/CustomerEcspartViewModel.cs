using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class CustomerEcspartViewModel : ObservableObject
{
    // ============ 输入字段 ============

    [ObservableProperty] private string _inputEcs = string.Empty;
    [ObservableProperty] private string _inputCus = string.Empty;

    // ============ 客户代码下拉 ============

    public ObservableCollection<CusCodeOption> CusCodeOptions { get; } = new();
    [ObservableProperty] private CusCodeOption? _selectedCusCode;

    // ============ 时间 ============

    [ObservableProperty] private bool _useTime;
    [ObservableProperty] private DateTime? _filterTime;

    // ============ 结果表格 ============

    public ObservableCollection<EcsCusPartItem> ResultItems { get; } = new();

    [ObservableProperty] private EcsCusPartItem? _selectedItem;

    /// <summary>选中行 → 回填表单</summary>
    partial void OnSelectedItemChanged(EcsCusPartItem? value)
    {
        if (value == null) return;

        InputEcs = value.EcsPart;
        InputCus = value.CusPart;

        // 客户代码下拉选中
        SelectedCusCode = CusCodeOptions.FirstOrDefault(c => c.Value == value.CusCode);

        // 时间回填
        if (DateTime.TryParse(value.UpdateDate, out var dt))
        {
            FilterTime = dt;
        }
    }

    // ============ 构造 ============

    public CustomerEcspartViewModel()
    {
        LoadCusCodes();
    }

    private void LoadCusCodes()
    {
        try
        {
            CusCodeOptions.Clear();
            foreach (var opt in CustomerEcspartService.LoadCusCodes())
                CusCodeOptions.Add(opt);

            SelectedCusCode = CusCodeOptions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.Error($"[ECS] Load cus codes failed: {ex.Message}");
            MessageBox.Show("加载客户代码失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：查找 ============

    [RelayCommand]
    private void Search()
    {
        try
        {
            var list = CustomerEcspartService.Query(
                InputEcs.Trim(),
                InputCus.Trim(),
                SelectedCusCode?.Value ?? string.Empty,
                UseTime,
                FilterTime);

            ResultItems.Clear();
            foreach (var item in list)
                ResultItems.Add(item);

            Logger.Info($"[ECS] Search: ecs={InputEcs}, cus={InputCus}, " +
                        $"code={SelectedCusCode?.Value}, count={list.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[ECS] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：添加 ============

    [RelayCommand]
    private void Add()
    {
        string ecs = InputEcs.Trim();
        string cus = InputCus.Trim();
        string code = SelectedCusCode?.Value ?? string.Empty;

        if (ecs.Length == 0)
        {
            MessageBox.Show("ECS料号不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cus.Length == 0)
        {
            MessageBox.Show("客户料号不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (code.Length == 0)
        {
            MessageBox.Show("客户代码不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"确定要添加以下记录吗？\n\n" +
            $"ECS料号：{ecs}\n" +
            $"客户料号：{cus}\n" +
            $"客户代码：{code}\n" +
            "更新时间：当前时间",
            "确认添加",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = CustomerEcspartService.Add(ecs, cus, code);

            Logger.Info($"[ECS] Add: ecs={ecs}, cus={cus}, code={code}, affected={affected}");

            MessageBox.Show("记录已添加。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新
            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[ECS] Add failed: {ex.Message}");
            MessageBox.Show("添加失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：删除 ============

    [RelayCommand]
    private void Delete()
    {
        string ecs = InputEcs.Trim();
        string cus = InputCus.Trim();
        string code = SelectedCusCode?.Value ?? string.Empty;

        if (ecs.Length == 0 || cus.Length == 0 || code.Length == 0)
        {
            MessageBox.Show(
                "删除操作需要同时提供 ECS料号、客户料号、客户代码，\n" +
                "以避免误删其他记录。",
                "输入不完整",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"确定要删除以下记录吗？\n\n" +
            $"ECS料号：{ecs}\n" +
            $"客户料号：{cus}\n" +
            $"客户代码：{code}\n\n" +
            "此操作不可撤销！",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = CustomerEcspartService.Delete(ecs, cus, code);

            Logger.Info($"[ECS] Delete: ecs={ecs}, cus={cus}, code={code}, affected={affected}");

            if (affected == 0)
            {
                MessageBox.Show("未找到匹配的记录，删除失败。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"已删除 {affected} 条记录。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新
            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[ECS] Delete failed: {ex.Message}");
            MessageBox.Show("删除失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}