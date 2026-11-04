using System.IO;
using System.Net.Sockets;
using System.Text;
using WPF_MES.Module.ReelUp.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.ReelUp.Services;

public class IoDataTask : IDisposable
{
    private readonly string _ip;
    private readonly string _line;
    private readonly string _gunType; // "DI" (Input) 或 "DO" (Output)
    private readonly bool _isBottomLine;
    private readonly InputOutputDao _ioDao;
    private CancellationTokenSource _cts;
    public bool IsManualStop { get; set; } = false;

    private TcpClient? _client;
    private StreamReader? _reader;
    public event Action<bool>? OnStateChanged;

    public bool IsRunning { get; private set; }
    public event Action<ReceiveBean>? OnReceived;

    public IoDataTask(string ip, string line, string gunType, InputOutputDao ioDao)
    {
        _ip = ip;
        _line = line;
        _gunType = gunType;
        _ioDao = ioDao;
        _isBottomLine = line.EndsWith("LB", StringComparison.OrdinalIgnoreCase);
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(int port = 4660)
    {
        IsRunning = true;
        IsManualStop = false; // 重置标志
        if (_cts.IsCancellationRequested) _cts = new CancellationTokenSource();
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_ip, port, _cts.Token);
                    var stream = _client.GetStream();
                    _reader = new StreamReader(stream, Encoding.ASCII);
                    OnStateChanged?.Invoke(true);
                    Logger.Info($"[IOData] 已连接到 {_ip}:{port}");
                }

                if (_reader == null) continue;
                string? readMess = await _reader.ReadLineAsync(_cts.Token);
                if (readMess == null)
                {
                    _client.Close();
                    continue;
                }

                readMess = readMess.Trim();
                if (readMess.Length < 4) continue;
                readMess = readMess.ToUpper();

                string feedback = string.Empty;

                if (_gunType.Equals("DI", StringComparison.OrdinalIgnoreCase))
                {
                    feedback = ProcessInput(readMess);
                }
                else if (_gunType.Equals("DO", StringComparison.OrdinalIgnoreCase))
                {
                    feedback = ProcessOutput(readMess);
                }

                // 触发 UI 更新
                OnReceived?.Invoke(new ReceiveBean
                {
                    StrLine = _line,
                    StrIp = _ip,
                    StrTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    StrData = readMess,
                    Feedback = feedback
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                OnStateChanged?.Invoke(false);
                Logger.Error($"[IOData] {_ip} 异常: {ex.Message}");
                if (IsManualStop) break;
                await Task.Delay(3000, _cts.Token);
            }
        }
        if (!IsManualStop)
        {
            OnStateChanged?.Invoke(false);
        }
        IsRunning = false;
    }

    private string ProcessInput(string sn)
    {
        try
        {
            if (sn.Length > 36) return "SN Length > 36";

            // 进站：底板为2，主板为1
            int site = _isBottomLine ? 2 : 1;
            _ioDao.InsertSn(sn, _line, site);

            return "OK";
        }
        catch (Exception ex)
        {
            Logger.Error($"[IOData] Input SN: {sn} 异常: {ex.Message}");
            return ex.Message;
        }
    }

    private string ProcessOutput(string sn)
    {
        try
        {
            if (sn.Length > 36) return "SN Length > 36";

            // 出站：底板为3，主板为0
            int site = _isBottomLine ? 3 : 0;
            _ioDao.InsertSn(sn, _line, site);

            // 出站后触发扣料
            _ioDao.DeduckMaterial(_line);

            return "OK";
        }
        catch (Exception ex)
        {
            Logger.Error($"[IOData] Output SN: {sn} 异常: {ex.Message}");
            return ex.Message;
        }
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
        _cts.Dispose();
    }
}