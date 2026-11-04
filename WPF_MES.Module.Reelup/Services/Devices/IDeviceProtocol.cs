namespace WPF_MES.Module.ReelUp.Services.Devices;

public interface IDeviceProtocol
{
    /// <summary>协议版本名，如 M101</summary>
    string Version { get; }

    /// <summary>从设备发来的报文中提取终端 ID</summary>
    int GetId(string message);

    /// <summary>从设备发来的报文中提取扫码数据/指令</summary>
    string GetData(string message);

    /// <summary>将存储过程返回的显示内容包装成设备能识别的 ANSI 字节流</summary>
    byte[] Encode(string data, int id, bool isComplexMode);
}