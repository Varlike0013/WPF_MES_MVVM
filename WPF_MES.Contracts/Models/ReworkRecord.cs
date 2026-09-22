namespace WPF_MES.Contracts;

/// <summary>
/// 重工记录
/// </summary>
public class ReworkRecord
{
    /// <summary>重工号</summary>
    public string ReworkNo { get; set; } = string.Empty;

    /// <summary>操作人员</summary>
    public string EmpName { get; set; } = string.Empty;

    /// <summary>执行时间</summary>
    public string UpdateTime { get; set; } = string.Empty;

    /// <summary>备注</summary>
    public string Remark { get; set; } = string.Empty;
}