namespace WPF_MES.Contracts;

/// <summary>
/// 登录结果
/// </summary>
public class LoginResult
{
    /// <summary>是否登录成功</summary>
    public bool Success { get; set; }
    /// <summary>提示信息（失败时显示原因）</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>工号</summary>
    public string UserNo { get; set; } = string.Empty;
    /// <summary>姓名</summary>
    public string UserName { get; set; } = string.Empty;
}