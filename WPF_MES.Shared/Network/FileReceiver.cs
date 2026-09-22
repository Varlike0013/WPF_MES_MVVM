using System.IO;

namespace WPF_MES.Shared.Network;

/// <summary>
/// 接收文件的临时缓冲。按分块索引写盘，乱序到达时缓冲等待。
/// </summary>
internal class FileReceiver
{
    public string FileName { get; }
    public long FileSize { get; }
    public string SavePath { get; }

    private readonly FileStream _fs;
    private int _nextIndex = 0;
    private readonly SortedDictionary<int, byte[]> _buffer = new();

    public FileReceiver(string fileName, long fileSize, string receiveFolder)
    {
        FileName = fileName;
        FileSize = fileSize;

        Directory.CreateDirectory(receiveFolder);

        // 文件名冲突时加时间戳
        SavePath = Path.Combine(receiveFolder, fileName);
        if (File.Exists(SavePath))
        {
            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);
            SavePath = Path.Combine(receiveFolder, $"{name}_{DateTime.Now:yyyyMMddHHmmss}{ext}");
        }

        _fs = new FileStream(SavePath, FileMode.Create, FileAccess.Write);
    }

    public void Append(byte[] data, int index)
    {
        if (index == _nextIndex)
        {
            _fs.Write(data, 0, data.Length);
            _nextIndex++;

            // 把缓冲里连续的块写出去
            while (_buffer.TryGetValue(_nextIndex, out var buffered))
            {
                _fs.Write(buffered, 0, buffered.Length);
                _buffer.Remove(_nextIndex);
                _nextIndex++;
            }
        }
        else if (index > _nextIndex)
        {
            _buffer[index] = data;
        }
    }

    public void Finish()
    {
        _fs.Flush();
        _fs.Dispose();
    }
}