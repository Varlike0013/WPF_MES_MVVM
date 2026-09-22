using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows;
using WPF_MES.Shared;
using WPF_MES.Shared.Network;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class NetworkChatViewModel : ObservableObject
{
    private readonly NetworkChatService _service = new();

    // ============ 连接参数 ============

    /// <summary>true=服务端（监听），false=客户端（连接）</summary>
    [ObservableProperty] private bool _isServerMode = false;

    [ObservableProperty] private string _ipAddress = "192.168.1.100";
    [ObservableProperty] private int _port = 8888;
    [ObservableProperty] private string _status = "未连接";
    [ObservableProperty] private bool _isConnected;

    // ============ 聊天 ============

    public ObservableCollection<ChatLogItem> ChatLogs { get; } = new();

    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 文件 ============

    [ObservableProperty] private string _receiveFolder;

    public NetworkChatViewModel()
    {
        _receiveFolder = Path.Combine(AppContext.BaseDirectory, "ReceivedFiles");
        _service.ReceiveFolder = _receiveFolder;

        // 事件订阅
        _service.StatusChanged += s =>
            Application.Current.Dispatcher.Invoke(() => Status = s);

        _service.MessageReceived += m =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatLogs.Add(new ChatLogItem
                {
                    Time = m.Time,
                    From = "对方",
                    Content = m.Content,
                    IsSelf = false,
                });
            });

        _service.FileReceived += path =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatLogs.Add(new ChatLogItem
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    From = "文件",
                    Content = $"已接收: {path}",
                    IsSelf = false,
                });
            });

        _service.FileProgress += (name, cur, total) =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                Status = $"传输中 {name} ({cur}/{total})";
            });
    }

    // ============ 连接/断开 ============

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            if (IsServerMode)
            {
                await _service.StartServerAsync(Port);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(IpAddress))
                {
                    MessageBox.Show("请输入目标 IP。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                await _service.ConnectAsync(IpAddress.Trim(), Port);
            }

            IsConnected = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"[NET] Connect failed: {ex.Message}");
            MessageBox.Show("连接失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            IsConnected = false;
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        _service.Disconnect();
        IsConnected = false;
    }

    // ============ 发送聊天 ============

    [RelayCommand]
    private async Task SendAsync()
    {
        string text = InputText.Trim();
        if (text.Length == 0) return;

        if (!IsConnected)
        {
            MessageBox.Show("请先连接。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _service.SendChatAsync(text);

            ChatLogs.Add(new ChatLogItem
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                From = "我",
                Content = text,
                IsSelf = true,
            });

            InputText = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show("发送失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 发送文件 ============

    [RelayCommand]
    private async Task SendFileAsync()
    {
        if (!IsConnected)
        {
            MessageBox.Show("请先连接。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new OpenFileDialog
        {
            Title = "选择要发送的文件",
            Filter = "所有文件 (*.*)|*.*",
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            ChatLogs.Add(new ChatLogItem
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                From = "文件",
                Content = $"正在发送: {dlg.FileName}",
                IsSelf = true,
            });

            await _service.SendFileAsync(dlg.FileName);

            ChatLogs.Add(new ChatLogItem
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                From = "文件",
                Content = $"发送完成: {Path.GetFileName(dlg.FileName)}",
                IsSelf = true,
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"[NET] Send file failed: {ex.Message}");
            MessageBox.Show("发送文件失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 选择接收文件夹 ============

    [RelayCommand]
    private void BrowseReceiveFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "选择接收文件夹",
            InitialDirectory = ReceiveFolder,
        };

        if (dlg.ShowDialog() == true)
        {
            ReceiveFolder = dlg.FolderName;
            _service.ReceiveFolder = ReceiveFolder;
        }
    }

    // ============ 打开接收文件夹 ============

    [RelayCommand]
    private void OpenReceiveFolder()
    {
        try
        {
            Directory.CreateDirectory(ReceiveFolder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ReceiveFolder,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("打开文件夹失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 清空聊天 ============

    [RelayCommand]
    private void ClearChat()
    {
        ChatLogs.Clear();
    }
}

// ============ 聊天记录项 ============

public class ChatLogItem
{
    public string Time { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsSelf { get; set; }

    /// <summary>显示文本： [时间] 来源: 内容</summary>
    public string Display => $"[{Time}] {From}: {Content}";
}