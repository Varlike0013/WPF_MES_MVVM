namespace WPF_MES.Shared.Models;

/// <summary>
/// 员工列表项
/// </summary>
public class EmpInfoItem
{
    public string EmpNo { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UpdateTime { get; set; } = string.Empty;
    public string DeptName { get; set; } = string.Empty;
}
