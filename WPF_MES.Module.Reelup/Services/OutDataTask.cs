using System.IO;
using System.Net.Sockets;
using System.Text;
using WPF_MES.Module.ReelUp.Models;
using WPF_MES.Module.ReelUp.Services.Devices;
using WPF_MES.Shared;

namespace WPF_MES.Module.ReelUp.Services;

public class OutDataTask : IDisposable
{
    private readonly string _strIp;
    private readonly IDeviceProtocol _dataTrans;
    private readonly MagicBean[] _magic;
    private readonly WlsCore _core;
    private CancellationTokenSource _cts = new();

    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    public event Action<bool>? OnStateChanged;
    public bool IsManualStop { get; set; } = false;

    public bool IsRunning { get; private set; }
    public event Action<ReceiveBean>? OnReceived;

    public OutDataTask(string ip, string gunType, MagicBean[] magic, WlsCore core)
    {
        _strIp = ip;
        _magic = magic;
        _core = core;
        _dataTrans = DeviceProtocolFactory.Create(gunType) ?? throw new ArgumentException($"未知的枪型: {gunType}");
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(int port = 4660)
    {
        IsRunning = true;
        IsManualStop = false;
        if (_cts.IsCancellationRequested) _cts = new CancellationTokenSource();
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_strIp, port, _cts.Token);
                    _stream = _client.GetStream();
                    _reader = new StreamReader(_stream, Encoding.ASCII);
                    OnStateChanged?.Invoke(true);
                    Logger.Info($"[OutData] 已连接到 {_strIp}:{port}");
                }

                // 阻塞读取一行
                if (_reader == null) continue;
                string? readMess = await _reader.ReadLineAsync(_cts.Token);
                if (readMess == null)
                {
                    // 连接断开
                    _client.Close();
                    continue;
                }

                readMess = readMess.Trim();
                if (readMess.Length == 0) continue;
                if (readMess.Length > 60) readMess = readMess.Substring(0, 60);

                int id = _dataTrans.GetId(readMess);
                string data = _dataTrans.GetData(readMess).Trim();

                // 越界保护
                if (id < 0 || id >= _magic.Length) continue;

                var bean = _magic[id];

                // 1. 匹配命令并更新状态码 (cst[1])
                CheckCMD(bean, data);

                // 2. 记录扫码历史
                int status = int.TryParse(bean.Cst[1], out var s) ? s : 0;
                if (status >= 0 && status < bean.His.Length)
                {
                    bean.His[status] = data;
                }

                // 3. 触发存储过程并回传设备
                if (_stream == null) continue;
                string feedback = _core.CoreAction(id, bean, _stream, _dataTrans, data);

                // 4. 记录最后使用时间（供超时清理使用）
                bean.LastUseTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                // 5. 触发 UI 更新事件
                OnReceived?.Invoke(new ReceiveBean
                {
                    StrData = data,
                    StrId = id.ToString(),
                    StrIp = _strIp,
                    StrLine = GetLine(id.ToString()),
                    StrTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Feedback = feedback
                });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                OnStateChanged?.Invoke(false);
                Logger.Error($"[OutData] {_strIp} 异常: {ex.Message}");
                if (IsManualStop) break; // 如果是手动停止，不再重连，直接退出循环
                await Task.Delay(3000, _cts.Token); // 否则延迟 3 秒重连
            }
        }
        if (!IsManualStop)
        {
            OnStateChanged?.Invoke(false);
        }
        IsRunning = false;
    }

    private void CheckCMD(MagicBean bean, string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        for (int i = 0; i < bean.Cmd.Length; i++)
        {
            string cmd = bean.Cmd[i]?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(cmd)) continue;

            // 原版逻辑：完全匹配，或者如果 data 长度大于1，取前两位匹配
            if (data.Equals(cmd, StringComparison.OrdinalIgnoreCase) ||
                (data.Length > 1 && data.Substring(0, 2).Equals(cmd, StringComparison.OrdinalIgnoreCase)))
            {
                bean.Cst[1] = i.ToString();
            }
        }
    }

    private string GetLine(string idStr)
    {
        foreach (var bean in _magic)
        {
            if (bean != null && bean.Cst[4] == idStr) return bean.Cst[6];
        }
        return "NULL";
    }

    public void Stop()
    {
        IsManualStop = true;
        _cts.Cancel();
        _client?.Close();
    }

    public void Dispose()
    {
        Stop();
        _client?.Dispose();
        _cts?.Dispose();
    }
}