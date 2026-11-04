using System.Collections.Concurrent;
using WPF_MES.Module.ReelUp.Models;

namespace WPF_MES.Module.ReelUp.Services;

public class ReelUpCommunicationManager : IDisposable
{
    private readonly ReelUpDao _reelUpDao;
    private readonly InputOutputDao _ioDao;
    private readonly WlsCore _wlsCore;
    private readonly GunTimeoutMonitor _timeoutMonitor;
    private readonly ReelUpConfig _config;

    // 维护运行中的线程任务 (线程安全字典，Key 为 IP)
    private readonly ConcurrentDictionary<string, OutDataTask> _outTasks = new();
    private readonly ConcurrentDictionary<string, IoDataTask> _ioTasks = new();

    // 记录所有活跃的 MagicBean 集合，供超时监控使用
    private readonly Dictionary<string, MagicBean[]> _allMagicBeans = new();

    // UI 订阅此事件以刷新表格
    public event Action<ReceiveBean>? OnDataReceived;
    // UI 订阅此事件以更新树节点的红绿灰状态
    public event Action<string, bool>? OnConnectionStateChanged;

    public ReelUpCommunicationManager(
        ReelUpDao reelUpDao,
        InputOutputDao ioDao,
        WlsCore wlsCore,
        GunTimeoutMonitor timeoutMonitor,
        ReelUpConfig config)
    {
        _reelUpDao = reelUpDao;
        _ioDao = ioDao;
        _wlsCore = wlsCore;
        _timeoutMonitor = timeoutMonitor;
        _config = config;
    }

    /// <summary>
    /// 启动单个上料终端（对应 UI 双击节点）
    /// </summary>
    public void StartOutTask(string ip, string gunType, string floorCode)
    {
        if (_outTasks.ContainsKey(ip)) return; // 已经在跑了

        // 从缓存或数据库获取该楼层的 MagicBean 数组
        if (!_allMagicBeans.TryGetValue(floorCode, out var magicBeans))
        {
            magicBeans = _reelUpDao.GetFloorId(floorCode);
            _allMagicBeans[floorCode] = magicBeans;
            _timeoutMonitor.RegisterMagicBeans(floorCode, magicBeans);
        }

        var task = new OutDataTask(ip, gunType, magicBeans, _wlsCore);
        task.OnReceived += (bean) => OnDataReceived?.Invoke(bean);

        // 监听任务内部的连接状态变化，通知 UI 变绿/变灰
        task.OnStateChanged += (isConnected) =>
        {
            OnConnectionStateChanged?.Invoke(ip, isConnected);
        };

        _outTasks[ip] = task;
        _ = Task.Run(() => task.StartAsync(_config.WlsPort));

        Logger.Info($"[CommManager] 启动上料终端 {ip}");
    }

    /// <summary>
    /// 停止单个上料终端（对应 UI 双击断开节点）
    /// </summary>
    public void StopOutTask(string ip)
    {
        if (_outTasks.TryRemove(ip, out var task))
        {
            task.Stop();
            task.Dispose();
            Logger.Info($"[CommManager] 停止上料终端 {ip}");
        }
    }

    /// <summary>
    /// 启动单个 IO 终端
    /// </summary>
    public void StartIoTask(string ip, string line, string gunType)
    {
        if (_ioTasks.ContainsKey(ip)) return;

        var task = new IoDataTask(ip, line, gunType, _ioDao);
        task.OnReceived += (bean) => OnDataReceived?.Invoke(bean);

        task.OnStateChanged += (isConnected) =>
        {
            OnConnectionStateChanged?.Invoke(ip, isConnected);
        };

        _ioTasks[ip] = task;
        _ = Task.Run(() => task.StartAsync(_config.WlsPort));

        Logger.Info($"[CommManager] 启动 IO 终端 {ip}");
    }

    /// <summary>
    /// 停止单个 IO 终端
    /// </summary>
    public void StopIoTask(string ip)
    {
        if (_ioTasks.TryRemove(ip, out var task))
        {
            task.Stop();
            task.Dispose();
            Logger.Info($"[CommManager] 停止 IO 终端 {ip}");
        }
    }

    /// <summary>
    /// 停止所有通讯（应对意外情况）
    /// </summary>
    public void StopAllCommunication()
    {
        foreach (var ip in _outTasks.Keys) StopOutTask(ip);
        foreach (var ip in _ioTasks.Keys) StopIoTask(ip);
        _allMagicBeans.Clear();
        Logger.Info("[CommunicationManager] 所有通讯线程已停止");
    }

    public void Dispose()
    {
        StopAllCommunication();
    }
}