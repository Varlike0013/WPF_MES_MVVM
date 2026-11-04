// Services/UserSession.cs
namespace WPF_MES.Module.ReelUp.Services;

public sealed class UserSession
{
    public static UserSession Instance { get; } = new();

    public string UserNo { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public bool IsDeveloper { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public void SignIn(string userNo, string userName, bool isDeveloper)
    {
        UserNo = userNo;
        UserName = userName;
        IsDeveloper = isDeveloper;
        IsAuthenticated = true;
    }

    public void SignOut()
    {
        UserNo = string.Empty;
        UserName = string.Empty;
        IsDeveloper = false;
        IsAuthenticated = false;
    }
}