using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Models;

public class ReworkExecuteContext
{
    public int NumberIndex { get; set; }
    public string EmpSfis { get; set; } = string.Empty;
    public string ReworkNo { get; set; } = string.Empty; // 处理结果 (OK/OTHER)
    public string SolutionType { get; set; } = string.Empty; // 处理方式 (如: 重工至R01)

    // 所有的输入参数 (键值对)
    public List<ConditionItem> Parameters { get; set; } = new();
}
public class SolutionOption
{
    public string Name { get; set; } = string.Empty;

    // 为 null 表示不执行特殊函数（如人工处理）
    public Action<ReworkExecuteContext>? ExecuteAction { get; set; }
}