using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace WPF_MES.Shared.Network;

/// <summary>
/// 网络通信服务：聊天 + 文件传输。
/// 协议：4 字节长度前缀 + UTF-8 JSON。
/// </summary>
public class NetworkChatService : IDisposable
{
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private readonly object _writeLock = new();
    private readonly Dictionary<string, FileReceiver> _receivingFiles = new();

    /// <summary>接收文件夹</summary>
    public string ReceiveFolder { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "ReceivedFiles");

    /// <summary>状态变化</summary>
    public event Action<string>? StatusChanged;

    /// <summary>收到聊天消息</summary>
    public event Action<ChatMessage>? MessageReceived;

    /// <summary>收到文件</summary>
    public event Action<string>? FileReceived;

    /// <summary>文件发送/接收进度（文件名，当前块，总块数）</summary>
    public event Action<string, int, int>? FileProgress;

    // ============ 启动服务端 ============

    public async Task StartServerAsync(int port)
    {
        Disconnect();

        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        StatusChanged?.Invoke($"监听中 端口 {port}，等待连接...");

        _client = await _listener.AcceptTcpClientAsync();
        _stream = _client.GetStream();
        StatusChanged?.Invoke($"已连接 {_client.Client.RemoteEndPoint}");

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    // ============ 连接客户端 ============

    public async Task ConnectAsync(string ip, int port)
    {
        Disconnect();

        _client = new TcpClient();
        await _client.ConnectAsync(ip, port);
        _stream = _client.GetStream();
        StatusChanged?.Invoke($"已连接到 {ip}:{port}");

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    // ============ 断开 ============

    public void Disconnect()
    {
        try { _cts?.Cancel(); } catch { }

        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        try { _listener?.Stop(); } catch { }

        _stream = null;
        _client = null;
        _listener = null;

        // 清理未完成的文件接收
        foreach (var fr in _receivingFiles.Values)
        {
            try { fr.Finish(); } catch { }
        }
        _receivingFiles.Clear();

        StatusChanged?.Invoke("已断开");
    }

    // ============ 发送聊天 ============

    public async Task SendChatAsync(string text)
    {
        var msg = new ChatMessage
        {
            Type = "chat",
            Content = text,
            Time = DateTime.Now.ToString("HH:mm:ss"),
        };
        await SendAsync(msg);
    }

    // ============ 发送文件 ============

    public async Task SendFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件不存在", filePath);

        const int chunkSize = 64 * 1024;    // 64KB 每块
        var fi = new FileInfo(filePath);
        int totalChunks = (int)Math.Ceiling((double)fi.Length / chunkSize);
        if (totalChunks == 0) totalChunks = 1;

        // 1. 发文件头
        await SendAsync(new ChatMessage
        {
            Type = "file_start",
            FileName = fi.Name,
            FileSize = fi.Length,
            TotalChunks = totalChunks,
        });

        // 2. 发分块
        using var fs = File.OpenRead(filePath);
        var buffer = new byte[chunkSize];
        int index = 0;
        int read;

        while ((read = await fs.ReadAsync(buffer)) > 0)
        {
            var chunk = new byte[read];
            Array.Copy(buffer, chunk, read);

            await SendAsync(new ChatMessage
            {
                Type = "file_chunk",
                FileName = fi.Name,
                ChunkIndex = index,
                TotalChunks = totalChunks,
                Data = Convert.ToBase64String(chunk),
            });

            index++;
            FileProgress?.Invoke(fi.Name, index, totalChunks);
        }

        // 3. 发文件尾
        await SendAsync(new ChatMessage
        {
            Type = "file_end",
            FileName = fi.Name,
        });
    }

    // ============ 接收循环 ============

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        if (_stream == null) return;

        var header = new byte[4];
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 读 4 字节长度
                int read = 0;
                while (read < 4)
                {
                    int n = await _stream.ReadAsync(header.AsMemory(read, 4 - read), ct);
                    if (n == 0) { Disconnect(); return; }
                    read += n;
                }

                int len = BitConverter.ToInt32(header, 0);
                if (len <= 0 || len > 200 * 1024 * 1024) { Disconnect(); return; }

                // 读 body
                var body = new byte[len];
                read = 0;
                while (read < len)
                {
                    int n = await _stream.ReadAsync(body.AsMemory(read, len - read), ct);
                    if (n == 0) { Disconnect(); return; }
                    read += n;
                }

                string json = Encoding.UTF8.GetString(body);
                var msg = JsonSerializer.Deserialize<ChatMessage>(json, jsonOptions);
                if (msg != null) HandleMessage(msg);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"接收失败: {ex.Message}");
            Disconnect();
        }
    }

    private void HandleMessage(ChatMessage msg)
    {
        switch (msg.Type)
        {
            case "chat":
                MessageReceived?.Invoke(msg);
                break;

            case "file_start":
                _receivingFiles[msg.FileName] = new FileReceiver(
                    msg.FileName, msg.FileSize, ReceiveFolder);
                StatusChanged?.Invoke($"开始接收文件: {msg.FileName}");
                break;

            case "file_chunk":
                if (_receivingFiles.TryGetValue(msg.FileName, out var fr))
                {
                    fr.Append(Convert.FromBase64String(msg.Data), msg.ChunkIndex);
                    FileProgress?.Invoke(msg.FileName, msg.ChunkIndex + 1, msg.TotalChunks);
                }
                break;

            case "file_end":
                if (_receivingFiles.TryGetValue(msg.FileName, out var fr2))
                {
                    fr2.Finish();
                    _receivingFiles.Remove(msg.FileName);
                    StatusChanged?.Invoke($"文件接收完成: {fr2.SavePath}");
                    FileReceived?.Invoke(fr2.SavePath);
                }
                break;
        }
    }

    // ============ 底层发送 ============

    private Task SendAsync(ChatMessage msg)
    {
        if (_stream == null)
            throw new InvalidOperationException("未连接");

        string json = JsonSerializer.Serialize(msg);
        var body = Encoding.UTF8.GetBytes(json);
        var header = BitConverter.GetBytes(body.Length);

        lock (_writeLock)
        {
            _stream.Write(header, 0, 4);
            _stream.Write(body, 0, body.Length);
        }

        return Task.CompletedTask;
    }

    public void Dispose() => Disconnect();
}