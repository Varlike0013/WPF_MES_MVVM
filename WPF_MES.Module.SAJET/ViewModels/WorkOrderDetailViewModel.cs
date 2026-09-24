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
    /// <summary>原始客户 ID（工单当前客户，用于判断是否有改动）</summary>
    private int _originalCustomerId;
    /// <summary>抑制客户变更事件（程序设置时不触发数据库更新）</summary>
    private bool _suppressCustomerChange = false;

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
    /// <summary>客户列表（下拉框数据源）</summary>
    public ObservableCollection<CustomerInfo> Customers { get; } = new();
    /// <summary>当前选中的客户</summary>
    [ObservableProperty] private CustomerInfo? _selectedCustomer;

    public WorkOrderDetailViewModel()
    {
        // 加载客户列表
        LoadCustomers();
    }
    private void LoadCustomers()
    {
        try
        {
            Customers.Clear();
            foreach (var c in SajetCommonService.GetCustomers())
                Customers.Add(c);
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO-DETAIL] Load customers failed: {ex.Message}");
            MessageBox.Show("加载客户失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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
    partial void OnSelectedCustomerChanged(CustomerInfo? value)
    {
        // 1. 抑制中（程序设置），跳过
        if (_suppressCustomerChange) return;

        // 2. 没有工单，跳过
        if (string.IsNullOrEmpty(_currentWorkOrder)) return;

        // 3. 未选中，跳过
        if (value == null) return;

        // 4. 没变，跳过
        if (value.CustomerId == _originalCustomerId) return;

        // 5. 确认
        var result = MessageBox.Show(
            $"确定要将工单 {_currentWorkOrder} 的客户修改为：\n\n" +
            $"{value.Display}\n\n" +
            $"此操作会修改 工单 和 工单Sn（流程和状态） 的客户。",
            "确认修改",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            // 用户取消 → 延迟恢复原选中
            RestoreOriginalCustomer();
            return;
        }

        // 6. 执行更新
        try
        {
            int affected = WorkOrderDetailService.UpdateCustomer(
                _currentWorkOrder, value.CustomerId);

            Logger.Info($"[WO-DETAIL] Customer updated: {_currentWorkOrder} -> " +
                        $"{value.CustomerCode}, affected={affected}");

            MessageBox.Show($"修改成功。\n\n共影响 {affected} 条记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            _originalCustomerId = value.CustomerId;
            LoadSnList();
        }
        catch (Exception ex)
        {
            Logger.Error($"[WO-DETAIL] Update customer failed: {ex.Message}");
            MessageBox.Show("修改失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);

            // 失败 → 延迟恢复原选中
            RestoreOriginalCustomer();
        }
    }

    /// <summary>
    /// 延迟恢复原选中客户。避开 ComboBox 内部状态处理。
    /// </summary>
    private void RestoreOriginalCustomer()
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(
            new Action(() =>
            {
                SetSelectedCustomer(
                    Customers.FirstOrDefault(c => c.CustomerId == _originalCustomerId));
            }),
            System.Windows.Threading.DispatcherPriority.Background);
    }
    /// <summary>
    /// 程序设置当前选中的客户。
    /// 会抑制 OnSelectedCustomerChanged 的数据库更新逻辑。
    /// 用户操作请直接用 ComboBox 绑定 SelectedCustomer，不要调这个方法。
    /// </summary>
    /// <param name="customer">目标客户。null 表示清空</param>
    private void SetSelectedCustomer(CustomerInfo? customer)
    {
        _suppressCustomerChange = true;
        try
        {
            SelectedCustomer = customer;
        }
        finally
        {
            _suppressCustomerChange = false;
        }
    }
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
        _originalCustomerId = d.CustomerId;
        SetSelectedCustomer(Customers.FirstOrDefault(c => c.CustomerId == d.CustomerId));
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
        _currentWorkOrder = string.Empty;
        _originalCustomerId = 0;
        _suppressCustomerChange = true;
        SelectedCustomer = null;
        _suppressCustomerChange = false;
        SnRecords.Clear();
    }
}