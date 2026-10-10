using Oracle.ManagedDataAccess.Client;

namespace WPF_MES.Module.AOI.Services;

/// <summary>
/// AOI 模块专用的 Oracle 连接助手。
/// </summary>
internal static class AoiDbHelper
{
    private const string ConnString =
        "User Id=SAJET;Password=tech;" +
        "Data Source=10.240.144.17:1521/SAJET;" +
        "Pooling=true;Min Pool Size=1;Max Pool Size=20;";

    public static OracleConnection GetConnection()
    {
        var conn = new OracleConnection(ConnString);
        conn.Open();
        return conn;
    }
}