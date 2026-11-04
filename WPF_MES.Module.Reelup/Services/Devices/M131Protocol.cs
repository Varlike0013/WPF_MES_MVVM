using System.Text;

namespace WPF_MES.Module.ReelUp.Services.Devices;

public class M131Protocol : IDeviceProtocol
{
    public string Version => "M131";

    public int GetId(string message)
    {
        try
        {
            if (message.StartsWith("/*"))
            {
                var tmp = message.Split('/');
                if (tmp.Length >= 3) message = tmp[2];
            }

            if (message.Length < 4) return 1;

            string s = message.Substring(0, 4);
            return int.TryParse(s, out int i) ? i : 1;
        }
        catch
        {
            return 1;
        }
    }

    public string GetData(string message)
    {
        try
        {
            if (message.StartsWith("/*"))
            {
                var tmp = message.Split('/');
                if (tmp.Length >= 3) message = tmp[2];
            }

            return message.Length > 4 ? message.Substring(4) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private byte[] FormatDisplay(string s)
    {
        byte[] b = Encoding.ASCII.GetBytes(s ?? string.Empty);
        // 每16个字符加 2 个字节 (27, 69)
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

        byte[] buffer;
        if (!isComplexMode)
        {
            buffer = new byte[40 + dataBytes.Length];
            Array.Copy(idBytes, 0, buffer, 0, 4);

            // 原版静态 ANSI
            byte[] head = { 27, 91, 50, 74, 27, 91, 51, 113, 27, 91, 54, 113, 27, 91, 49, 59, 49, 72, 27, 35, 53 };
            Array.Copy(head, 0, buffer, 4, head.Length);

            Array.Copy(dataBytes, 0, buffer, 25, dataBytes.Length);

            // 尾段固定
            buffer[buffer.Length - 15] = 27; buffer[buffer.Length - 14] = 91; buffer[buffer.Length - 13] = 51;
            buffer[buffer.Length - 12] = 59; buffer[buffer.Length - 11] = 54; buffer[buffer.Length - 10] = 72;
            buffer[buffer.Length - 9] = 27; buffer[buffer.Length - 8] = 35; buffer[buffer.Length - 7] = 52;
            buffer[buffer.Length - 6] = 79; buffer[buffer.Length - 5] = 75; buffer[buffer.Length - 4] = 33;
            buffer[buffer.Length - 3] = 27; buffer[buffer.Length - 2] = 35; buffer[buffer.Length - 1] = 53;
        }
        else
        {
            buffer = new byte[52 + dataBytes.Length];
            Array.Copy(idBytes, 0, buffer, 0, 4);

            byte[] head = { 27, 91, 50, 74, 27, 91, 52, 113, 27, 91, 55, 113, 27, 91, 56, 113, 27, 91, 53, 113, 27, 91, 52, 113, 27, 91, 49, 59, 49, 72, 27, 35, 53 };
            Array.Copy(head, 0, buffer, 4, head.Length);

            Array.Copy(dataBytes, 0, buffer, 37, dataBytes.Length);

            buffer[buffer.Length - 15] = 27; buffer[buffer.Length - 14] = 91; buffer[buffer.Length - 13] = 51;
            buffer[buffer.Length - 12] = 59; buffer[buffer.Length - 11] = 52; buffer[buffer.Length - 10] = 72;
            buffer[buffer.Length - 9] = 27; buffer[buffer.Length - 8] = 35; buffer[buffer.Length - 7] = 52;
            buffer[buffer.Length - 6] = 78; buffer[buffer.Length - 5] = 71; buffer[buffer.Length - 4] = 33;
            buffer[buffer.Length - 3] = 27; buffer[buffer.Length - 2] = 35; buffer[buffer.Length - 1] = 53;
        }

        // 统一追加 \r
        byte[] finalBuffer = new byte[buffer.Length + 1];
        Array.Copy(buffer, 0, finalBuffer, 0, buffer.Length);
        finalBuffer[finalBuffer.Length - 1] = 13;

        return finalBuffer;
    }
}