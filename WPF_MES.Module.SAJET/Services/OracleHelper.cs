using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text;
using WPF_MES.Contracts.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// SAJET 数据库访问工具。只做连接和通用执行，不写业务 SQL。
/// </summary>
internal static class OracleHelper
{
    private const string ConnString =
        "User Id=SAJET;Password=tech;" +
        "Data Source=10.240.144.17:1521/SAJET;" +
        "Pooling=true;Min Pool Size=1;Max Pool Size=20;";
    /// <summary>
    /// 获取数据库连接
    /// </summary>
    public static OracleConnection GetConnection()
    {
        var conn = new OracleConnection(ConnString);
        conn.Open();
        return conn;
    }
    /// <summary>
    /// 查询表格输入sql和参数数组Dic<string,T>
    /// </summary>
    public static DataTable QueryDataTable(string sql,
        Dictionary<string, object>? parameters = null)
    {
        try
        {
            var dt = new DataTable();
            using var conn = GetConnection();
            using var cmd = new OracleCommand(sql, conn);
            AddParams(cmd, parameters);
            using var adapter = new OracleDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"查询失败：{ex.Message}\nSQL: {sql.Trim()}\n参数：{FormatParams(parameters)}", ex);
        }
    }
    /// <summary>
    /// 执行删改查，返回影响行数
    /// </summary>
    public static int ExecuteNonQuery(string sql,
        Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var conn = GetConnection();
            using var cmd = new OracleCommand(sql, conn);
            AddParams(cmd, parameters);
            return cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"执行失败：{ex.Message}\nSQL: {sql.Trim()}\n参数：{FormatParams(parameters)}", ex);
        }
    }
    /// <summary>
    /// 执行查询返回第一行的结果
    /// </summary>
    public static object? ExecuteScalar(string sql,
        Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var conn = GetConnection();
            using var cmd = new OracleCommand(sql, conn);
            AddParams(cmd, parameters);
            return cmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"执行失败：{ex.Message}\nSQL: {sql.Trim()}\n参数：{FormatParams(parameters)}", ex);
        }
    }

    // ============ 私有辅助 ============

    private static void AddParams(OracleCommand cmd, Dictionary<string, object>? ps)
    {
        if (ps == null) return;
        foreach (var kv in ps)
        {
            var p = cmd.Parameters.Add(kv.Key, InferDbType(kv.Value));
            p.Value = kv.Value ?? DBNull.Value;
        }
    }

    private static OracleDbType InferDbType(object? value)
    {
        if (value == null || value == DBNull.Value) return OracleDbType.Varchar2;
        return value switch
        {
            int => OracleDbType.Int32,
            long => OracleDbType.Int64,
            short => OracleDbType.Int16,
            decimal => OracleDbType.Decimal,
            double => OracleDbType.Double,
            float => OracleDbType.Single,
            DateTime => OracleDbType.Date,
            bool => OracleDbType.Int16,
            _ => OracleDbType.Varchar2,
        };
    }

    private static string FormatParams(Dictionary<string, object>? ps)
    {
        if (ps == null || ps.Count == 0) return "(无)";
        var sb = new StringBuilder();
        foreach (var kv in ps)
            sb.Append(kv.Key).Append('=')
              .Append(kv.Value?.ToString() ?? "NULL").Append("; ");
        return sb.ToString();
    }
    /// <summary>
    /// 在一个事务里执行多条 SQL。任意一条失败则整体回滚。使用时要求多个语句的绑定参数一致；
    /// </summary>
    /// <param name="statements">(SQL, 参数) 列表</param>
    /// <returns>所有语句影响行数之和</returns>
    public static int ExecuteInTransaction(
        List<(string Sql, Dictionary<string, object>? Params)> statements)
    {
        if (statements == null || statements.Count == 0) return 0;

        using var conn = GetConnection();
        using var tran = conn.BeginTransaction();

        try
        {
            int total = 0;

            foreach (var (sql, ps) in statements)
            {
                using var cmd = new OracleCommand(sql, conn)
                {
                    Transaction = tran,
                };

                if (ps != null)
                {
                    foreach (var kv in ps)
                    {
                        var p = cmd.Parameters.Add(kv.Key, InferDbType(kv.Value));
                        p.Value = kv.Value ?? DBNull.Value;
                    }
                }

                total += cmd.ExecuteNonQuery();
            }

            tran.Commit();
            return total;
        }
        catch (Exception ex)
        {
            try { tran.Rollback(); } catch { }

            string sqlList = string.Join("\n---\n", statements.Select(s => s.Sql.Trim()));
            throw new Exception(
                $"事务执行失败：{ex.Message}\nSQL:\n{sqlList}", ex);
        }
    }
    // ============ 通用取值辅助（DataRow → 类型） ============

    /// <summary>读取字符串。NULL → ""</summary>
    public static string GetStr(DataRow r, string col)
        => r.Table.Columns.Contains(col) && r[col] != DBNull.Value
            ? r[col].ToString()!.Trim()
            : string.Empty;

    /// <summary>读取 int。NULL 或转换失败 → 0</summary>
    public static int GetInt(DataRow r, string col)
    {
        if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return 0;
        return int.TryParse(r[col].ToString(), out var v) ? v : 0;
    }

    /// <summary>读取 long。NULL → 0</summary>
    public static long GetLong(DataRow r, string col)
    {
        if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return 0;
        return long.TryParse(r[col].ToString(), out var v) ? v : 0;
    }

    /// <summary>读取 decimal。NULL → 0</summary>
    public static decimal GetDec(DataRow r, string col)
    {
        if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return 0m;
        return decimal.TryParse(r[col].ToString(), out var v) ? v : 0m;
    }

    /// <summary>读取 double。NULL → 0</summary>
    public static double GetDouble(DataRow r, string col)
    {
        if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return 0d;
        return double.TryParse(r[col].ToString(), out var v) ? v : 0d;
    }

    /// <summary>读取 DateTime?。NULL → null</summary>
    public static DateTime? GetDate(DataRow r, string col)
    {
        if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
        return r[col] is DateTime dt ? dt : null;
    }

    /// <summary>读取 DateTime 格式化字符串。NULL → ""</summary>
    public static string GetDateStr(DataRow r, string col, string format = "yyyy/MM/dd HH:mm:ss")
    {
        var d = GetDate(r, col);
        return d?.ToString(format) ?? string.Empty;
    }
    /// <summary>
    /// 判断某表某列是否存在指定值。
    /// 注意：table 和 column 必须是代码里写死的常量，不能来自用户输入。
    /// </summary>
    public static bool Exists(string table, string column, string value)
    {
        string sql = $"SELECT COUNT(1) FROM {table} WHERE {column} = :v AND ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "v", value } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return false;
        return Convert.ToInt32(result) > 0;
    }
}