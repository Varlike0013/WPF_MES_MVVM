using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;
using WPF_MES.Shared.Cache;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class SmtReelInfoViewModel : ObservableObject
{
    // ============ 顶部条件 ============

    public ObservableCollection<string> LineOptions { get; } = new();
    public ObservableCollection<string> SiteOptions { get; } = new();

    [ObservableProperty] private string? _selectedLine;
    [ObservableProperty] private string? _selectedSite;

    /// <summary>上料时间 - 日期部分</summary>
    [ObservableProperty] private DateTime? _inputDate = DateTime.Now.AddDays(-7);

    /// <summary>上料时间 - 时分秒部分（HH:mm:ss）</summary>
    [ObservableProperty] private string _inputTimeText = "00:00:00";

    // ============ 忙碌状态 ============

    /// <summary>是否正在执行耗时操作（禁用 UI）</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isBusy;

    /// <summary>忙碌状态取反，用于 XAML 绑定</summary>
    public bool IsIdle => !IsBusy;

    [ObservableProperty] private string _busyMessage = string.Empty;

    // ============ 料盘列表 ============

    public ObservableCollection<ReelInfo> ReelList { get; } = new();

    [ObservableProperty] private ReelInfo? _selectedReel;

    // ============ 编辑区（派生自 SelectedReel） ============

    public string EditIndex => SelectedReel?.NumIndex.ToString() ?? string.Empty;
    public string EditSite => SelectedReel?.Site ?? string.Empty;
    public string EditLine => SelectedReel?.LineId ?? string.Empty;
    public string EditSn => SelectedReel?.ReelUpSn ?? string.Empty;

    public string[] ActiveOptions { get; } = { "N", "P", "Y" };

    [ObservableProperty] private string? _editActive;

    partial void OnSelectedReelChanged(ReelInfo? value)
    {
        OnPropertyChanged(nameof(EditIndex));
        OnPropertyChanged(nameof(EditSite));
        OnPropertyChanged(nameof(EditLine));
        OnPropertyChanged(nameof(EditSn));
        EditActive = value?.Active;
    }

    // ============ 构造 ============

    public SmtReelInfoViewModel()
    {
        // 后台初始化：有缓存立即生效，无缓存异步加载
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (SmtReelInfoService.HasCache())
        {
            var (sites, lines) = SmtReelInfoService.GetCached();
            ApplySites(sites);
            ApplyLines(lines);
            return;
        }
        await RefreshOptionsAsync(forceRefresh: false);
    }


    // ============ 核心：加载/刷新站点 + 线别 ============
    [RelayCommand]
    private async Task ReloadAsync()
    {
        await RefreshOptionsAsync(forceRefresh: true);
    }
    /// <summary>
    /// 刷新站点和线别选项。
    /// 有缓存且不强制刷新 → UI 线程直接读缓存（很快）；
    /// 否则 → 后台并行查询，期间禁用界面。
    /// </summary>
    private async Task RefreshOptionsAsync(bool forceRefresh)
    {
        if (!forceRefresh && SmtReelInfoService.HasCache())
        {
            var (sites, lines) = SmtReelInfoService.GetCached();
            ApplySites(sites);
            ApplyLines(lines);
            return;
        }

        IsBusy = true;
        BusyMessage = "正在加载站点和线别，请稍候...";
        try
        {
            var (sites, lines) = await Task.Run(() => SmtReelInfoService.Refresh());
            ApplySites(sites);
            ApplyLines(lines);
        }
        catch (Exception ex)
        {
            Logger.Error($"[SMTREEL] Refresh failed: {ex.Message}");
            MessageBox.Show("加载站点/线别失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private void ApplySites(List<string> sites)
    {
        SiteOptions.Clear();
        foreach (var s in sites) SiteOptions.Add(s);
        if (SiteOptions.Count > 0 && string.IsNullOrEmpty(SelectedSite))
            SelectedSite = SiteOptions[0];
    }

    private void ApplyLines(List<string> lines)
    {
        LineOptions.Clear();
        foreach (var l in lines) LineOptions.Add(l);
        if (LineOptions.Count > 0 && string.IsNullOrEmpty(SelectedLine))
            SelectedLine = LineOptions[0];
    }

    // ============ 命令：查询料盘信息 ============

    [RelayCommand]
    private async Task SelectAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedSite))
        {
            MessageBox.Show("请选择站点。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(SelectedLine))
        {
            MessageBox.Show("请选择线别。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryBuildInputTime(out DateTime inputTime))
        {
            MessageBox.Show("时间格式无效，应为 yyyy-MM-dd HH:mm:ss", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        BusyMessage = "正在查询，请稍候...";
        try
        {
            string site = SelectedSite!.Trim();
            string line = SelectedLine!.Trim();

            var list = await Task.Run(() =>
                SmtReelInfoService.QueryReelInfo(site, line, inputTime));

            ReelList.Clear();
            foreach (var item in list) ReelList.Add(item);
            SelectedReel = null;

            Logger.Info($"[SMTREEL] Query done: site={site}, line={line}, count={list.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[SMTREEL] Query failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    private bool TryBuildInputTime(out DateTime inputTime)
    {
        inputTime = default;
        if (InputDate == null) return false;
        string combined = $"{InputDate.Value:yyyy-MM-dd} {InputTimeText}";
        return DateTime.TryParse(combined, out inputTime);
    }

    // ============ 命令：保存状态 ============

    [RelayCommand]
    private void Save()
    {
        if (SelectedReel == null)
        {
            MessageBox.Show("请先在表格中选择一条记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrEmpty(EditActive))
        {
            MessageBox.Show("请选择状态 (N/P/Y)。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"确定要将以下记录的状态修改为 \"{EditActive}\" 吗？\n\n" +
            $"NUMINDEX: {SelectedReel.NumIndex}\n料盘SN: {SelectedReel.ReelUpSn}",
            "确认保存", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int rows = SmtReelInfoService.UpdateActive(
                SelectedReel.NumIndex, SelectedReel.ReelUpSn, EditActive!);

            if (rows == 0)
            {
                MessageBox.Show("更新失败，未找到匹配记录。", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // 重新查询（异步，忽略返回值）
            _ = SelectAsync();
        }
        catch (Exception ex)
        {
            Logger.Error($"[SMTREEL] Update failed: {ex.Message}");
            MessageBox.Show("更新失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：删除 ============

    [RelayCommand]
    private void Delete()
    {
        if (SelectedReel == null)
        {
            MessageBox.Show("请先在表格中选择一条记录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string sn = SelectedReel.ReelUpSn;

        try
        {
            int count = SmtReelInfoService.GetReelUpSnCount(sn);
            if (count <= 1)
            {
                MessageBox.Show(
                    $"Reel SN \"{sn}\" 仅有一条记录，\n为保证数据完整性，必须至少保留一条记录。",
                    "无法删除", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"确定要删除以下记录吗？\n\n" +
                $"NUMINDEX: {SelectedReel.NumIndex}\nReel SN: {sn}\n" +
                $"该 SN 当前共有 {count} 条记录，删除后保留 {count - 1} 条。\n\n" +
                $"此操作不可撤销！",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            int rows = SmtReelInfoService.DeleteReel(SelectedReel.NumIndex, sn);
            if (rows == 0)
            {
                MessageBox.Show("删除失败，未找到匹配记录。", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _ = SelectAsync();
        }
        catch (Exception ex)
        {
            Logger.Error($"[SMTREEL] Delete failed: {ex.Message}");
            MessageBox.Show("删除失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}