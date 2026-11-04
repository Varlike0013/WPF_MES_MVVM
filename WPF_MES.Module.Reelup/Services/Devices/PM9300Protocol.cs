using System.Text;

namespace WPF_MES.Module.ReelUp.Services.Devices;

public class PM9300Protocol : IDeviceProtocol
{
    public string Version => "PM9300";

    // PM9300 的 ID 提取逻辑与 M131 完全一致
    private readonly M131Protocol _m131 = new();

    public int GetId(string message) => _m131.GetId(message);

    public string GetData(string message) => _m131.GetData(message);

    private byte[] FormatDisplay(string s)
    {
        // FormatDisplay 逻辑与 M131 完全一致
        byte[] b = Encoding.ASCII.GetBytes(s ?? string.Empty);
        int extra = (b.Length / 16) * 2;
        byte[] tmp = new byte[b.Length + extra];

        int j = 0;
        for (int i = 0; i < b.Length; i++)
        {
            tmp[i + j * 2] = b[i];
            if (i % 16 == 15)
            {
                tmp[i + j * 2 + 1] = 27;
                tmp[i + j * 2 + 2] = 69;
                j++;
            }
        }
        return tmp;
    }

    public byte[] Encode(string data, int id, bool isComplexMode)
    {
        string idStr = id switch
        {
            >= 0 and < 10 => "000" + id,
            >= 10 and < 100 => "00" + id,
            >= 100 and < 1000 => "0" + id,
            >= 1000 and < 10000 => id.ToString(),
            _ => "0001"
        };

        byte[] idBytes = Encoding.ASCII.GetBytes(idStr);
        byte[] dataBytes = FormatDisplay(data);

        // 原版代码里有 "flag = true"，我们直接忽略传进来的 isComplexMode
        // 修复了原版未将 dataBytes 拼入数组的严重 Bug

        int headLen = 24;
        byte[] buffer = new byte[headLen + dataBytes.Length + 1]; // +1 是结尾的 \r

        Array.Copy(idBytes, 0, buffer, 0, 4);

        // 固定的 Head ANSI
        byte[] head = { 27, 91, 56, 113, 27, 91, 53, 113, 27, 91, 57, 113, 27, 35, 52, 78, 72, 27, 35, 53 };
        // 因为原版 array 大小是 24 + length，且只写了 24 位，所以后 4 个字节其实是 0
        // 为了兼容原版行为，我们保留 0，但不让它覆盖数据。
        Array.Copy(head, 0, buffer, 4, head.Length);

        // 将数据写入（原版缺失的部分）
        Array.Copy(dataBytes, 0, buffer, 24, dataBytes.Length);

        // 结尾追加 \r
        buffer[buffer.Length - 1] = 13;

        return buffer;
    }
}