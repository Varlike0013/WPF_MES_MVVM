using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WPF_MES.Contracts;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class ExportTravelsViewModel : ObservableObject
{
    // ============ 输入字段 ============

    [ObservableProperty] private string _inputWorkOrder = string.Empty;
    [ObservableProperty] private string _inputPartNo = string.Empty;

    // ============ 产线 / 工序 / 机台 ============

    public ObservableCollection<ComboStringItem> LineOptions { get; } = new();
    public ObservableCollection<ComboStringItem> ProcessOptions { get; } = new();
    public ObservableCollection<ComboStringItem> TerminalOptions { get; } = new();

    [ObservableProperty] private ComboStringItem? _selectedLine;
    [ObservableProperty] private ComboStringItem? _selectedProcess;
    [ObservableProperty] private ComboStringItem? _selectedTerminal;

    // ============ 时间范围 ============

    [ObservableProperty] private DateTime _startTime = DateTime.Now.AddDays(-3);
    [ObservableProperty] private DateTime _endTime = DateTime.Now;

    // ============ 结果表格 ============

    public ObservableCollection<TravelRecord> ResultItems { get; } = new();

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 构造 ============

    public ExportTravelsViewModel()
    {
        LoadLines();
    }

    // ============ 联动：产线变化 → 加载工序 ============

    partial void OnSelectedLineChanged(ComboStringItem? value)
    {
        ProcessOptions.Clear();
        SelectedProcess = null;
        TerminalOptions.Clear();
        SelectedTerminal = null;

        if (value == null || value.Value.Length == 0) return;

        try
        {
            foreach (var p in ExportTravelsService.LoadProcessesByLine(value.Value))
                ProcessOptions.Add(p);
        }
        catch (Exception ex)
        {
            Logger.Error($"[TRAVEL] Load processes failed: {ex.Message}");
        }
    }

    // ============ 联动：工序变化 → 加载机台 ============

    partial void OnSelectedProcessChanged(ComboStringItem? value)
    {
        TerminalOptions.Clear();
        SelectedTerminal = null;

        if (value == null || value.Value.Length == 0) return;
        if (SelectedLine == null || SelectedLine.Value.Length == 0) return;

        try
        {
            foreach (var t in ExportTravelsService.LoadTerminals(
                SelectedLine.Value, value.Value))
                TerminalOptions.Add(t);
        }
        catch (Exception ex)
        {
            Logger.Error($"[TRAVEL] Load terminals failed: {ex.Message}");
        }
    }

    // ============ 加载产线 ============

    private void LoadLines()
    {
        try
        {
            LineOptions.Clear();
            foreach (var l in ExportTravelsService.LoadPdLines())
                LineOptions.Add(l);

            SelectedLine = null;
        }
        catch (Exception ex)
        {
            Logger.Error($"[TRAVEL] Load lines failed: {ex.Message}");
            MessageBox.Show("加载产线失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：查询 ============

    [RelayCommand]
    private void Search()
    {
        if (EndTime < StartTime)
        {
            MessageBox.Show("结束时间不能早于起始时间。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var list = ExportTravelsService.Query(
                InputWorkOrder.Trim(),
                InputPartNo.Trim(),
                SelectedLine?.Value ?? string.Empty,
                SelectedProcess?.Value ?? string.Empty,
                SelectedTerminal?.Value ?? string.Empty,
                StartTime,
                EndTime);

            ResultItems.Clear();
            foreach (var item in list)
                ResultItems.Add(item);

            StatusMessage = $"共查询到 {list.Count} 条记录";

            Logger.Info($"[TRAVEL] Search: wo={InputWorkOrder}, part={InputPartNo}, " +
                        $"line={SelectedLine?.Value}, process={SelectedProcess?.Value}, " +
                        $"terminal={SelectedTerminal?.Value}, count={list.Count}");

            if (list.Count == 0)
            {
                MessageBox.Show("未找到符合条件的记录。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[TRAVEL] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：导出 CSV ============

    [RelayCommand]
    private void Export()
    {
        if (ResultItems.Count == 0)
        {
            MessageBox.Show("没有数据可导出。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "导出出行记录",
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"Travel_{DateTime.Now:yyyyMMddHHmmss}.csv",
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            string[] headers =
            {
            "工单", "料号", "序列号", "产线", "工序", "当前状态",
            "产出时间", "终端", "员工", "客户", "客户SN",
            "质检编号", "返工编号", "电压值"
        };

            CsvExporter.Export(
                dialog.FileName,
                headers,
                ResultItems,
                r => new[]
                {
                r.WorkOrder,
                r.PartNo,
                r.SerialNumber,
                r.PdlineName,
                r.ProcessName,
                r.StatusText,          // ← 用 StatusText
                r.OutProcessTime,
                r.TerminalName,
                r.EmpName,
                r.CustomerName,
                r.CustomerSN,
                r.QcNo,
                r.ReworkNo,
                r.PanelNo,
                });

            Logger.Info($"[TRAVEL] Export OK: {dialog.FileName}, {ResultItems.Count} rows");

            var result = MessageBox.Show(
                $"导出成功！\n\n文件：{dialog.FileName}\n\n是否打开文件？",
                "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[TRAVEL] Export failed: {ex.Message}");
            MessageBox.Show("导出失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}