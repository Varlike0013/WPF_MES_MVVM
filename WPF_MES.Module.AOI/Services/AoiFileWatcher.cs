using System.IO;

namespace WPF_MES.Module.AOI.Services;

/// <summary>
/// 文件监控。事件回调在后台线程触发，ViewModel 需自行切到 UI 线程。
/// </summary>
internal sealed class AoiFileWatcher : IDisposable
{
    private FileSystemWatcher? _watcher;

    public event Action? Changed;

    public bool IsWatching => _watcher?.EnableRaisingEvents == true;

    public void Start(string directory)
    {
        Stop();
        if (!Directory.Exists(directory)) return;

        _watcher = new FileSystemWatcher(directory)
        {
            Filter = "*.txt",
            NotifyFilter = NotifyFilters.FileName
                         | NotifyFilters.LastWrite
                         | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Created += (_, _) => Changed?.Invoke();
        _watcher.Changed += (_, _) => Changed?.Invoke();
        _watcher.Renamed += (_, _) => Changed?.Invoke();
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public void Dispose() => Stop();
}