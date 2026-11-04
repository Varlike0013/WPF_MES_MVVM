using System.Text;

namespace WPF_MES.Module.ReelUp.Services.Devices;

public class M101Protocol : IDeviceProtocol
{
    public string Version => "M101";

    public int GetId(string message)
    {
        try
        {
            if (message.Length < 3) return 0;
            string idStr = message.Substring(0, 3).Trim();
            if (string.IsNullOrEmpty(idStr)) return 0;

            // 原版逻辑：跳过前导0
            for (int i = 0; i < idStr.Length; i++)
            {
                if (idStr[i] != '0')
                    return int.Parse(idStr.Substring(i));
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    public string GetData(string message)
    {
        try
        {
            return message.Length > 3 ? message.Substring(3) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public byte[] Encode(string data, int id, bool isComplexMode)
    {
        string idStr = id switch
        {
            >= 0 and < 10 => "00" + id,
            >= 10 and < 100 => "0" + id,
            >= 100 and < 1000 => id.ToString(),
            _ => "000" // 越界兜底
        };

        byte[] idBytes = Encoding.ASCII.GetBytes(idStr);
        byte[] dataBytes = Encoding.ASCII.GetBytes(data ?? string.Empty);

        int rowNum = dataBytes.Length % 16 == 0 ? dataBytes.Length / 16 : dataBytes.Length / 16 + 1;
        if (rowNum == 0) rowNum = 1;

        int headLen = 11; // ID(3) + ESC[0M(4) + ESC[0J(4)
        int tailLen = isComplexMode ? 7 : 3;
        int rowBreakLen = 7;

        // 分配总长度
        byte[] buffer = new byte[headLen + dataBytes.Length + (rowNum - 1) * rowBreakLen + tailLen];

        // 1. 写入 ID
        Array.Copy(idBytes, 0, buffer, 0, 3);

        // 2. 写入头部控制符 ESC [ 0 M ESC [ 0 J
        buffer[3] = 27; buffer[4] = 91; buffer[5] = 48; buffer[6] = 77;
        buffer[7] = 27; buffer[8] = 91; buffer[9] = 48; buffer[10] = 74;

        int count = 10;
        // 3. 写入数据及换行控制
        for (int j = 0; j < dataBytes.Length; j++)
        {
            if (j % 16 == 0 && j / 16 > 0)
            {
                buffer[++count] = 27; buffer[++count] = 91; buffer[++count] = 71; // ESC [ G
                buffer[++count] = 27; buffer[++count] = 91; buffer[++count] = 49; buffer[++count] = 66; // ESC [ 1 B
            }
            buffer[++count] = dataBytes[j];
        }

        // 4. 写入尾部控制符
        if (!isComplexMode)
        {
            buffer[buffer.Length - 3] = 27; buffer[buffer.Length - 2] = 69; buffer[buffer.Length - 1] = 13; // ESC E \r
        }
        else
        {
            buffer[buffer.Length - 7] = 27; buffer[buffer.Length - 6] = 91; buffer[buffer.Length - 5] = 52; buffer[buffer.Length - 4] = 113; // ESC [ 4 q
            buffer[buffer.Length - 3] = 27; buffer[buffer.Length - 2] = 69; buffer[buffer.Length - 1] = 13; // ESC E \r
        }

        return buffer;
    }
}