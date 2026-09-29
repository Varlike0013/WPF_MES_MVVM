using System.IO;
using System.Text;
using System.Text.Json;

namespace WPF_MES.Shared.Cache;

/// <summary>
/// 通用文件缓存。
/// 每个 key 一个文件，存在 Config/Cache/{key}.json。
/// 支持任意类型（通过 JSON 序列化）。
/// </summary>
public static class FileCache
{
    // ============ 路径 ============

    private static string CacheDir => AppPaths.CacheDir;

    private static string GetFilePath(string fileName)
    {
        // fileName 里不允许有路径分隔符
        string safeKey = fileName.Replace('/', '_').Replace('\\', '_').Trim();
        if (safeKey.Length == 0) throw new ArgumentException("缓存 key 不能为空");

        Directory.CreateDirectory(CacheDir);
        return Path.Combine(CacheDir, $"{safeKey}.json");
    }

    // ============ 序列化选项 ============

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };

    // ============ 内部包装 ============

    private class CacheEntry<T>
    {
        public DateTime UpdateTime { get; set; }
        public int Count { get; set; }
        public List<T>? Items { get; set; }
    }

    // ============ 基础读写 ============

    /// <summary>
    /// 读缓存。不存在或反序列化失败返回 null。
    /// </summary>
    public static List<T>? Load<T>(string key)
    {
        string path = GetFilePath(key);

        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return null;

            var entry = JsonSerializer.Deserialize<CacheEntry<T>>(json, Options);
            return entry?.Items;
        }
        catch (Exception ex)
        {
            Logger.Warn($"[CACHE] Load failed: {key}, {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 写缓存。
    /// </summary>
    public static void Save<T>(string key, List<T> items)
    {
        string path = GetFilePath(key);

        try
        {
            var entry = new CacheEntry<T>
            {
                UpdateTime = DateTime.Now,
                Count = items.Count,
                Items = items,
            };

            string json = JsonSerializer.Serialize(entry, Options);
            File.WriteAllText(path, json, Encoding.UTF8);

            Logger.Info($"[CACHE] Saved {items.Count} items to {key}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[CACHE] Save failed: {key}, {ex.Message}");
            throw new Exception($"保存缓存失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 删除某个 key 的缓存。
    /// </summary>
    public static void Remove(string key)
    {
        string path = GetFilePath(key);
        if (File.Exists(path))
        {
            try { File.Delete(path); }
            catch { }
        }
    }

    /// <summary>
    /// 清空所有缓存。
    /// </summary>
    public static void Clear()
    {
        if (!Directory.Exists(CacheDir)) return;

        try
        {
            foreach (var file in Directory.GetFiles(CacheDir, "*.json"))
            {
                try { File.Delete(file); } catch { }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"[CACHE] Clear failed: {ex.Message}");
        }
    }

    // ============ 核心方法：GetOrLoad ============

    /// <summary>
    /// 优先读缓存；无缓存或强制刷新时，调 loader 拿数据并写缓存。
    /// </summary>
    /// <param name="key">缓存 key</param>
    /// <param name="loader">数据加载函数</param>
    /// <param name="forceRefresh">是否强制刷新</param>
    /// <param name="ttl">缓存有效期；null 表示永不过期</param>
    public static List<T> GetOrLoad<T>(
        string key,
        Func<List<T>> loader,
        bool forceRefresh = false,
        TimeSpan? ttl = null)
    {
        // 1. 尝试读缓存
        if (!forceRefresh)
        {
            var cached = Load<T>(key);
            if (cached != null && !IsExpired(key, ttl))
            {
                Logger.Debug($"[CACHE] Hit: {key}, {cached.Count} items");
                return cached;
            }
        }

        // 2. 加载数据
        Logger.Debug($"[CACHE] Miss: {key}, loading...");
        var data = loader() ?? new List<T>();

        // 3. 写缓存
        if (data.Count > 0)
        {
            Save(key, data);
        }

        return data;
    }

    // ============ 过期判断 ============

    private static bool IsExpired(string key, TimeSpan? ttl)
    {
        if (ttl == null) return false;   // 永不过期

        string path = GetFilePath(key);
        if (!File.Exists(path)) return true;

        try
        {
            var lastWrite = File.GetLastWriteTime(path);
            return (DateTime.Now - lastWrite) > ttl.Value;
        }
        catch
        {
            return true;
        }
    }

    // ============ 查询缓存元信息 ============

    /// <summary>
    /// 获取缓存的更新时间。
    /// </summary>
    public static DateTime? GetUpdateTime(string key)
    {
        string path = GetFilePath(key);
        if (!File.Exists(path)) return null;

        try { return File.GetLastWriteTime(path); }
        catch { return null; }
    }

    /// <summary>
    /// 判断某个 key 是否有缓存。
    /// </summary>
    public static bool Exists(string key)
    {
        return File.Exists(GetFilePath(key));
    }
}