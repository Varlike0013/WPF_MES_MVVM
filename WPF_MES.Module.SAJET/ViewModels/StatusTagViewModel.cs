using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using WPF_MES.Contracts;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class StatusTagViewModel : ObservableObject
{
    public StatusTagViewModel()
    {
        InputTypes = new List<string>
        {
            "序号", "料件", "卡号", "SSN", "出货序号", "新序号"
        };
    }

    // ============ 顶部查询 ============

    public List<string> InputTypes { get; }

    [ObservableProperty] private int _selectedInputType = 0;
    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 顶部 12 个标签 ============

    [ObservableProperty] private string _lblSN = string.Empty;
    [ObservableProperty] private string _lblWO = string.Empty;
    [ObservableProperty] private string _lblPartNo = string.Empty;
    [ObservableProperty] private string _lblPartDesc = string.Empty;
    [ObservableProperty] private string _lblNextProcess = string.Empty;
    [ObservableProperty] private string _lblWorkFlag = string.Empty;
    [ObservableProperty] private string _lblCSN = string.Empty;
    [ObservableProperty] private string _lblCarton = string.Empty;
    [ObservableProperty] private string _lblRoute = string.Empty;
    [ObservableProperty] private string _lblMac = string.Empty;
    [ObservableProperty] private string _lblSSN = string.Empty;
    [ObservableProperty] private string _lblPPID = string.Empty;

    // ============ 表格数据 ============

    public ObservableCollection<TravelRecord> TravelRecords { get; } = new();
    public ObservableCollection<PartRecord> PartRecords { get; } = new();
    public ObservableCollection<ReworkRecord> ReworkRecords { get; } = new();

    // ============ 命令 ============

    [RelayCommand]
    private void Search()
    {
        string input = InputText.Trim();
        if (input.Length == 0)
        {
            ClearAll();
            return;
        }

        try
        {
            var serials = StatusTagService.GetSerialNumbers(SelectedInputType, input);
            if (serials.Count == 0)
            {
                Logger.Info($"[STATUS] No match for {input}");
                ClearAll();
                return;
            }

            if (serials.Count > 1)
            {
                var r = MessageBox.Show(
                    $"查询到 {serials.Count} 个匹配的序列号，显示第一个：{serials[0]}\n\n继续？",
                    "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (r != MessageBoxResult.Yes) return;
            }

            string sn = serials[0];
            Logger.Info($"[STATUS] Match: {input} -> {sn}");

            LoadStatusTagInfo(sn);
            LoadTravelRecords(sn);
            LoadPartRecords(sn);
            LoadReworkRecords(sn);
        }
        catch (Exception ex)
        {
            Logger.Error($"[STATUS] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ClearAll();
        }
    }

    [RelayCommand]
    private void Export()
    {
        MessageBox.Show("导出功能尚未实现。", "提示",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ============ 加载方法 ============

    private void LoadStatusTagInfo(string sn)
    {
        var info = StatusTagService.GetStatusTagInfo(sn);
        if (info == null)
        {
            ClearLabels(sn);
            return;
        }

        LblSN = info.SerialNumber;
        LblWO = info.WorkOrder;
        LblPartNo = info.PartNo;
        LblPartDesc = info.PartDesc;
        LblNextProcess = info.NextProcess;
        LblWorkFlag = info.WorkFlagText;
        LblCSN = info.CustomerSN;
        LblCarton = info.CartonNo;
        LblRoute = info.RouteName;
        LblMac = info.Mac;
        LblSSN = info.SSN;
        LblPPID = info.PcbQrCode;
    }

    private void LoadTravelRecords(string sn)
    {
        try
        {
            TravelRecords.Clear();
            foreach (var r in StatusTagService.GetTravelRecords(sn))
                TravelRecords.Add(r);
        }
        catch (Exception ex)
        {
            Logger.Error($"[STATUS] Load travel failed: {ex.Message}");
            MessageBox.Show("加载流程表失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadPartRecords(string sn)
    {
        try
        {
            PartRecords.Clear();
            foreach (var r in StatusTagService.GetPartRecords(sn))
                PartRecords.Add(r);
        }
        catch (Exception ex)
        {
            Logger.Error($"[STATUS] Load parts failed: {ex.Message}");
            MessageBox.Show("加载料件表失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void LoadReworkRecords(string sn)
    {
        try
        {
            ReworkRecords.Clear();
            foreach (var r in StatusTagService.GetReworkRecords(sn))
                ReworkRecords.Add(r);
        }
        catch (Exception ex)
        {
            Logger.Error($"[STATUS] Load rework failed: {ex.Message}");
            MessageBox.Show("加载重工表失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void ClearAll()
    {
        ClearLabels(string.Empty);
        TravelRecords.Clear();
        PartRecords.Clear();
        ReworkRecords.Clear();   // ← 加这行
    }

    private void ClearLabels(string sn)
    {
        LblSN = sn;
        LblWO = string.Empty;
        LblPartNo = string.Empty;
        LblPartDesc = string.Empty;
        LblNextProcess = string.Empty;
        LblWorkFlag = string.Empty;
        LblCSN = string.Empty;
        LblCarton = string.Empty;
        LblRoute = string.Empty;
        LblMac = string.Empty;
        LblSSN = string.Empty;
        LblPPID = string.Empty;
    }
}