namespace WPF_MES.Shared.Models;

/// <summary>
/// 部门选项
/// </summary>
public class DeptOption
{
    public int DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;

    public override string ToString() => DeptName;
}
