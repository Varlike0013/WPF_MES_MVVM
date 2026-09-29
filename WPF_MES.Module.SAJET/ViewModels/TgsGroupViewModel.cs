using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class TgsGroupViewModel : ObservableObject
{
    // ============ 查询 ============

    public List<string> InputTypes { get; } = new() { "行为ID", "行为名称" };
    [ObservableProperty] private string _selectedInputType = "行为ID";
    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 当前行为组信息 ============

    [ObservableProperty] private string _currentGroupId = "ID";
    [ObservableProperty] private string _currentGroupName = "未找到对应行为";

    // ============ 结果表格 ============

    public ObservableCollection<TgsJobItem> JobItems { get; } = new();

    [ObservableProperty] private TgsJobItem? _selectedJob;

    // ============ Tab1：源码 ============

    [ObservableProperty] private string _sourceText = string.Empty;
    [ObservableProperty] private string _queryText = string.Empty;

    private readonly List<int> _matchPositions = new();
    private int _currentMatchIndex = -1;

    /// <summary>View 层持有的 RichTextBox 引用（用于高亮）</summary>
    public RichTextBox? SourceRichTextBox { get; set; }

    // ============ Tab2：测试存储过程 ============

    [ObservableProperty] private string _procName = string.Empty;
    [ObservableProperty] private string _selectedTabIndex = "0";   // 未使用，TabControl 不需要绑定

    public ObservableCollection<ProcParamItem> InputParams { get; } = new();
    public ObservableCollection<ProcParamItem> OutputParams { get; } = new();

    [ObservableProperty] private bool _hasInputParams;
    [ObservableProperty] private bool _hasOutputParams;

    // ============ 构造 ============

    public TgsGroupViewModel()
    {
    }

    // ============ 命令：查询 ============

    [RelayCommand]
    private void Search()
    {
        string input = InputText.Trim();
        if (input.Length == 0)
        {
            MessageBox.Show("请输入查询内容。", "输入错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int type = SelectedInputType == "行为ID" ? 0 : 1;

        try
        {
            var (groupId, groupName, items) = TgsGroupService.Query(type, input);

            JobItems.Clear();
            foreach (var item in items)
                JobItems.Add(item);

            if (groupId.Length > 0)
            {
                CurrentGroupId = groupId;
                CurrentGroupName = groupName;
            }
            else
            {
                CurrentGroupId = "ID";
                CurrentGroupName = "未找到对应行为";
            }

            Logger.Info($"[TGS] Query: type={type}, input={input}, jobs={items.Count}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[TGS] Query failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 加载源码 ============

    /// <summary>
    /// 选中表格一行 → 填充 ProcName + 加载源码
    /// </summary>
    partial void OnSelectedJobChanged(TgsJobItem? value)
    {
        if (value == null)
        {
            // 清空选中时：清空源码 + 清空参数表单
            SourceText = string.Empty;
            ClearSourceDisplay();

            InputParams.Clear();
            OutputParams.Clear();
            HasInputParams = false;
            HasOutputParams = false;

            return;
        }

        // 1. 填充存储过程名到测试区输入框
        ProcName = value.SprocName;

        // 2. 加载源码到 Tab1
        LoadSource(value.SprocName);

        // 3. 清空参数表单（等用户点"构建"再加载）
        InputParams.Clear();
        OutputParams.Clear();
        HasInputParams = false;
        HasOutputParams = false;
    }

    /// <summary>
    /// 加载存储过程源码
    /// </summary>
    private void LoadSource(string procName)
    {
        Logger.Info($"[TGS] LoadSource start: procName={procName}, " +
                    $"RichTextBox={SourceRichTextBox != null}");

        // 1. 无存储过程名
        if (string.IsNullOrEmpty(procName))
        {
            SourceText = "未关联存储过程";
            WriteToRichTextBox(SourceText);
            return;
        }

        // 2. 获取源码
        string source;
        try
        {
            source = TgsGroupService.GetProcedureSource(procName);

            if (string.IsNullOrEmpty(source))
            {
                source = $"未找到存储过程 {procName} 的源代码（可能权限不足或名称错误）";
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[TGS] GetProcedureSource failed: {ex.Message}");
            source = "查询存储过程代码失败：" + ex.Message;
        }

        Logger.Info($"[TGS] LoadSource got {source.Length} chars");

        // 3. 更新 ViewModel 属性
        SourceText = source;
        _matchPositions.Clear();
        _currentMatchIndex = -1;
        QueryText = string.Empty;   // 清空上次的搜索关键字

        // 4. 写到 RichTextBox
        WriteToRichTextBox(source);
    }

    /// <summary>
    /// 把源码写到 RichTextBox
    /// </summary>
    private void WriteToRichTextBox(string text)
    {
        if (SourceRichTextBox == null)
        {
            Logger.Warn("[TGS] LoadSource: SourceRichTextBox is NULL!");
            return;
        }

        SourceRichTextBox.Document.Blocks.Clear();
        SourceRichTextBox.Document.Blocks.Add(
            new Paragraph(new Run(text ?? string.Empty)));

        Logger.Info("[TGS] LoadSource written to RichTextBox");
    }

    /// <summary>
    /// 清空 RichTextBox 显示
    /// </summary>
    private void ClearSourceDisplay()
    {
        if (SourceRichTextBox == null) return;

        SourceRichTextBox.Document.Blocks.Clear();
        SourceRichTextBox.Document.Blocks.Add(new Paragraph(new Run(string.Empty)));
    }

    // ============ 命令：高亮查询 ============

    [RelayCommand]
    private void Highlight()
    {
        HighlightAllMatches(QueryText);
    }

    private void HighlightAllMatches(string text)
    {
        if (SourceRichTextBox == null) return;

        ClearHighlights();
        _matchPositions.Clear();
        _currentMatchIndex = -1;

        if (string.IsNullOrEmpty(text)) return;

        // 提取 RichTextBox 的纯文本
        var range = new TextRange(
            SourceRichTextBox.Document.ContentStart,
            SourceRichTextBox.Document.ContentEnd);
        string source = range.Text;

        // 找所有匹配
        int start = 0;
        while (true)
        {
            int idx = source.IndexOf(text, start, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            _matchPositions.Add(idx);
            start = idx + text.Length;
        }

        if (_matchPositions.Count == 0) return;

        // 给所有匹配加黄色背景
        for (int i = 0; i < _matchPositions.Count; i++)
        {
            var startPtr = GetPointerAtOffset(SourceRichTextBox.Document, _matchPositions[i]);
            var endPtr = GetPointerAtOffset(SourceRichTextBox.Document,
                                            _matchPositions[i] + text.Length);

            if (startPtr == null || endPtr == null) continue;

            var matchRange = new TextRange(startPtr, endPtr);
            matchRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
        }

        // 跳到第一个
        _currentMatchIndex = 0;
        ScrollToMatch(_matchPositions[0], text.Length);
    }

    private void ClearHighlights()
    {
        if (SourceRichTextBox == null) return;

        var range = new TextRange(
            SourceRichTextBox.Document.ContentStart,
            SourceRichTextBox.Document.ContentEnd);
        range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
    }

    private void ScrollToMatch(int startOffset, int length)
    {
        if (SourceRichTextBox == null) return;

        // WPF 用 TextPointer 定位
        var doc = SourceRichTextBox.Document;
        var startPtr = GetPointerAtOffset(doc, startOffset);
        var endPtr = GetPointerAtOffset(doc, startOffset + length);

        if (startPtr == null || endPtr == null) return;

        // 选中并滚动到可见
        SourceRichTextBox.Selection.Select(startPtr, endPtr);
        SourceRichTextBox.Focus();
    }
    /// <summary>
    /// 把"字符串索引"转成 WPF 的 TextPointer。
    /// </summary>
    private static TextPointer? GetPointerAtOffset(FlowDocument doc, int offset)
    {
        if (offset < 0) return null;

        TextPointer pointer = doc.ContentStart;
        int count = 0;

        while (pointer != null)
        {
            var context = pointer.GetPointerContext(LogicalDirection.Forward);

            if (context == TextPointerContext.Text)
            {
                string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                int len = textRun.Length;

                if (count + len >= offset)
                {
                    return pointer.GetPositionAtOffset(offset - count);
                }
                count += len;
            }

            pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
        }
        return null;
    }

    // ============ 命令：上一个 / 下一个 ============

    [RelayCommand]
    private void PreviousMatch()
    {
        if (_matchPositions.Count == 0) return;

        _currentMatchIndex = (_currentMatchIndex - 1 + _matchPositions.Count)
                             % _matchPositions.Count;
        ScrollToMatch(_matchPositions[_currentMatchIndex], QueryText.Length);
    }

    [RelayCommand]
    private void NextMatch()
    {
        if (_matchPositions.Count == 0) return;

        _currentMatchIndex = (_currentMatchIndex + 1) % _matchPositions.Count;
        ScrollToMatch(_matchPositions[_currentMatchIndex], QueryText.Length);
    }

    // ============ 命令：构建参数 ============

    [RelayCommand]
    private void Build()
    {
        if (string.IsNullOrEmpty(ProcName))
        {
            MessageBox.Show("请先选择一个存储过程。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var (inParams, outParams) = TgsGroupService.GetProcedureParams(ProcName);

            if (inParams.Count == 0 && outParams.Count == 0)
            {
                MessageBox.Show("未获取到参数信息，请检查权限。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InputParams.Clear();
            foreach (var p in inParams)
                InputParams.Add(new ProcParamItem { Name = p, Value = string.Empty });

            OutputParams.Clear();
            foreach (var p in outParams)
                OutputParams.Add(new ProcParamItem { Name = p, Value = string.Empty });

            HasInputParams = InputParams.Count > 0;
            HasOutputParams = OutputParams.Count > 0;
        }
        catch (Exception ex)
        {
            Logger.Error($"[TGS] Build failed: {ex.Message}");
            MessageBox.Show("构建失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：执行存储过程 ============

    [RelayCommand]
    private void Execute()
    {
        if (string.IsNullOrEmpty(ProcName))
        {
            MessageBox.Show("请先选择存储过程。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var inParams = InputParams.Select(p => p.Name).ToList();
            var inValues = InputParams.Select(p => p.Value).ToList();
            var outNames = OutputParams.Select(p => p.Name).ToList();

            var results = TgsGroupService.ExecuteProcedure(
                ProcName, inParams, inValues, outNames);

            // 回填输出结果
            for (int i = 0; i < OutputParams.Count && i < results.Count; i++)
                OutputParams[i].Value = results[i];

            Logger.Info($"[TGS] Execute OK: {ProcName}, outs={results.Count}");

            MessageBox.Show("执行完成。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"[TGS] Execute failed: {ex.Message}");
            MessageBox.Show("执行失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}