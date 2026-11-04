namespace WPF_MES.Module.ReelUp.Services.Devices;

public static class DeviceProtocolFactory
{
    public static IDeviceProtocol? Create(string gunType)
    {
        if (string.IsNullOrWhiteSpace(gunType)) return null;

        return gunType.Trim().ToUpper() switch
        {
            "M101" => new M101Protocol(),
            "M131" => new M131Protocol(),
            "PM9300" => new PM9300Protocol(),
            _ => null
        };
    }
}