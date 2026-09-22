using System.Windows.Interop;
using WPF_MES.Contracts;

namespace WPF_MES.Module.GEDTA.Services;

internal class DtAuthService : IAuthService
{
    public LoginResult Login(string userNo, string password)
    {
        return new LoginResult
        {
            Success = true,
            Message = "OK",
            UserNo = userNo,
            UserName = "ADMIN",
        };
    }
}