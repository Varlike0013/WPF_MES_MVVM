using WPF_MES.Contracts;
using WPF_MES.Module.ReelUp.Models;

namespace WPF_MES.Module.ReelUp.Services;

public sealed class ReelUpAuthService : IAuthService
{
    private readonly UserDao _userDao = new();

    // 1. 硬编码账号列表：(密码, 是否为开发者)
    // 只要匹配成功，不需要连接数据库
    private static readonly Dictionary<string, (string Password, bool IsDeveloper)> HardcodedUsers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "varlike", ("021226", true) },  // 开发者：有设置页，能新增终端
        { "guest",   ("123456", false) }  // 访客：无设置页，只能看界面
    };

    public LoginResult Login(string userNo, string password)
    {
        try
        {
            // ==========================================
            // 第一步：优先比对软件硬编码的账号
            // ==========================================
            if (HardcodedUsers.TryGetValue(userNo, out var hardcodedUser) && hardcodedUser.Password == password)
            {
                // 根据硬编码配置赋予权限
                bool isDev = hardcodedUser.IsDeveloper;

                UserSession.Instance.SignIn(userNo, userNo, isDeveloper: isDev);

                Logger.Info($"[Auth] 硬编码用户 {userNo} 登录成功 ({(isDev ? "Developer" : "Operator")})");

                return new LoginResult
                {
                    Success = true,
                    Message = "OK",
                    UserNo = userNo,
                    UserName = userNo
                };
            }

            // ==========================================
            // 第二步：如果硬编码不匹配，再查数据库
            // ==========================================
            var user = _userDao.GetUser(userNo, password);

            if (user == null)
            {
                Logger.Warn($"[Auth] 登录失败：账号或密码错误 ({userNo})");
                return new LoginResult { Success = false, Message = "账号或密码错误", UserNo = userNo };
            }

            // 数据库用户的权限判定（只有 ADMIN 字段为 1 才是开发者）
            bool isDbDev = user.Admin;

            UserSession.Instance.SignIn(user.UserNo, user.Username, isDbDev);

            Logger.Info($"[Auth] 数据库用户 {userNo} 登录成功 ({(isDbDev ? "Developer" : "Operator")})");

            return new LoginResult
            {
                Success = true,
                Message = "OK",
                UserNo = user.UserNo,
                UserName = user.Username
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"[Auth] 登录异常：{ex.Message}");
            return new LoginResult { Success = false, Message = $"系统异常：{ex.Message}", UserNo = userNo };
        }
    }
}