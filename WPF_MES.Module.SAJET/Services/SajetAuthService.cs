using WPF_MES.Contracts;

namespace WPF_MES.Module.SAJET.Services;

internal class SajetAuthService : IAuthService
{
    public LoginResult Login(string userNo, string password)
    {
        try
        {
            var (ok, msg) = SajetCommonService.CheckEmpPwd(userNo, password);

            if (!ok)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = string.IsNullOrEmpty(msg) ? "用户名或密码错误" : msg,
                    UserNo = userNo,
                };
            }

            return new LoginResult
            {
                Success = true,
                Message = msg,
                UserNo = userNo,
                UserName = SajetCommonService.GetUserName(userNo),
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                Message = "数据库访问失败：" + ex.Message,
                UserNo = userNo,
            };
        }
    }
}