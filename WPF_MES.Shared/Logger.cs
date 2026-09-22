using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WPF_MES.Shared;

/// <summary>
/// 日志输出。根据启动参数决定是否启用控制台。
/// 用法：mes.exe --console
/// </summary>
public static class Logger
{
    // ============ P/Invoke ============

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(uint dwProcessId);
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
    private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

    // ============ 状态 ============

    private static readonly object _lock = new();
    private static StreamWriter? _fileWriter;
    private static string? _logFile;

    public static bool ConsoleEnabled { get; private set; }
    public static bool FileEnabled { get; private set; }

    // ============ 初始化 ============

    /// <summary>
    /// 根据命令行参数初始化。
    /// </summary>
    public static void Init(string[] args)
    {
        EnableFileLog();

        foreach (var arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--console":
                case "-c":
                    EnableConsole();
                    break;

                case "--help":
                case "-h":
                case "-?":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
            }
        }
    }

    private static void EnableConsole()
    {
        if (ConsoleEnabled) return;

        // 1. 先尝试附加到父进程的控制台（从 cmd 启动时有效）
        if (AttachConsole(ATTACH_PARENT_PROCESS))
        {
            // 重定向所有标准流，让 Console.Write* 生效
            RedirectStdStreams();
            ConsoleEnabled = true;
            return;
        }

        // 2. 失败才新建控制台
        if (AllocConsole())
        {
            RedirectStdStreams();
            Console.OutputEncoding = Encoding.UTF8;
            ConsoleEnabled = true;
        }
    }

    /// <summary>
    /// 把 Console 的输入/输出流重定向到当前控制台。
    /// AttachConsole / AllocConsole 之后必须调用，否则 Console.Write 无效。
    /// </summary>
    private static void RedirectStdStreams()
    {
        try
        {
            var stdout = Console.OpenStandardOutput();
            var writer = new StreamWriter(stdout) { AutoFlush = true };
            Console.SetOut(writer);

            var stderr = Console.OpenStandardError();
            var errWriter = new StreamWriter(stderr) { AutoFlush = true };
            Console.SetError(errWriter);

            var stdin = Console.OpenStandardInput();
            Console.SetIn(new StreamReader(stdin));
        }
        catch
        {
            // 重定向失败时不影响主流程
        }
    }

    private static void EnableFileLog()
    {
        if (FileEnabled) return;

        try
        {
            // 复用 AppPaths，统一 Logs 目录
            _logFile = AppPaths.LogFile($"mes_{DateTime.Now:yyyyMMdd}.log");

            // 常驻 StreamWriter：Append + 自动 flush
            var fs = new FileStream(_logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _fileWriter = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };

            FileEnabled = true;
        }
        catch
        {
            FileEnabled = false;
        }
    }

    // ============ 日志方法 ============

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Debug(string message) => Write("DEBUG", message);

    private static void Write(string level, string message)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

        // 1. VS 调试窗口
        System.Diagnostics.Debug.WriteLine(line);

        // 2. 控制台
        if (ConsoleEnabled)
        {
            try { Console.WriteLine(line); } catch { }
        }

        // 3. 文件（加锁保护，多线程安全）
        if (FileEnabled && _fileWriter != null)
        {
            lock (_lock)
            {
                try
                {
                    _fileWriter.WriteLine(line);
                }
                catch
                {
                    // 日志写失败不影响程序
                }
            }
        }
    }

    /// <summary>
    /// 关闭文件句柄，释放控制台。程序退出前调用。
    /// </summary>
    public static void Shutdown()
    {
        if (FileEnabled && _fileWriter != null)
        {
            lock (_lock)
            {
                try
                {
                    _fileWriter.Flush();
                    _fileWriter.Dispose();
                }
                catch { }
                _fileWriter = null;
                FileEnabled = false;
            }
        }

        if (ConsoleEnabled)
        {
            FreeConsole();
            ConsoleEnabled = false;
        }
    }

    // ============ 帮助 ============

    /// <summary>
    /// 显示命令行帮助
    /// </summary>
    private static void ShowHelp()
    {
        if (!ConsoleEnabled) EnableConsole();
        Console.Write("\r\n");
        Console.WriteLine("WPF_MES_MVVM - MES System");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  WPF_MES_MVVM.exe [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -c, --console        Enable console window for live log output");
        Console.WriteLine("  -h, --help, -?       Show this help message and exit");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  WPF_MES_MVVM.exe                    Start normally (log to file)");
        Console.WriteLine("  WPF_MES_MVVM.exe --console          Start with console output");
        Console.WriteLine("  WPF_MES_MVVM.exe --console --help   Show this help");
        Console.WriteLine();
        Console.WriteLine($"Log file: {_logFile ?? "(not initialized)"}");
        Console.WriteLine();
        Console.ReadKey(true);
        Console.Write("\r\n");
    }
}