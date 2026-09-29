namespace WPF_MES.Shared.Models;

/// <summary>
/// 查询类型下拉项
/// </summary>
public class QueryTypeItem
{
    /// <summary>0=工号，1=名称</summary>
    public int Type { get; set; }
    public string Display { get; set; } = string.Empty;

    public QueryTypeItem() { }

    public QueryTypeItem(int type, string display)
    {
        Type = type;
        Display = display;
    }

    public override string ToString() => Display;
}