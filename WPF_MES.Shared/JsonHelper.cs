using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WPF_MES.Shared;

/// <summary>
/// JSON 序列化/反序列化工具。
/// 统一配置：缩进输出、支持中文、失败不抛异常。
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // 让中文不被转义成 \uXXXX
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// 把对象序列化到文件。目录不存在时自动创建。
    /// </summary>
    public static void Save<T>(string filePath, T obj)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(obj, Options);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            throw new Exception($"保存 JSON 失败：{filePath}，{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从文件加载对象。文件不存在或解析失败时返回 null。
    /// </summary>
    public static T? Load<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch
        {
            // 解析失败时返回 null，由调用方决定怎么处理
            return null;
        }
    }
}