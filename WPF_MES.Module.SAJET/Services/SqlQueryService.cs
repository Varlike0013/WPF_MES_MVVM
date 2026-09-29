using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// SQL 查询配置管理（读写 config/queries.json）
/// </summary>
internal static class SqlQueryService
{
    private const string FileName = "queries.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };

    private static string FilePath => AppPaths.ConfigFile(FileName);

    // ============ 加载 ============

    public static List<SavedQuery> LoadAll()
    {
        if (!File.Exists(FilePath)) return new List<SavedQuery>();

        try
        {
            string json = File.ReadAllText(FilePath, System.Text.Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return new List<SavedQuery>();

            return JsonSerializer.Deserialize<List<SavedQuery>>(json, Options)
                   ?? new List<SavedQuery>();
        }
        catch (Exception ex)
        {
            Logger.Error($"[SQLQUERY] Load failed: {ex.Message}");
            return new List<SavedQuery>();
        }
    }

    // ============ 保存 ============

    public static void SaveAll(List<SavedQuery> list)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(list, Options);
            File.WriteAllText(FilePath, json, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Logger.Error($"[SQLQUERY] Save failed: {ex.Message}");
            throw new Exception("保存配置文件失败：" + ex.Message, ex);
        }
    }

    // ============ 增删改 ============

    public static void Add(SavedQuery query)
    {
        var list = LoadAll();
        list.Add(query);
        SaveAll(list);
    }

    public static void Update(string name, string newSql, string newDesc)
    {
        var list = LoadAll();
        var item = list.FirstOrDefault(q => q.Name == name);
        if (item == null) throw new Exception($"未找到名为 '{name}' 的记录");

        item.Sql = newSql;
        item.Description = newDesc;
        SaveAll(list);
    }

    public static void Delete(string name)
    {
        var list = LoadAll();
        int removed = list.RemoveAll(q => q.Name == name);
        if (removed == 0) throw new Exception($"未找到名为 '{name}' 的记录");
        SaveAll(list);
    }
}