using System.Data;
using Oracle.ManagedDataAccess.Client;
using WPF_MES.Contracts;

namespace WPF_MES.Module.AOI.Services;

/// <summary>
/// AOI 模块登录认证。复用 SAJET 账号系统。
/// </summary>
public sealed class AoiAuthService : IAuthService
{
    public LoginResult Login(string userNo, string password)
    {
        try
        {
            using var conn = AoiDbHelper.GetConnection();
            using var cmd = new OracleCommand("SAJET.sj_chk_emp_pwd", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("temp2", OracleDbType.Varchar2).Value = userNo;
            cmd.Parameters.Add("trev", OracleDbType.Varchar2).Value = password;

            var outParam = new OracleParameter("tres", OracleDbType.Varchar2, 200)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            cmd.ExecuteNonQuery();

            string msg = outParam.Value?.ToString()?.Trim() ?? string.Empty;
            bool ok = msg.StartsWith("OK", StringComparison.OrdinalIgnoreCase);

            if (!ok)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = msg.Length > 0 ? msg : "账号或密码错误",
                    UserNo = userNo,
                    UserName = string.Empty,
                };
            }

            // 登录成功：查一次姓名回填（可选）
            string userName = GetUserName(userNo);

            return new LoginResult
            {
                Success = true,
                Message = "OK",
                UserNo = userNo,
                UserName = userName,
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                Message = $"登录异常：{ex.Message}",
                UserNo = userNo,
                UserName = string.Empty,
            };
        }
    }

    /// <summary>按工号查姓名。查不到返回空字符串。</summary>
    private static string GetUserName(string userNo)
    {
        try
        {
            using var conn = AoiDbHelper.GetConnection();
            using var cmd = new OracleCommand(
                "SELECT EMP_NAME FROM SAJET.SYS_EMP WHERE EMP_NO = :no AND ENABLED = 'Y'", conn)
            {
                BindByName = true
            };
            cmd.Parameters.Add(":no", OracleDbType.Varchar2).Value = userNo;

            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value
                ? string.Empty
                : result.ToString()!.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }
}