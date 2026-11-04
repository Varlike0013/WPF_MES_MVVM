using Oracle.ManagedDataAccess.Client;

namespace WPF_MES.Module.ReelUp.Services;

internal static class ReelUpDbHelper
{
    private static string _connString = "User Id=wls;Password=123456;Data Source=192.168.0.16:1521/SMT;Pooling=true;Min Pool Size=1;Max Pool Size=20;";

    public static void SetConnectionString(string cs)
    {
        if (!string.IsNullOrWhiteSpace(cs)) _connString = cs;
    }

    public static OracleConnection GetConnection()
    {
        var conn = new OracleConnection(_connString);
        conn.Open();
        return conn;
    }
}