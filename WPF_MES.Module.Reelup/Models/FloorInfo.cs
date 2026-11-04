// Models/FloorInfo.cs
namespace WPF_MES.Module.ReelUp.Models;

/// <summary>
/// 楼层定义。
/// 规则：B{栋} 表示聚合，B{栋}{楼}F 是具体楼层。旧命名 B42F 等价 B402F。
/// </summary>
public sealed record FloorInfo(string Building, string Code, string OldPattern, string NewPattern);

public static class Floors
{
    // 根据之前读出的 11 个楼层配置
    public static readonly IReadOnlyList<FloorInfo> All = new[]
    {
        new FloorInfo("B4", "B402F", "B42%", "B402%"),
        new FloorInfo("B4", "B403F", "B43%", "B403%"),
        new FloorInfo("B3", "B302F", "B32%", "B302%"),
        new FloorInfo("B3", "B303F", "B33%", "B303%"),
        new FloorInfo("B2", "B202F", "B22%", "B202%"),
        new FloorInfo("B2", "B203F", "B23%", "B203%"),
        new FloorInfo("B1", "B103F", "B13%", "B103%"),
        new FloorInfo("B1", "B102F", "B12%", "B102%"),
        new FloorInfo("B4", "B401F", "B41%", "B401%"),
        new FloorInfo("B5", "B501F", "B51%", "B501%"),
        new FloorInfo("B5", "B502F", "B52%", "B502%"),
    };

    public static IEnumerable<FloorInfo> GetByBuilding(string building)
        => All.Where(f => f.Building.Equals(building, StringComparison.OrdinalIgnoreCase));

    public static FloorInfo? Find(string? floorCode)
        => string.IsNullOrWhiteSpace(floorCode) ? null : All.FirstOrDefault(f => f.Code.Equals(floorCode, StringComparison.OrdinalIgnoreCase));

    public static string BuildLineFilter(string? floorCode, string columnName = "LINE")
    {
        var f = Find(floorCode);
        if (f is null) return string.Empty;
        return $"and ({columnName} like '{f.OldPattern}' or {columnName} like '{f.NewPattern}') ";
    }
}