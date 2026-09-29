using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Module.SAJET.Views;
using WPF_MES.Module.SAJET.Views.Dialogs;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class PcbQrCodeViewModel : ObservableObject
{
    // ============ 条件管理 ============

    private readonly ConditionManager _conditions = new();

    public ObservableCollection<ConditionItem> ConditionItems { get; } = new();

    // ============ 输入类型下拉 ============

    public List<InputTypeItem> InputTypes { get; } = new()
    {
        new("序号",   ConditionType.SerialNumber),
        new("二维码", ConditionType.QrCode),
        new("重工号", ConditionType.Rework),
    };

    [ObservableProperty] private InputTypeItem? _selectedInputType;
    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 结果表格 ============

    public ObservableCollection<PcbQrCodeItem> ResultItems { get; } = new();

    [ObservableProperty] private PcbQrCodeItem? _selectedItem;

    /// <summary>选中行 → 填充编辑区</summary>
    partial void OnSelectedItemChanged(PcbQrCodeItem? value)
    {
        if (value == null)
        {
            ClearEdit();
            return;
        }

        EditEcsPartNo = value.EcsPartNo;
        EditPcbCustPn = value.PcbCustPn;
        EditPcbSn = value.PcbSn;
        EditStrSmtsn = value.StrSmtsn;
        EditPcbQrcode = value.PcbQrcode;
    }

    // ============ 编辑字段 ============

    [ObservableProperty] private string _editEcsPartNo = string.Empty;
    [ObservableProperty] private string _editPcbCustPn = string.Empty;
    [ObservableProperty] private string _editPcbSn = string.Empty;
    [ObservableProperty] private string _editStrSmtsn = string.Empty;
    [ObservableProperty] private string _editPcbQrcode = string.Empty;

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 构造 ============

    public PcbQrCodeViewModel()
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
                ConditionType.QrCode => true,   // QRCode 不校验
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
            Logger.Error($"[PCB] Check exists failed: {ex.Message}");
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

        _conditions.Add(SelectedInputType.Type, text);
        ConditionItems.Add(new ConditionItem(
            SelectedInputType.Type, SelectedInputType.Display, text));

        InputText = string.Empty;
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
            var serials = _conditions.GetValues(ConditionType.SerialNumber);
            var qrcodes = _conditions.GetValues(ConditionType.QrCode);
            var reworks = _conditions.GetValues(ConditionType.Rework);

            var list = PcbQrCodeService.Query(serials, qrcodes, reworks);

            ResultItems.Clear();
            foreach (var item in list)
                ResultItems.Add(item);

            StatusMessage = $"共查询到 {list.Count} 条记录";
        }
        catch (Exception ex)
        {
            Logger.Error($"[PCB] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ResultItems.Clear();
            StatusMessage = "查询失败";
        }
    }

    // ============ 命令：新增 ============

    [RelayCommand]
    private void Add()
    {
        var dialog = new PcbAddDialog
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true) return;

        // 确认
        var confirm = MessageBox.Show(
            $"确认插入以下记录？\n\n" +
            $"ECS零件号：{dialog.EcsPartNo}\n" +
            $"PCB客户PN：{dialog.PcbCustPn}\n" +
            $"PCB SN：{dialog.PcbSn}\n" +
            $"STR SMTSN：{dialog.StrSmtsn}\n" +
            $"PCB二维码：{dialog.PcbQrcode}\n" +
            $"创建时间：{dialog.CreateTime:yyyy-MM-dd HH:mm:ss}",
            "确认插入",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = PcbQrCodeService.Add(
                dialog.EcsPartNo,
                dialog.PcbCustPn,
                dialog.PcbSn,
                dialog.StrSmtsn,
                dialog.PcbQrcode,
                dialog.CreateTime);

            Logger.Info($"[PCB] Add: sn={dialog.StrSmtsn}, qr={dialog.PcbQrcode}, " +
                        $"affected={affected}");

            MessageBox.Show("记录已添加。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新
            if (!_conditions.IsEmpty)
                Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[PCB] Add failed: {ex.Message}");
            MessageBox.Show("添加失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：修改 ============

    [RelayCommand]
    private void Update()
    {
        string strSmtsn = EditStrSmtsn.Trim();
        string qrcode = EditPcbQrcode.Trim();

        if (strSmtsn.Length == 0 || qrcode.Length == 0)
        {
            MessageBox.Show("STR SMTSN 和 PCB 二维码不能为空。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"确定要更新 SN [{strSmtsn}] 的记录吗？",
            "确认修改",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = PcbQrCodeService.Update(
                EditEcsPartNo.Trim(),
                EditPcbCustPn.Trim(),
                EditPcbSn.Trim(),
                strSmtsn,
                qrcode);

            Logger.Info($"[PCB] Update: sn={strSmtsn}, affected={affected}");

            if (affected == 0)
            {
                MessageBox.Show("未找到匹配的记录，可能已被修改或删除。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"已更新 {affected} 条记录。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[PCB] Update failed: {ex.Message}");
            MessageBox.Show("修改失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：全部删除 ============

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
        var qrcodes = _conditions.GetValues(ConditionType.QrCode);
        var reworks = _conditions.GetValues(ConditionType.Rework);

        var condDesc = new List<string>();
        if (serials.Count > 0)
            condDesc.Add($"序号：{string.Join(", ", serials)}");
        if (qrcodes.Count > 0)
            condDesc.Add($"二维码：{string.Join(", ", qrcodes)}");
        if (reworks.Count > 0)
            condDesc.Add($"重工号：{string.Join(", ", reworks)}");

        var confirm = MessageBox.Show(
            $"确定要删除符合条件的记录吗？\n\n" +
            string.Join("\n", condDesc) + "\n\n" +
            "此操作不可撤销！",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = PcbQrCodeService.Delete(serials, qrcodes, reworks);

            Logger.Info($"[PCB] DeleteAll: affected={affected}");

            if (affected == 0)
            {
                MessageBox.Show("未找到匹配的记录。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"已删除 {affected} 条记录。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[PCB] DeleteAll failed: {ex.Message}");
            MessageBox.Show("删除失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 内部：清空编辑 ============

    private void ClearEdit()
    {
        EditEcsPartNo = string.Empty;
        EditPcbCustPn = string.Empty;
        EditPcbSn = string.Empty;
        EditStrSmtsn = string.Empty;
        EditPcbQrcode = string.Empty;
    }
}