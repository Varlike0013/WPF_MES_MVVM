using System.Data;
using System.Text;
using Oracle.ManagedDataAccess.Client;

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

    /// <summary>
    /// 调用存储过程 sj_chk_emp_pwd，返回 (是否成功, 原始返回消息)
    /// </summary>
    public static (bool ok, string msg) CheckEmpPwd(string empNo, string password)
    {
        using var conn = GetConnection();
        using var cmd = new OracleCommand("SAJET.sj_chk_emp_pwd", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("temp2", OracleDbType.Varchar2).Value = empNo;
        cmd.Parameters.Add("trev", OracleDbType.Varchar2).Value = password;

        var outParam = new OracleParameter("tres", OracleDbType.Varchar2, 200)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(outParam);

        cmd.ExecuteNonQuery();

        string msg = outParam.Value?.ToString()?.Trim() ?? string.Empty;
        bool ok = msg.StartsWith("OK", StringComparison.OrdinalIgnoreCase);
        return (ok, msg);
    }

    /// <summary>
    /// 通过工号获取姓名
    /// </summary>
    public static string GetUserName(string userNo)
    {
        const string sql = @"
            SELECT EMP_NAME FROM SYS_EMP
            WHERE EMP_NO = :userNo AND ENABLED = 'Y'";

        var result = ExecuteScalar(sql,
            new Dictionary<string, object> { { "userNo", userNo } });

        return result == null || result == DBNull.Value
            ? string.Empty
            : result.ToString()!.Trim();
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
    // ============ 共用业务查询 ============

    /// <summary>
    /// 获取所有启用的产线（ID + 名称）
    /// </summary>
    public static List<(int Id, string Name)> GetPDLines()
    {
        const string sql = @"
        SELECT PDLINE_ID, PDLINE_NAME
        FROM SAJET.SYS_PDLINE
        WHERE ENABLED = 'Y'
        ORDER BY PDLINE_NAME";

        var dt = QueryDataTable(sql);
        var list = new List<(int, string)>();
        foreach (DataRow r in dt.Rows)
        {
            if (r[1] == DBNull.Value) continue;
            list.Add((Convert.ToInt32(r[0]), r[1].ToString()!.Trim()));
        }
        return list;
    }
    /// <summary>
    /// 获取所有启用的产线（ID + 名称）
    /// </summary>
    public static List<(int Id, string Name)> GetPDLinesLikeBfloor()
    {
        const string sql = @"
        SELECT PDLINE_ID, PDLINE_NAME
        FROM SAJET.SYS_PDLINE
        WHERE ENABLED = 'Y' AND PDLINE_NAME LIKE 'B%'
        ORDER BY PDLINE_NAME";

        var dt = QueryDataTable(sql);
        var list = new List<(int, string)>();
        foreach (DataRow r in dt.Rows)
        {
            if (r[1] == DBNull.Value) continue;
            list.Add((Convert.ToInt32(r[0]), r[1].ToString()!.Trim()));
        }
        return list;
    }

    /// <summary>
    /// 根据流程 ID 获取该流程下的工序（ID + 名称）
    /// </summary>
    public static List<(int Id, string Name)> GetRouteProcesses(int routeId)
    {
        const string sql = @"
        SELECT P.PROCESS_ID, P.PROCESS_NAME
        FROM SAJET.SYS_ROUTE_DETAIL RD
        LEFT JOIN SAJET.SYS_PROCESS P ON P.PROCESS_ID = RD.NEXT_PROCESS_ID
        WHERE RD.ROUTE_ID = :routeId
          AND RD.SEQ = RD.STEP
        ORDER BY RD.STEP";

        var ps = new Dictionary<string, object> { { "routeId", routeId } };
        var dt = QueryDataTable(sql, ps);

        var list = new List<(int, string)>();
        foreach (DataRow r in dt.Rows)
        {
            if (r[1] == DBNull.Value) continue;
            list.Add((Convert.ToInt32(r[0]), r[1].ToString()!.Trim()));
        }
        return list;
    }
    /// <summary>
    /// 根据流程名称获取该流程下的工序（ID + 名称）
    /// </summary>
    public static List<(int Id, string Name)> GetRouteProcesses(string routeName)
    {
        const string sql = @"
        SELECT P.PROCESS_ID, P.PROCESS_NAME
        FROM SAJET.SYS_ROUTE_DETAIL RD
        LEFT JOIN SAJET.SYS_PROCESS P ON P.PROCESS_ID = RD.NEXT_PROCESS_ID
        WHERE RD.ROUTE_ID = (
            SELECT R.ROUTE_ID FROM SAJET.SYS_ROUTE R WHERE R.ROUTE_NAME = :route
        )
        AND RD.SEQ = RD.STEP
        ORDER BY RD.STEP";

        var ps = new Dictionary<string, object> { { "route", routeName } };
        var dt = QueryDataTable(sql, ps);

        var list = new List<(int, string)>();
        foreach (DataRow r in dt.Rows)
        {
            if (r[1] == DBNull.Value) continue;
            list.Add((Convert.ToInt32(r[0]), r[1].ToString()!.Trim()));
        }
        return list;
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
}