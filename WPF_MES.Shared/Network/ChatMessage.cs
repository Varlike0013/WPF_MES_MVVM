namespace WPF_MES.Shared.Network;

/// <summary>
/// 网络通信消息
/// </summary>
public class ChatMessage
{
    /// <summary>消息类型：chat / file_start / file_chunk / file_end</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>聊天文本</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>文件名</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>文件大小（字节）</summary>
    public long FileSize { get; set; }

    /// <summary>分块索引</summary>
    public int ChunkIndex { get; set; }

    /// <summary>总分块数</summary>
    public int TotalChunks { get; set; }

    /// <summary>分块数据（Base64）</summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>发送时间</summary>
    public string Time { get; set; } = string.Empty;
}