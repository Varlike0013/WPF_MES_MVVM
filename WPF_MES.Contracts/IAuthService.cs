namespace WPF_MES.Contracts;

/// <summary>
/// 认证服务。每个模块实现自己的登录逻辑（连自己的数据库）
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 校验用户名密码
    /// </summary>
    LoginResult Login(string userNo, string password);
}