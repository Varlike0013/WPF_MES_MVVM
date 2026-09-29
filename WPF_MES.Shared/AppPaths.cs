using System.IO;

namespace WPF_MES.Shared;

/// <summary>
/// 应用运行时路径统一管理。
/// 所有可写文件都放在 exe 目录下的固定子文件夹。
/// </summary>
public static class AppPaths
{
    /// <summary>exe 所在目录</summary>
    public static string BaseDir => AppContext.BaseDirectory;
    /// <summary>配置文件夹</summary>
    public static string ConfigDir => Path.Combine(BaseDir, "Config");
    /// <summary>日志文件夹</summary>
    public static string LogDir => Path.Combine(BaseDir, "Logs");
    /// <summary>缓存文件夹</summary>
    public static string CacheDir => Path.Combine(BaseDir, "Config", "Cache");
    /// <summary>
    /// 获取配置文件完整路径，并确保 Config 目录存在。
    /// </summary>
    public static string ConfigFile(string fileName)
    {
        Directory.CreateDirectory(ConfigDir);
        return Path.Combine(ConfigDir, fileName);
    }
    /// <summary>
    /// 获取日志文件完整路径，并确保 Logs 目录存在。
    /// </summary>
    public static string LogFile(string fileName)
    {
        Directory.CreateDirectory(LogDir);
        return Path.Combine(LogDir, fileName);
    }
}