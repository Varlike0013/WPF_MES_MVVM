namespace WPF_MES.Shared;

/// <summary>
/// 当前登录用户信息。登录成功后设置。
/// </summary>
public static class CurrentUser
{
    /// <summary>工号</summary>
    public static string UserNo { get; set; } = string.Empty;

    /// <summary>姓名</summary>
    public static string UserName { get; set; } = string.Empty;

    /// <summary>当前模块 Key（如 "SAJET"）</summary>
    public static string ModuleKey { get; set; } = string.Empty;

    /// <summary>是否已登录</summary>
    public static bool IsLoggedIn => !string.IsNullOrEmpty(UserNo);

    /// <summary>清空</summary>
    public static void Clear()
    {
        UserNo = string.Empty;
        UserName = string.Empty;
        ModuleKey = string.Empty;
    }
}