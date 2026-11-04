// Models/UserInfo.cs
namespace WPF_MES.Module.ReelUp.Models;

public class UserInfo
{
    public string UserNo { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool Admin { get; set; }
}