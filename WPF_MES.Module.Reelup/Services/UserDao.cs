using Oracle.ManagedDataAccess.Client;
using WPF_MES.Module.ReelUp.Models;

namespace WPF_MES.Module.ReelUp.Services;

public sealed class UserDao
{
    /// <summary>
    /// 按用户名/密码查用户。查不到返回 null。
    /// 注意：原表密码是明文，未做加密。
    /// </summary>
    public UserInfo? GetUser(string username, string password)
    {
        try
        {
            using var conn = ReelUpDbHelper.GetConnection();

            // 1. 先数行，避免 ExecuteScalar 在 0 行时返回 null 与 admin=0 混淆
            using (var cmd = new OracleCommand(
                "select count(*) from wls.wls_user_info where username = :u and password = :p", conn))
            {
                cmd.Parameters.Add(":u", OracleDbType.Varchar2).Value = username;
                cmd.Parameters.Add(":p", OracleDbType.Varchar2).Value = password;
                var v = cmd.ExecuteScalar();
                int count = v is null or DBNull ? 0 : Convert.ToInt32(v);
                if (count <= 0) return null;
            }

            // 2. 查 admin 标志
            int admin;
            using (var cmd = new OracleCommand(
                "select admin from wls.wls_user_info " +
                "where username = :u and password = :p and rownum = 1", conn))
            {
                cmd.Parameters.Add(":u", OracleDbType.Varchar2).Value = username;
                cmd.Parameters.Add(":p", OracleDbType.Varchar2).Value = password;
                var v = cmd.ExecuteScalar();
                admin = v is null or DBNull ? 0 : Convert.ToInt32(v);
            }

            return new UserInfo
            {
                UserNo = username,
                Username = username,
                Password = password,
                Admin = admin == 1,
            };
        }
        catch
        {
            return null;
        }
    }
}