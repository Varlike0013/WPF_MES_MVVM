using System.IO;

namespace WPF_MES.Module.ReelUp.Services;

public static class Logger
{
    private static readonly object _lock = new();
    private static readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "ReelUp.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_logPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, line);
            }
            catch { /* 忽略日志自身的异常 */ }
        }
    }
}