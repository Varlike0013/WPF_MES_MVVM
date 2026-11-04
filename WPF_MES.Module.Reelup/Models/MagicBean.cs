// Models/MagicBean.cs
namespace WPF_MES.Module.ReelUp.Models;

/// <summary>
/// 终端通讯状态缓存（对应 Java MagicBean）
/// </summary>
public sealed class MagicBean
{
    // 数组长度基于 systemconfig.properties 的默认值 (Status.Qty=100, CST.Qty=50)
    public string[] His { get; set; } = new string[100];
    public string[] Cst { get; set; } = new string[50];
    public string[] Cmd { get; set; } = new string[100]; // 命令码映射
    public const int MaxCmdStatus = 100;

    public long LastUseTime { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public int ReConnectLoseTime { get; set; }

    public MagicBean()
    {
        // 初始化空字符串，避免 NullReferenceException
        for (int i = 0; i < His.Length; i++) His[i] = string.Empty;
        for (int i = 0; i < Cst.Length; i++) Cst[i] = string.Empty;
        for (int i = 0; i < Cmd.Length; i++) Cmd[i] = string.Empty;
    }

    /// <summary>
    /// 深拷贝（原 Java 也有 copy 方法，用于安全传递）
    /// </summary>
    public MagicBean Copy()
    {
        var copy = new MagicBean
        {
            His = (string[])this.His.Clone(),
            Cst = (string[])this.Cst.Clone(),
            Cmd = (string[])this.Cmd.Clone(),
            LastUseTime = this.LastUseTime,
            ReConnectLoseTime = this.ReConnectLoseTime
        };
        return copy;
    }
}