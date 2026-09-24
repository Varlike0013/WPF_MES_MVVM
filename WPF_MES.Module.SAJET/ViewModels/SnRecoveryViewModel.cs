using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using WPF_MES.Contracts.Models.SnItem;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class SnRecoveryViewModel : ObservableObject
{
    // ============ 输入 ============

    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 左侧：当前状态 ============

    public ObservableCollection<ReworkSnItem> CurrentItems { get; } = new();

    [ObservableProperty] private ReworkSnItem? _selectedCurrentItem;

    // ============ 右侧：重工前状态 ============

    public ObservableCollection<ReworkSnItem> BackupItems { get; } = new();

    [ObservableProperty] private ReworkSnItem? _selectedBackupItem;

    // ============ 恢复按钮 ============

    [ObservableProperty] private bool _canRecover;
    /// <summary>当前是否重工号模式（输入 RWV 开头）</summary>
    [ObservableProperty] private bool _isReworkNoMode;

    /// <summary>恢复全部按钮是否可用</summary>
    public bool CanRecoverAll => IsReworkNoMode && CurrentItems.Count > 0;

    // ============ 选中联动 ============
    /// <summary>IsReworkNoMode 变化时通知 CanRecoverAll</summary>
    partial void OnIsReworkNoModeChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRecoverAll));
    }
    partial void OnSelectedCurrentItemChanged(ReworkSnItem? value)
    {
        if (value == null) return;

        // 清空右侧选中
        _selectedBackupItem = null;
        OnPropertyChanged(nameof(SelectedBackupItem));

        CanRecover = true;
        StatusMessage = $"选中：{value.SerialNumber}";
    }

    partial void OnSelectedBackupItemChanged(ReworkSnItem? value)
    {
        if (value == null) return;

        _selectedCurrentItem = null;
        OnPropertyChanged(nameof(SelectedCurrentItem));

        CanRecover = true;
        StatusMessage = $"选中：{value.SerialNumber}";
    }

    // ============ 命令 ============

    [RelayCommand]
    private void Search()
    {
        string input = InputText.Trim();
        if (input.Length == 0)
        {
            MessageBox.Show("请输入 SN 或重工号。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 清空
        CurrentItems.Clear();
        BackupItems.Clear();
        CanRecover = false;
        SelectedCurrentItem = null;
        SelectedBackupItem = null;

        try
        {
            if (input.StartsWith("RW", StringComparison.OrdinalIgnoreCase))
            {
                IsReworkNoMode = true;
                LoadByReworkNo(input);
            }
            else
            {
                IsReworkNoMode = false;
                LoadBySn(input);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[RECOVERY] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "查询失败";
        }
        finally
        {
            // 通知 CanRecoverAll 重新计算
            OnPropertyChanged(nameof(CanRecoverAll));
        }
    }

    [RelayCommand]
    private void Recover()
    {
        string? sn = SelectedCurrentItem?.SerialNumber
                  ?? SelectedBackupItem?.SerialNumber;

        if (string.IsNullOrEmpty(sn))
        {
            MessageBox.Show("请先在左侧或右侧选择一个 SN。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"确定要将 SN [{sn}] 恢复到重工前状态吗？\n\n" +
            $"此操作会用 备份状态 覆盖 当前状态。",
            "确认恢复", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = SnRecoveryService.RecoverFromLatestBackup(sn);

            if (affected == 0)
            {
                MessageBox.Show($"恢复失败：SN [{sn}] 没有备份记录或当前状态不存在。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Logger.Info($"[RECOVERY] Recovered {sn}, affected={affected}");

            MessageBox.Show($"SN [{sn}] 恢复成功。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 重新查询刷新界面
            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[RECOVERY] Recover failed: {ex.Message}");
            MessageBox.Show("恢复失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    [RelayCommand]
    private void RecoverAll()
    {
        if (!IsReworkNoMode)
        {
            MessageBox.Show("请先输入重工号查询。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string reworkNo = InputText.Trim();
        if (reworkNo.Length == 0 || CurrentItems.Count == 0)
        {
            MessageBox.Show("没有可恢复的数据。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 确认
        var confirm = MessageBox.Show(
            $"确定要恢复重工号 [{reworkNo}] 下的所有 SN 吗？\n\n" +
            $"共 {CurrentItems.Count} 个 SN 将被恢复到重工前状态。\n\n" +
            $"⚠ 此操作会覆盖 G_SN_STATUS 的现有数据。",
            "确认恢复全部",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = SnRecoveryService.RecoverAllByReworkNo(reworkNo);

            Logger.Info($"[RECOVERY] RecoverAll {reworkNo}, affected={affected}");

            MessageBox.Show($"恢复成功。\n\n共影响 {affected} 条记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 重新查询刷新界面
            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[RECOVERY] RecoverAll failed: {ex.Message}");
            MessageBox.Show("恢复全部失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    // ============ 内部 ============

    private void LoadByReworkNo(string reworkNo)
    {
        var snList = SnRecoveryService.GetSnListByReworkNo(reworkNo);
        if (snList.Count == 0)
        {
            StatusMessage = $"重工号 [{reworkNo}] 没有匹配记录";
            MessageBox.Show($"重工号 [{reworkNo}] 没有匹配记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var currentList = SnRecoveryService.GetCurrentByReworkNo(reworkNo);
        var backupList = SnRecoveryService.GetBackupByReworkNo(reworkNo);

        foreach (var item in currentList) CurrentItems.Add(item);
        foreach (var item in backupList) BackupItems.Add(item);

        StatusMessage = $"重工号 [{reworkNo}]，共 {snList.Count} 个 SN，" +
                        $"当前 {currentList.Count} / 备份 {backupList.Count}";
    }

    private void LoadBySn(string sn)
    {
        var currentList = SnRecoveryService.GetCurrentBySn(sn);
        var backupList = SnRecoveryService.GetBackupBySn(sn);

        if (currentList.Count == 0 && backupList.Count == 0)
        {
            StatusMessage = $"未找到 SN [{sn}]";
            MessageBox.Show($"未找到 SN [{sn}]。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var item in currentList) CurrentItems.Add(item);
        foreach (var item in backupList) BackupItems.Add(item);

        StatusMessage = $"SN [{sn}]，当前 {currentList.Count} / 备份 {backupList.Count}";
    }

}