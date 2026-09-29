using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class TgsGroupTestViewModel : ObservableObject
{
    // ============ 顶部条件 ============

    public ObservableCollection<string> LineOptions { get; } = new();
    public ObservableCollection<string> ProcessOptions { get; } = new();
    public ObservableCollection<TgsTerminalItem> TerminalOptions { get; } = new();

    [ObservableProperty] private string? _selectedLine;
    [ObservableProperty] private string? _selectedProcess;
    [ObservableProperty] private TgsTerminalItem? _selectedTerminal;
    public ObservableCollection<DataMapItem> DataMapItems { get; } = new();

    /// <summary>
    /// 更新 dataMap 字典 + 同步到表格
    /// </summary>
    private void UpdateDataMap(string key, string value)
    {
        _dataMap[key] = value;

        // 找到已有项，更新
        var item = DataMapItems.FirstOrDefault(x => x.Key == key);
        if (item != null)
        {
            item.Value = value;
        }
        else
        {
            DataMapItems.Add(new DataMapItem(key, value));
        }
    }

    /// <summary>选中产线 → 加载工序</summary>
    partial void OnSelectedLineChanged(string? value)
    {
        ProcessOptions.Clear();
        SelectedProcess = null;

        if (string.IsNullOrEmpty(value)) return;

        try
        {
            foreach (var p in TgsGroupTestService.LoadProcesses(value))
                ProcessOptions.Add(p);
        }
        catch (Exception ex)
        {
            Logger.Error($"[TGSTEST] Load processes failed: {ex.Message}");
        }
    }

    /// <summary>选中工序 → 加载终端</summary>
    partial void OnSelectedProcessChanged(string? value)
    {
        TerminalOptions.Clear();
        SelectedTerminal = null;

        if (string.IsNullOrEmpty(value)) return;
        if (string.IsNullOrEmpty(SelectedLine)) return;

        try
        {
            foreach (var t in TgsGroupTestService.LoadTerminals(SelectedLine, value))
                TerminalOptions.Add(t);
        }
        catch (Exception ex)
        {
            Logger.Error($"[TGSTEST] Load terminals failed: {ex.Message}");
        }
    }

    /// <summary>选中终端 → 记入 dataMap + 日志</summary>
    partial void OnSelectedTerminalChanged(TgsTerminalItem? value)
    {
        if (value == null)
        {
            AppendLog("[Terminal selection cleared]");
            return;
        }

        UpdateDataMap("TTERMINALID", value.TerminalId);
        UpdateDataMap("TTERMINALNAME", value.TerminalName);
        UpdateDataMap("TPDLINEID", value.PdlineId);
        UpdateDataMap("TPROCESSID", value.ProcessId);
        UpdateDataMap("TSTAGEID", value.StageId);

        AppendLog($"[TERMINAL CHANGED] TTERMINALID={value.TerminalId} | " +
                  $"TTERMINALNAME={value.TerminalName} | " +
                  $"TPDLINEID={value.PdlineId} | " +
                  $"TPROCESSID={value.ProcessId} | " +
                  $"TSTAGEID={value.StageId}");
    }

    // ============ 行为ID ============

    [ObservableProperty] private string _inputGroupId = string.Empty;

    // ============ 执行输入 ============

    [ObservableProperty] private string _inputTrev = string.Empty;
    [ObservableProperty] private string _currentInputType = "SN";
    [ObservableProperty] private string _stepDisplay = "未开始";

    // ============ 日志 ============

    [ObservableProperty] private string _logText = string.Empty;

    private readonly StringBuilder _logBuilder = new();

    // ============ 内部状态 ============

    private readonly Dictionary<string, string> _dataMap = new();
    private List<GroupJobInfo> _groupJobs = new();
    private List<JobDetailInfo> _jobDetails = new();
    private int _currentStep;
    private int _maxStep;

    // ============ 构造 ============

    public TgsGroupTestViewModel()
    {
        LoadLines();
    }

    private void LoadLines()
    {
        try
        {
            LineOptions.Clear();
            foreach (var l in TgsGroupTestService.LoadPdLines())
                LineOptions.Add(l);
        }
        catch (Exception ex)
        {
            Logger.Error($"[TGSTEST] Load lines failed: {ex.Message}");
            MessageBox.Show("加载产线失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：查询行为组 ============

    [RelayCommand]
    private void LoadGroup()
    {
        string idStr = InputGroupId.Trim();
        if (idStr.Length == 0)
        {
            MessageBox.Show("请输入行为ID。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(idStr, out int groupId))
        {
            MessageBox.Show("请输入有效的数字。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedTerminal == null)
        {
            MessageBox.Show("请先选择终端。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            UpdateDataMap("TGROUPID", groupId.ToString());

            var jobs = TgsGroupTestService.LoadGroupJobs(groupId);
            _groupJobs = jobs;

            if (jobs.Count == 0)
            {
                MessageBox.Show("未找到匹配的记录。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 打印第一个 Group 信息
            var first = jobs[0];
            AppendLog($"[Current Execute Group: {first.GroupId}-{first.GroupName}]");
            CurrentInputType = first.TypeNameE;

            // 打印每个 Job
            foreach (var job in jobs)
            {
                AppendLog($"[Will Execute Job: {job.JobId}-{job.JobDesc} " +
                          $"(Step: {job.GroupSeq}); " +
                          $"ProcCallName: {job.ProcCallName}; " +
                          $"TypeName: {job.TypeNameE}]");
            }

            _maxStep = jobs.Count;
            _currentStep = 0;

            UpdateStepDisplay();

            Logger.Info($"[TGSTEST] LoadGroup: groupId={groupId}, jobs={jobs.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[TGSTEST] LoadGroup failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：执行当前步骤 ============

    [RelayCommand]
    private void ExecuteStep()
    {
        if (_groupJobs.Count == 0)
        {
            MessageBox.Show("请先查询数据。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentStep >= _maxStep)
        {
            MessageBox.Show("所有步骤已执行完毕。", "完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string trev = InputTrev.Trim();
        var jobInfo = _groupJobs[_currentStep];

        AppendLog($"[EXECUTE {_currentStep + 1}/{_maxStep}: " +
                  $"JOB_ID={jobInfo.JobId}, TREV={(trev.Length == 0 ? "(空)" : trev)}]");

        // 记录 TNOW
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        UpdateDataMap("TNOW", now);
        AppendLog($"[INFO][INPUT({now}) SAVE TO TNOW]");

        // 加载 Job 详情
        try
        {
            _jobDetails = TgsGroupTestService.LoadJobDetails(
                jobInfo.GroupId, jobInfo.JobId);
        }
        catch (Exception ex)
        {
            AppendLog($"[ERROR][Load job details failed: {ex.Message}]");
            return;
        }

        // 执行 Job
        ExecuteJobProc(trev, jobInfo.ProcCallName);

        InputTrev = string.Empty;
        _currentStep++;

        if (_currentStep >= _maxStep)
        {
            UpdateStepDisplay();
            MessageBox.Show("所有步骤已执行完毕。", "完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 更新下一个 Job 的输入类型
        var nextJob = _groupJobs[_currentStep];
        CurrentInputType = nextJob.TypeNameE;
        UpdateStepDisplay();
    }

    // ============ 内部：执行 Job 下的所有存储过程 ============

    private void ExecuteJobProc(string trev, string procCallName)
    {
        if (_jobDetails.Count == 0)
        {
            AppendLog("[ERROR][Execute JobProc: No procedures to execute]");
            return;
        }

        AppendLog("[INFO][JOB START]");

        // 保存 TREV 到 procCallName 对应的键
        UpdateDataMap(procCallName, trev);
        AppendLog($"[INFO][INPUT({trev}) SAVE TO {procCallName}]");

        foreach (var detail in _jobDetails)
        {
            string procName = detail.SprocName;

            if (string.IsNullOrEmpty(procName))
            {
                AppendLog("[WARNING][Procedure name is empty, skipped]");
                continue;
            }

            AppendLog($"[INFO][Start Execute: {procName}]");
            AppendLog($"[INFO][TREV input: {trev}]");

            try
            {
                string log = TgsGroupTestService.ExecuteProcedure(
                    procName, _dataMap, trev,
                    out string tresValue,
                    out Dictionary<string, string> outParams);

                AppendLog(log);

                // ============ 保存输出参数到 dataMap ============
                foreach (var kv in outParams)
                {
                    // 跳过 TRES（它不是数据，只是执行结果标志）
                    if (kv.Key == "TRES") continue;
                    if (string.IsNullOrEmpty(kv.Value)) continue;

                    // 保存（已存在则覆盖）
                    bool isNew = !_dataMap.ContainsKey(kv.Key);
                    UpdateDataMap(kv.Key, kv.Value);

                    if (isNew)
                        AppendLog($"[INFO][OUTPUT({kv.Value}) SAVE TO {kv.Key}]");
                    else
                        AppendLog($"[INFO][OUTPUT({kv.Value}) UPDATE {kv.Key}]");
                }
                // =================================================

                if (tresValue == "OK")
                    AppendLog($"[INFO][TRES: {tresValue}]");
                else
                    AppendLog($"[ERROR][TRES: {tresValue}]");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR][Execute {procName} failed: {ex.Message}]");
            }
        }

        AppendLog("[INFO][JOB COMPLETE]");
        AppendLog("");
    }

    // ============ 命令：清空日志 ============

    [RelayCommand]
    private void ClearLog()
    {
        _logBuilder.Clear();
        LogText = string.Empty;
    }
    [RelayCommand]
    private void ClearDataMap()
    {
        _dataMap.Clear();
        DataMapItems.Clear();
        AppendLog("[INFO] DataMap cleared");
    }

    // ============ 内部：追加日志 ============

    private void AppendLog(string text)
    {
        _logBuilder.AppendLine(text);
        LogText = _logBuilder.ToString();
    }

    private void UpdateStepDisplay()
    {
        StepDisplay = _maxStep == 0
            ? "未开始"
            : $"步骤 {_currentStep}/{_maxStep}";
    }
}