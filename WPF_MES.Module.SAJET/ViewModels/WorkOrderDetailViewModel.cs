using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using WPF_MES.Contracts.Models;
using WPF_MES.Contracts.Models.SnItem;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class WorkOrderDetailViewModel : ObservableObject
{
    // ============ 查询模式 ============

    public List<string> QueryModes { get; } = new()
    {
        "当前工单",   // 索引 0：G_SN_STATUS
        "途径工单"    // 索引 1：G_SN_TRAVEL
    };

    [ObservableProperty] private int _selectedQueryMode = 0;

    /// <summary>当前查询的工单号（用于切换模式时重查）</summary>
    private string _currentWorkOrder = string.Empty;

    // ============ 输入 ============

    [ObservableProperty] private string _inputWorkOrder = string.Empty;

    // ============ 工单信息（同原来） ============

    [ObservableProperty] private string _lblWoNo = string.Empty;
    [ObservableProperty] private string _lblPartNo = string.Empty;
    [ObservableProperty] private string _lblPartDesc = string.Empty;
    [ObservableProperty] private string _lblVersion = string.Empty;
    [ObservableProperty] private string _lblStatus = string.Empty;
    [ObservableProperty] private string _lblTargetQty = string.Empty;
    [ObservableProperty] private string _lblInputQty = string.Empty;
    [ObservableProperty] private string _lblOutputQty = string.Empty;
    [ObservableProperty] private string _lblRoute = string.Empty;
    [ObservableProperty] private string _lblStartProcess = string.Empty;
    [ObservableProperty] private string _lblEndProcess = string.Empty;
    [ObservableProperty] private string _lblLine = string.Empty;
    [ObservableProperty] private string _lblSnCount = string.Empty;

    // ============ SN 列表 ============

    public ObservableCollection<SnListItem> SnRecords { get; } = new();

    // ============ 命令 ============

    [RelayCommand]
    private void Search()
    {
        string wo = InputWorkOrder.Trim();
        if (wo.Length == 0)
        {
            MessageBox.Show("请输入工单号。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // 查询工单基本信息
            var list = WorkOrderService.QueryWorkOrders(wo, 0, -1);
            if (list == null || list.Count == 0)
            {
                Logger.Info($"[WO-DETAIL] Not found: {wo}");
                MessageBox.Show($"未找到工单 {wo}。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearAll();
                return;
            }

            var detail = list[0];
            if (list.Count > 1)
            {
                Logger.Warn($"[WO-DETAIL] {wo} matched {list.Count}, using first: {detail.WorkOrderNo}");
            }

            // 记录当前工单号，供切换模式用
            _currentWorkOrder = detail.WorkOrderNo;

            FillLabels(detail);

            // 加载 SN 列表（根据当前模式）
            LoadSnList();

            Logger.Info($"[WO-DETAIL] {detail.WorkOrderNo} loaded, mode={SelectedQueryMode}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO-DETAIL] Failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ClearAll();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputWorkOrder = string.Empty;
        _currentWorkOrder = string.Empty;
        ClearAll();
    }
    [RelayCommand]
    private void Export()
    {
        // 无数据时提示
        if (SnRecords == null || SnRecords.Count == 0)
        {
            MessageBox.Show("没有数据可导出。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 让用户选保存路径
        var dialog = new SaveFileDialog
        {
            Title = "导出 SN 列表",
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"SN_{_currentWorkOrder}_{DateTime.Now:yyyyMMddHHmmss}.csv",
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            string[] headers =
            {
            "序号", "工单", "料号", "工序", "状态",
            "出货序号", "箱号", "流程", "质检编号", "重工编号", "更新时间"
        };

            CsvExporter.Export(
                dialog.FileName,
                headers,
                SnRecords,
                r => new[]
                {
                r.SerialNumber,
                r.WorkOrder,
                r.PartNo,
                r.ProcessName,
                r.WorkFlagText,       // 显示 Good / Repair / Hold
                r.CustomerSN,
                r.CartonNo,
                r.RouteName,
                r.QcNo,
                r.ReworkNo,
                r.UpdateTime,
                });

            Logger.Info($"[WO-DETAIL] Exported {SnRecords.Count} rows to {dialog.FileName}");

            var result = MessageBox.Show(
                $"导出成功！\n\n文件：{dialog.FileName}\n\n是否打开文件？",
                "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                // 用系统默认程序打开
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO-DETAIL] Export failed: {ex.Message}");
            MessageBox.Show("导出失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 模式切换 ============

    partial void OnSelectedQueryModeChanged(int value)
    {
        // 已查询过工单 → 重新加载 SN 列表
        if (!string.IsNullOrEmpty(_currentWorkOrder))
        {
            try
            {
                LoadSnList();
            }
            catch (Exception ex)
            {
                Logger.Error($"[WO-DETAIL] Reload failed: {ex.Message}");
                MessageBox.Show("加载失败：" + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// 按当前模式加载 SN 列表
    /// </summary>
    private void LoadSnList()
    {
        if (string.IsNullOrEmpty(_currentWorkOrder)) return;

        SnRecords.Clear();

        if (SelectedQueryMode == 0)
        {
            // 当前工单：G_SN_STATUS
            int count = WorkOrderDetailService.GetSnCount(_currentWorkOrder);
            LblSnCount = count.ToString();

            foreach (var r in WorkOrderDetailService.GetSnRecords(_currentWorkOrder))
                SnRecords.Add(r);

            Logger.Info($"[WO-DETAIL] Status SN count = {count}");
        }
        else
        {
            // 途径工单：G_SN_TRAVEL
            int count = WorkOrderDetailService.GetTravelSnCount(_currentWorkOrder);
            LblSnCount = count.ToString();

            foreach (var r in WorkOrderDetailService.GetTravelSnRecords(_currentWorkOrder))
                SnRecords.Add(r);

            Logger.Info($"[WO-DETAIL] Travel SN count = {count}");
        }
    }

    // ============ 内部方法 ============

    private void FillLabels(WorkOrder d)
    {
        LblWoNo = d.WorkOrderNo;
        LblPartNo = d.PartNo;
        LblPartDesc = d.PartDesc;
        LblVersion = d.Version;
        LblStatus = d.StatusDesc;
        LblTargetQty = d.TargetQty.ToString("0.####");
        LblInputQty = d.InputQty.ToString("0.####");
        LblOutputQty = d.OutputQty.ToString("0.####");
        LblRoute = d.RouteName;
        LblStartProcess = d.StartProcess;
        LblEndProcess = d.EndProcess;
        LblLine = d.PdlineName;
    }

    private void ClearAll()
    {
        LblWoNo = string.Empty;
        LblPartNo = string.Empty;
        LblPartDesc = string.Empty;
        LblVersion = string.Empty;
        LblStatus = string.Empty;
        LblTargetQty = string.Empty;
        LblInputQty = string.Empty;
        LblOutputQty = string.Empty;
        LblRoute = string.Empty;
        LblStartProcess = string.Empty;
        LblEndProcess = string.Empty;
        LblLine = string.Empty;
        LblSnCount = string.Empty;
        SnRecords.Clear();
    }
}