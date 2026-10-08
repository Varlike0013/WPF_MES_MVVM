using System.Runtime.InteropServices;
using System.IO;

namespace WPF_MES.Shared.Network;

/// <summary>
/// 访问需要凭据的网络共享（UNC 路径）。
/// </summary>
public static class NetworkShareConnector
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private class NETRESOURCE
    {
        public int dwScope = 0;
        public int dwType = 1;      // RESOURCETYPE_DISK
        public int dwDisplayType = 0;
        public int dwUsage = 0;
        public string? lpLocalName = null;
        public string? lpRemoteName;
        public string? lpComment = null;
        public string? lpProvider = null;
    }

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(
        NETRESOURCE lpNetResource, string? lpPassword, string? lpUsername, int dwFlags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

    /// <summary>
    /// 尝试访问共享根目录。
    /// 若系统已能访问（同域/已缓存凭据），直接返回成功；
    /// 否则用给定凭据调用 WNetAddConnection2 建立连接。
    /// </summary>
    public static (bool ok, string message) Connect(string uncRoot, string? user, string? password)
    {
        try
        {
            if (Directory.Exists(uncRoot))
                return (true, "已可直接访问");
        }
        catch { }

        try
        {
            var nr = new NETRESOURCE { lpRemoteName = uncRoot.TrimEnd('\\') };
            int rc = WNetAddConnection2(nr, password ?? "", user ?? "", 0);

            if (rc == 0) return (true, "连接成功");

            // 1219：已有其他凭据连接，先断开再重连
            if (rc == 1219)
            {
                try { WNetCancelConnection2(nr.lpRemoteName!, 0, true); } catch { }
                rc = WNetAddConnection2(nr, password ?? "", user ?? "", 0);
                if (rc == 0) return (true, "连接成功（已替换原连接）");
            }

            return (false, TranslateError(rc));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static void Disconnect(string uncRoot)
    {
        try { WNetCancelConnection2(uncRoot.TrimEnd('\\'), 0, true); } catch { }
    }

    private static string TranslateError(int code) => code switch
    {
        5 => "拒绝访问：用户名或密码错误",
        53 => "找不到网络路径",
        67 => "找不到网络名",
        85 => "本地设备名已在使用",
        1219 => "同名连接冲突（可能已用其他账号连过）",
        1326 => "登录失败：用户名或密码错误",
        1327 => "账号限制：空密码账号不能登录",
        1909 => "账号已被锁定",
        _ => $"未知错误（代码 {code}）"
    };
}