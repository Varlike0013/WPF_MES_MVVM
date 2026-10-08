using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPF_MES.Shared.Models;

namespace WPF_MES.Shared.Services;

/// <summary>
/// 共享信息 JSON 文件读写。
/// 支持 UNC 路径；写盘用临时文件 + Replace，避免网络中断损坏原文件。
/// </summary>
public static class ShareFileService
{
    public const string DefaultShareRoot = @"\\10.240.144.99\f\LeiHuang";
    public const string DefaultFileName = "share-info.json";
    public static string DefaultFullPath => Path.Combine(DefaultShareRoot, DefaultFileName);

    private static readonly object _lock = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static ShareTreeDocument Load(string path)
    {
        lock (_lock)
        {
            if (!File.Exists(path)) return new ShareTreeDocument();

            var json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return new ShareTreeDocument();

            return JsonSerializer.Deserialize<ShareTreeDocument>(json, Options)
                   ?? new ShareTreeDocument();
        }
    }

    public static void Save(string path, ShareTreeDocument doc)
    {
        lock (_lock)
        {
            doc.UpdateTime = DateTime.Now;
            string json = JsonSerializer.Serialize(doc, Options);

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json, Encoding.UTF8);

            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);
        }
    }
}