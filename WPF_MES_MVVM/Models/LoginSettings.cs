namespace WPF_MES_MVVM.Models;

/// <summary>
/// 登录界面设置
/// </summary>
public class LoginSettings
{
    /// <summary>上次登录的模块 Key</summary>
    public string LastModuleKey { get; set; } = string.Empty;

    /// <summary>历史用户名列表（最近的排前面）</summary>
    public List<string> UserNoHistory { get; set; } = new();

    /// <summary>历史用户名最多保留多少个</summary>
    public int MaxHistoryCount { get; set; } = 5;
}