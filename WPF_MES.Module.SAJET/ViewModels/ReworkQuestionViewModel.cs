using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class ReworkQuestionViewModel : ObservableObject
{
    // ============ 筛选条件 ============

    public List<string> FloorOptions { get; } = new()
    {
        "ALL", "B2", "B3", "B31", "B32", "B33",
        "B4", "B41", "B42", "B43",
    };

    [ObservableProperty] private string _selectedFloor = "ALL";
    [ObservableProperty] private DateTime _startDate = DateTime.Now.AddDays(-7);
    [ObservableProperty] private string _keyword = string.Empty;
    [ObservableProperty] private bool _showUnfinishedOnly;

    // ============ 结果表格 ============

    public ObservableCollection<ReworkQuestionItem> ResultItems { get; } = new();

    [ObservableProperty] private ReworkQuestionItem? _selectedItem;

    // ============ 回复 ============

    [ObservableProperty] private string _replyText = string.Empty;

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 构造 ============

    public ReworkQuestionViewModel()
    {
        Search();
    }

    // ============ 命令：查询 ============

    [RelayCommand]
    private void Search()
    {
        try
        {
            int floorKey = Math.Max(0, FloorOptions.IndexOf(SelectedFloor));

            var list = ReworkQuestionService.Query(
                floorKey,
                StartDate,
                Keyword,
                ShowUnfinishedOnly);

            ResultItems.Clear();
            foreach (var item in list)
                ResultItems.Add(item);

            StatusMessage = $"共查询到 {list.Count} 条记录";

            Logger.Info($"[RQ] Search: floor={SelectedFloor}, start={StartDate:yyyy-MM-dd}, " +
                        $"kw={Keyword}, unfinished={ShowUnfinishedOnly}, count={list.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[RQ] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ResultItems.Clear();
            StatusMessage = "查询失败";
        }
    }

    // ============ 命令：回复 ============

    [RelayCommand]
    private void Reply()
    {
        // 1. 必须有选中行
        if (SelectedItem == null)
        {
            MessageBox.Show("请先选择一条记录。", "选择错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 2. 回复内容（空则默认 OK）
        string reply = ReplyText.Trim();
        if (reply.Length == 0)
            reply = "OK";

        // 3. 当前用户
        string empNo = CurrentUser.UserNo;
        if (string.IsNullOrEmpty(empNo))
        {
            MessageBox.Show("无法获取当前用户信息。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 4. 确认
        var confirm = MessageBox.Show(
            $"确定要回复记录 {SelectedItem.NumberIndex} 为 {reply} 吗？",
            "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        // 5. 执行
        try
        {
            int affected = ReworkQuestionService.Reply(
                SelectedItem.NumberIndex, empNo, reply);

            Logger.Info($"[RQ] Reply: idx={SelectedItem.NumberIndex}, " +
                        $"emp={empNo}, reply={reply}, affected={affected}");

            if (affected == 0)
            {
                MessageBox.Show("未找到对应的记录，更新失败。", "更新结果",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show("已成功更新记录。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            ReplyText = string.Empty;
            Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[RQ] Reply failed: {ex.Message}");
            MessageBox.Show("回复失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}