namespace WPF_MES.Shared;

/// <summary>
/// 泛型 JSON 配置文件管理器。
/// 构造时自动从 Config 目录加载；调用 Save() 写回。
/// 加载/保存时自动记录日志。
/// </summary>
/// <typeparam name="T">配置数据类型，需有公共无参构造函数</typeparam>
public class JsonConfig<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly string _fileName;

    /// <summary>配置数据。直接修改属性后调用 Save() 即可。</summary>
    public T Data { get; private set; }

    /// <summary>配置文件完整路径</summary>
    public string FilePath => _filePath;

    /// <summary>
    /// 构造时自动加载（或初始化）。
    /// </summary>
    /// <param name="fileName">文件名，如 "favorites.json"</param>
    public JsonConfig(string fileName)
    {
        _fileName = fileName;
        _filePath = AppPaths.ConfigFile(fileName);
        Data = new T();

        Load();
    }

    /// <summary>
    /// 从文件加载。
    /// </summary>
    public void Load()
    {
        try
        {
            if (!System.IO.File.Exists(_filePath))
            {
                Logger.Info($"[CONFIG] File not found, use default: {_fileName}");
                Data = new T();
                return;
            }

            var loaded = JsonHelper.Load<T>(_filePath);
            if (loaded == null)
            {
                Logger.Warn($"[CONFIG] Load returned null, use default: {_fileName}");
                Data = new T();
                return;
            }

            Data = loaded;
            Logger.Info($"[CONFIG] Loaded: {_fileName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[CONFIG] Load failed: {_fileName}, {ex.Message}");
            Data = new T();
        }
    }

    /// <summary>
    /// 重新加载（丢弃内存修改）。
    /// </summary>
    public void Reload()
    {
        Load();
    }

    /// <summary>
    /// 保存到文件。成功/失败都写日志。
    /// </summary>
    public void Save()
    {
        try
        {
            JsonHelper.Save(_filePath, Data);
            Logger.Info($"[CONFIG] Saved: {_fileName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[CONFIG] Save failed: {_fileName}, {ex.Message}");
            throw;
        }
    }
}