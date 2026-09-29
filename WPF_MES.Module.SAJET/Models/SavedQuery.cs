namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 保存的 SQL 查询
/// </summary>
public class SavedQuery
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;      // 查询/修改/删除/增加/其他
    public int TypeId { get; set; }                        // 0/1/2/3/4
    public string Description { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
}