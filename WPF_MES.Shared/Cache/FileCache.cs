using System.IO;
using System.Text;
using System.Text.Json;

namespace WPF_MES.Shared.Cache;

/// <summary>
/// 通用文件缓存。
/// 结构：Config/Cache/{fileName}.json，内部是一组 key 组成的 JSON 对象。
/// 每个 key 独立维护 updateTime / count / items。
/// </summary>
public static class FileCache
{
    // ============ JSON 选项 ============

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };

    // ============ 文件锁 ============

    private static readonly Dictionary<string, object> _fileLocks = new();
    private static readonly object _lockGuard = new();

    private static object GetFileLock(string path)
    {
        lock (_lockGuard)
        {
            if (!_fileLocks.TryGetValue(path, out var lk))
            {
                lk = new object();
                _fileLocks[path] = lk;
            }
            return lk;
        }
    }

    // ============ 内部数据结构 ============

    private class CacheEntry
    {
        public DateTime UpdateTime { get; set; }
        public int Count { get; set; }
        public JsonElement? Items { get; set; }   // 延迟反序列化，避免类型丢失
    }

    private class CacheFile
    {
        public Dictionary<string, CacheEntry> Keys { get; set; } = new();
    }

    // ============ 路径 ============

    private static string GetFilePath(string fileName)
    {
        string safe = fileName
            .Replace('/', '_')
            .Replace('\\', '_')
            .Trim();

        if (safe.Length == 0)
            throw new ArgumentException("缓存文件名不能为空");

        if (!safe.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            safe += ".json";

        Directory.CreateDirectory(AppPaths.CacheDir);
        return Path.Combine(AppPaths.CacheDir, safe);
    }

    // ============ 文件级读写 ============

    private static CacheFile ReadFile(string path)
    {
        if (!File.Exists(path)) return new CacheFile();

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return new CacheFile();
            return JsonSerializer.Deserialize<CacheFile>(json, Options) ?? new CacheFile();
        }
        catch (Exception ex)
        {
            Logger.Warn($"[CACHE] Read failed: {path}, {ex.Message}");
            return new CacheFile();
        }
    }

    private static void WriteFile(string path, CacheFile file)
    {
        string json = JsonSerializer.Serialize(file, Options);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    // ============ 核心 API ============

    /// <summary>读缓存。不存在或反序列化失败返回 null。</summary>
    public static List<T>? Load<T>(string fileName, string key)
    {
        string path = GetFilePath(fileName);

        lock (GetFileLock(path))
        {
            var file = ReadFile(path);
            if (!file.Keys.TryGetValue(key, out var entry) || entry.Items == null)
                return null;

            try
            {
                return entry.Items.Value.Deserialize<List<T>>(Options);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[CACHE] Deserialize failed: {fileName}/{key}, {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>写缓存。同文件内其他 key 的数据会保留。</summary>
    public static void Save<T>(string fileName, string key, List<T> items)
    {
        string path = GetFilePath(fileName);

        lock (GetFileLock(path))
        {
            var file = ReadFile(path);

            file.Keys[key] = new CacheEntry
            {
                UpdateTime = DateTime.Now,
                Count = items.Count,
                Items = JsonSerializer.SerializeToElement(items, Options),
            };

            WriteFile(path, file);
            Logger.Info($"[CACHE] Saved {items.Count} items to {fileName}/{key}");
        }
    }

    /// <summary>删除某个 key（保留同文件内其他 key）。</summary>
    public static void Remove(string fileName, string key)
    {
        string path = GetFilePath(fileName);
        lock (GetFileLock(path))
        {
            var file = ReadFile(path);
            if (file.Keys.Remove(key))
                WriteFile(path, file);
        }
    }

    /// <summary>删除整个文件。</summary>
    public static void RemoveFile(string fileName)
    {
        string path = GetFilePath(fileName);
        lock (GetFileLock(path))
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }
    }

    /// <summary>判断 (fileName, key) 是否有缓存。</summary>
    public static bool Exists(string fileName, string key)
    {
        string path = GetFilePath(fileName);
        if (!File.Exists(path)) return false;

        lock (GetFileLock(path))
        {
            var file = ReadFile(path);
            return file.Keys.ContainsKey(key);
        }
    }

    /// <summary>取某个 key 的更新时间。</summary>
    public static DateTime? GetUpdateTime(string fileName, string key)
    {
        string path = GetFilePath(fileName);
        if (!File.Exists(path)) return null;

        lock (GetFileLock(path))
        {
            var file = ReadFile(path);
            return file.Keys.TryGetValue(key, out var e) ? e.UpdateTime : null;
        }
    }

    // ============ GetOrLoad ============

    public static List<T> GetOrLoad<T>(
        string fileName,
        string key,
        Func<List<T>> loader,
        bool forceRefresh = false,
        TimeSpan? ttl = null)
    {
        if (!forceRefresh)
        {
            var cached = Load<T>(fileName, key);
            if (cached != null && !IsExpired(fileName, key, ttl))
            {
                Logger.Debug($"[CACHE] Hit: {fileName}/{key}, {cached.Count} items");
                return cached;
            }
        }

        Logger.Debug($"[CACHE] Miss: {fileName}/{key}, loading...");
        var data = loader() ?? new List<T>();

        if (data.Count > 0)
            Save(fileName, key, data);

        return data;
    }

    private static bool IsExpired(string fileName, string key, TimeSpan? ttl)
    {
        if (ttl == null) return false;   // 永不过期
        var update = GetUpdateTime(fileName, key);
        if (update == null) return true;
        return (DateTime.Now - update.Value) > ttl.Value;
    }
}