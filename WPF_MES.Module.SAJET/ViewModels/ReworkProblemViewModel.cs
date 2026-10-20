using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class ReworkProblemViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;
    private bool _isFetching;
    private readonly ConditionManager _conditions = new();

    public ObservableCollection<ReworkProblemItem> Problems { get; } = new();

    [ObservableProperty] private ReworkProblemItem? _selectedProblem;
    [ObservableProperty] private string _statusText = "准备就绪";
    [ObservableProperty] private bool _isBusy;

    // ========== 中间详情 ==========
    [ObservableProperty] private string _detailSerialNumber = string.Empty;
    [ObservableProperty] private string _detailPdLine = string.Empty;
    [ObservableProperty] private string _detailReason = string.Empty;
    [ObservableProperty] private string _detailSubmitter = string.Empty;
    [ObservableProperty] private string _detailConfirmer = string.Empty;

    // ========== 右侧建议 ==========
    [ObservableProperty] private string _solutionText = "请选择左侧问题查看解决方法。";

    // ========== 附加条件/参数区 ==========
    public ObservableCollection<ConditionItem> ConditionItems { get; } = new();
    public ObservableCollection<InputTypeItem> InputTypes { get; } = new();
    [ObservableProperty] private InputTypeItem? _selectedInputType;
    [ObservableProperty] private string _inputText = string.Empty;

    // ========== 处理表单 ==========
    [ObservableProperty] private string _inputEmpSfis = string.Empty;
    public ObservableCollection<SolutionOption> SolutionTypes { get; } = new();
    [ObservableProperty] private SolutionOption? _selectedSolution;

    public ReworkProblemViewModel()
    {
        // 初始化条件下拉框
        InputTypes.Add(new InputTypeItem("序号", ConditionType.SerialNumber));
        InputTypes.Add(new InputTypeItem("工单", ConditionType.WorkOrder));
        InputTypes.Add(new InputTypeItem("箱号", ConditionType.Carton));
        InputTypes.Add(new InputTypeItem("其他", ConditionType.Rework)); // 借用现有的枚举
        SelectedInputType = InputTypes[0];

        AddSolutionOption("人工处理", UpdateRecored);
        // 默认选中第一个
        SelectedSolution = SolutionTypes.FirstOrDefault();

        // 监听条件变化，刷新列表
        _conditions.Changed += RefreshConditionItems;

        // 立即拉取一次
        _ = FetchNewProblemsAsync();

        // 每 30 秒轮询一次
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += async (_, _) => await FetchNewProblemsAsync();
        _timer.Start();
    }

    public void Dispose() => _timer.Stop();

    // ============ 条件管理 ============

    private void RefreshConditionItems()
    {
        ConditionItems.Clear();
        foreach (var kv in _conditions.GetAll())
        {
            foreach (var val in kv.Value)
            {
                ConditionItems.Add(new ConditionItem(kv.Key, GetTypeName(kv.Key), val));
            }
        }
    }

    private string GetTypeName(ConditionType type)
    {
        return InputTypes.FirstOrDefault(x => x.Type == type)?.Display ?? type.ToString();
    }

    [RelayCommand]
    private void AddCondition()
    {
        if (SelectedInputType == null || string.IsNullOrWhiteSpace(InputText)) return;

        _conditions.Add(SelectedInputType.Type, InputText.Trim());
        InputText = string.Empty; // 清空输入框，方便扫码枪连续扫码
    }

    [RelayCommand]
    private void RemoveCondition(ConditionItem? item)
    {
        if (item == null) return;
        _conditions.Remove(item.Type, item.Value);
    }

    [RelayCommand]
    private void ClearConditions()
    {
        _conditions.Clear();
    }

    // ============ 定时拉取 ============
    private async Task FetchNewProblemsAsync()
    {
        if (_isFetching) return;
        _isFetching = true;

        try
        {
            StatusText = "正在获取最新问题...";
            var result = await Task.Run(ReworkProblemService.QueryUnresolvedProblems);
            var existingIds = new HashSet<int>(Problems.Select(p => p.NumberIndex));
            int added = 0;

            foreach (var item in result)
            {
                if (!existingIds.Contains(item.NumberIndex))
                {
                    Problems.Insert(0, item);
                    added++;
                }
            }

            StatusText = added > 0
                ? $"新增 {added} 个问题（共 {Problems.Count} 个）"
                : $"暂无新增问题（共 {Problems.Count} 个）";
        }
        catch (Exception ex)
        {
            StatusText = $"获取失败：{ex.Message}";
            Logger.Error($"[REWORK-AUTO] 轮询失败: {ex.Message}");
        }
        finally
        {
            _isFetching = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await FetchNewProblemsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ============ 选中问题切换 ============
    partial void OnSelectedProblemChanged(ReworkProblemItem? value)
    {
        if (value == null)
        {
            DetailSerialNumber = string.Empty;
            DetailPdLine = string.Empty;
            DetailReason = string.Empty;
            DetailSubmitter = string.Empty;
            DetailConfirmer = string.Empty;
            SolutionText = "请选择左侧问题查看解决方法。";
            return;
        }

        DetailSerialNumber = value.SerialNumber;
        DetailPdLine = value.PdLineName;
        DetailReason = value.ReasonQuery;
        DetailSubmitter = value.SubmitterName;
        DetailConfirmer = value.ConfirmerName;

        // 默认带入当前登录用户和状态
        InputEmpSfis = CurrentUser.UserName;
        // 1. 清空之前的条件
        _conditions.Clear();
        // 2. 调用解析函数，识别并分类字符串
        var combinedText = $"{value.SerialNumber} \r\n {value.ReasonQuery}";
        var parsedParams = ParseAndClassifyInput(combinedText);
        // 3. 将解析出的条件自动加入 _conditions
        foreach (var kvp in parsedParams)
        {
            foreach (var val in kvp.Value)
            {
                _conditions.Add(kvp.Key, val);
            }
        }

        // 如果没有解析出任何序号，为了防止列表为空，可以做个兜底（可选）
        if (parsedParams[ConditionType.SerialNumber].Count == 0 && !string.IsNullOrWhiteSpace(value.SerialNumber))
        {
            _conditions.Add(ConditionType.SerialNumber, value.SerialNumber.Trim());
        }

        // 4. 智能识别处理方式
        //var keywords = AnalyzeAndSetSolutionType(value.ReasonQuery);

        // 5. 提取参数
        //ExtractAndSetParameters(value.ReasonQuery, keywords);

        try
        {
            SolutionText = ReworkSolutionService.Analyze(value);
        }
        catch (Exception ex)
        {
            SolutionText = $"解析异常：{ex.Message}";
        }
    }

    // ============ 提交处理 ============
    [RelayCommand]
    private async Task CompleteProblemAsync()
    {
        if (SelectedProblem == null) return;

        if (string.IsNullOrWhiteSpace(InputEmpSfis))
        {
            MessageBox.Show("请输入处理人姓名。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 如果有附加条件，拼接到提示框里
        string conditionDesc = ConditionItems.Count > 0
            ? "\n附加参数：" + string.Join(" | ", ConditionItems.Select(c => $"{c.TypeName}:{c.Value}"))
            : "";

        var confirm = MessageBox.Show(
            $"确定将序号 {SelectedProblem.SerialNumber} 标记为处理完成吗？\n\n" +
            (ConditionItems.Count > 0 ? $"\n附加参数：{string.Join(" | ", ConditionItems.Select(c => $"{c.TypeName}:{c.Value}"))}" : ""),
            "确认处理", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            //int rows = await Task.Run(() => ReworkProblemService.CompleteProblem(
            //SelectedProblem.NumberIndex, InputEmpSfis));
            int rows = 0;
            if (rows > 0)
            {
                MessageBox.Show("处理成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

                // 从列表中移除已处理的项
                var item = SelectedProblem;
                Problems.Remove(item);
                SelectedProblem = null;
            }
            else
            {
                MessageBox.Show("更新失败，未找到该记录或已被其他人处理。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("处理失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Logger.Error($"[REWORK-AUTO] 提交失败: {ex.Message}");
        }
    }
    /// <summary>
    /// 解析扫描输入的字符串，按规则分类为工单、箱号、序号
    /// </summary>
    /// <param name="input">输入的一长串字符串</param>
    /// <returns>按条件类型分类的字典</returns>
    /// <summary>
    /// 解析扫描或输入的字符串，并分类为工单、箱号、序号
    /// </summary>
    private Dictionary<ConditionType, List<string>> ParseAndClassifyInput(string input)
    {
        var result = new Dictionary<ConditionType, List<string>>
        {
            { ConditionType.WorkOrder, new List<string>() },
            { ConditionType.Carton, new List<string>() },
            { ConditionType.SerialNumber, new List<string>() }
        };

        if (string.IsNullOrWhiteSpace(input)) return result;

        // 1. 先按空格、换行、制表符拆分
        var tokens = input.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            string tempToken = token;

            // 2. 提取工单：以 -O, -S, -D, -P 结尾
            // 正则解析：允许字母、数字、点和连字符，直到末尾的 -O/-S/-D/-P
            var woMatches = Regex.Matches(tempToken, @"[A-Za-z0-9\.\-]+?-[OSDP]\b", RegexOptions.IgnoreCase);
            foreach (Match m in woMatches)
            {
                string wo = m.Value.Trim();
                if (!result[ConditionType.WorkOrder].Contains(wo))
                    result[ConditionType.WorkOrder].Add(wo);

                // 从字符串中剔除已匹配的工单，防止被误认为 SN
                tempToken = tempToken.Replace(m.Value, " ");
            }

            // 3. 提取箱号：'dddGdddddd' 或 'CARZY' 开头
            var cartonMatches = Regex.Matches(tempToken, @"(\d{3}G\d{6})|(CARZY[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
            foreach (Match m in cartonMatches)
            {
                string carton = m.Value.Trim();
                if (!result[ConditionType.Carton].Contains(carton))
                    result[ConditionType.Carton].Add(carton);

                // 同样剔除已匹配的箱号
                tempToken = tempToken.Replace(m.Value, " ");
            }

            // 4. 提取 SN：剩余的由字母和数字组成，且长度 >= 8
            // 提取连续的字母数字组合进行判断，避免中文干扰
            var snMatches = Regex.Matches(tempToken, @"[A-Za-z0-9]+");
            foreach (Match m in snMatches)
            {
                string sn = m.Value.Trim();
                if (sn.Length >= 8) // 长度不小于 8 位
                {
                    if (!result[ConditionType.SerialNumber].Contains(sn))
                        result[ConditionType.SerialNumber].Add(sn);
                }
            }
        }

        return result;
    }
    /// <summary>
    /// 添加一个处理方式选项
    /// </summary>
    /// <param name="name">选项名称（如：重工至 F1Test）</param>
    /// <param name="action">对应的执行函数，不传则默认为 null（走人工处理逻辑）</param>
    /// <param name="selectIt">是否在添加后立即选中该项</param>
    /// <returns>返回添加或已存在的选项对象</returns>
    public SolutionOption AddSolutionOption(string name, Action<ReworkExecuteContext>? action = null, bool selectIt = false)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("选项名称不能为空", nameof(name));

        // 1. 查重：如果已经存在同名选项，直接返回现有对象
        var existing = SolutionTypes.FirstOrDefault(x => x.Name == name);
        if (existing != null)
        {
            if (selectIt) SelectedSolution = existing;
            return existing;
        }

        // 2. 创建新选项
        var option = new SolutionOption
        {
            Name = name,
            ExecuteAction = action
        };

        // 3. 插入到集合。建议插在最前面（排在“人工处理”前面），或者根据业务追加在最后
        SolutionTypes.Insert(0, option);

        // 4. 是否自动选中
        if (selectIt)
        {
            SelectedSolution = option;
        }

        return option;
    }
    private void UpdateRecored(ReworkExecuteContext context)
    {
        // 这里可以写重工专属的 SQL 或接口调用
        Logger.Info($"[REWORK-AUTO] 更新重工记录: {context.NumberIndex}");
        // 比如调用 ReworkProblemService.ExecuteRework(context);
    }
    private void ExecuteRework(ReworkExecuteContext context)
    {
        // 这里可以写重工专属的 SQL 或接口调用
        Logger.Info($"[REWORK-AUTO] 开始执行重工，序号: {context.NumberIndex}");
        // 比如调用 ReworkProblemService.ExecuteRework(context);
    }
}