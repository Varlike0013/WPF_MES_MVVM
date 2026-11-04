using WPF_MES.Module.ReelUp.Models;

namespace WPF_MES.Module.ReelUp.Services;

public class GunTimeoutMonitor : IDisposable
{
    private readonly ReelUpDao _reelUpDao;
    private readonly ReelUpConfig _config;
    private readonly Dictionary<string, MagicBean[]> _activeMagicBeans = new();
    private readonly CancellationTokenSource _cts = new();

    public GunTimeoutMonitor(ReelUpDao reelUpDao, ReelUpConfig config)
    {
        _reelUpDao = reelUpDao;
        _config = config;
        _ = Task.Run(RunMonitorAsync);
    }

    public void RegisterMagicBeans(string floorCode, MagicBean[] beans)
    {
        _activeMagicBeans[floorCode] = beans;
    }

    private async Task RunMonitorAsync()
    {
        Logger.Info("[GunTimeoutMonitor] 超时监控服务已启动");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long timeoutMs = _config.GunTimeoutMs;

                foreach (var beans in _activeMagicBeans.Values)
                {
                    if (beans == null) continue;
                    foreach (var bean in beans)
                    {
                        if (bean == null) continue;
                        if (bean.Cst[1] != "0" && (now - bean.LastUseTime > timeoutMs))
                        {
                            bean.Cst[1] = "0";
                            bean.Cst[10] = string.Empty;
                            Logger.Warn($"[GunTimeoutMonitor] 终端 {bean.Cst[4]} (IP: {bean.Cst[5]}) 扫码超时，状态已重置");
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Logger.Error($"[GunTimeoutMonitor] 异常: {ex.Message}"); }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}