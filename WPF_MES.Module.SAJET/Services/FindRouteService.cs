using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 重工流程查询相关操作
/// </summary>
internal static class FindRouteService
{
    // ============ 加载所有启用的流程 ============

    public static List<string> LoadAllRoutes()
    {
        const string sql = @"
            SELECT ROUTE_NAME FROM SAJET.SYS_ROUTE
            WHERE ENABLED = 'Y'
            ORDER BY ROUTE_NAME";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<string>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            if (r[0] != DBNull.Value)
                list.Add(r[0].ToString()!.Trim());
        }
        return list;
    }

    // ============ 根据 SN 查走过的流程 ============

    /// <summary>
    /// 查询某个 SN 走过的流程（去重）
    /// </summary>
    public static List<RouteItem> GetRoutesBySn(string sn)
    {
        const string sql = @"
            SELECT R.ROUTE_ID, R.ROUTE_NAME
            FROM SAJET.SYS_ROUTE R
            WHERE R.ROUTE_ID IN (
                SELECT ROUTE_ID FROM SAJET.G_SN_TRAVEL
                WHERE SERIAL_NUMBER = :sn
                GROUP BY ROUTE_ID
            )
            ORDER BY R.ROUTE_NAME";

        var ps = new Dictionary<string, object> { { "sn", sn } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<RouteItem>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new RouteItem
            {
                RouteId = OracleHelper.GetInt(r, "ROUTE_ID"),
                RouteName = OracleHelper.GetStr(r, "ROUTE_NAME"),
                IsChecked = false,
            });
        }
        return list;
    }

    // ============ 根据 DIP + PACK 查合并后的重工流程 ============

    /// <summary>
    /// 查询满足"包含 DIP 和 PACK 所有必过工序"的重工流程
    /// </summary>
    public static List<string> QueryCombinedRoutes(string dip, string pack)
    {
        const string sql = @"
            WITH valid_processes AS (
                SELECT DISTINCT D.NEXT_PROCESS_ID
                FROM SAJET.SYS_ROUTE_DETAIL D
                JOIN SAJET.SYS_ROUTE R ON D.ROUTE_ID = R.ROUTE_ID
                WHERE R.ROUTE_NAME IN (:dip, :pack)
                  AND D.NECESSARY = 'Y'
                  AND D.SEQ = D.STEP
            )
            SELECT R.ROUTE_NAME
            FROM SAJET.SYS_ROUTE R
            JOIN SAJET.SYS_ROUTE_DETAIL D ON R.ROUTE_ID = D.ROUTE_ID
            JOIN SAJET.SYS_PROCESS P ON D.PROCESS_ID = P.PROCESS_ID
            WHERE D.PROCESS_ID IN (SELECT NEXT_PROCESS_ID FROM valid_processes)
            GROUP BY R.ROUTE_NAME
            HAVING COUNT(DISTINCT P.PROCESS_NAME) = (SELECT COUNT(*) FROM valid_processes)
            ORDER BY R.ROUTE_NAME";

        var ps = new Dictionary<string, object>
        {
            { "dip", dip },
            { "pack", pack },
        };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<string>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            if (r[0] != DBNull.Value)
                list.Add(r[0].ToString()!.Trim());
        }
        return list;
    }

    // ============ 查某流程的所有步骤 ============

    /// <summary>
    /// 获取某流程的所有步骤
    /// </summary>
    public static List<RouteStepItem> GetRouteSteps(string routeName)
    {
        const string sql = @"
            SELECT U.PROCESS_NAME, D.NECESSARY
            FROM SAJET.SYS_ROUTE_DETAIL D
            INNER JOIN SAJET.SYS_ROUTE R ON D.ROUTE_ID = R.ROUTE_ID
            INNER JOIN SAJET.SYS_PROCESS U ON D.NEXT_PROCESS_ID = U.PROCESS_ID
            WHERE R.ROUTE_NAME = :route AND SEQ = STEP
            ORDER BY D.SEQ ASC";

        var ps = new Dictionary<string, object> { { "route", routeName } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<RouteStepItem>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new RouteStepItem
            {
                ProcessName = OracleHelper.GetStr(r, "PROCESS_NAME"),
                Necessary = OracleHelper.GetStr(r, "NECESSARY"),
            });
        }
        return list;
    }

    // ============ 添加为新流程（调存储过程） ============

    /// <summary>
    /// 调用 SJ_INSERT_R_ROUTE 存储过程，合并 DIP + PACK 为新流程。
    /// </summary>
    /// <returns>存储过程返回的结果消息</returns>
    public static string AddAsNewRoute(
        string dip,
        string pack,
        string newRoute,
        string empNo)
    {
        using var conn = OracleHelper.GetConnection();
        using var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(
            "SAJET.SJ_INSERT_R_ROUTE", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };

        cmd.Parameters.Add("dip",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = dip;
        cmd.Parameters.Add("pack",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = pack;
        cmd.Parameters.Add("router",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = newRoute;
        cmd.Parameters.Add("emp_no",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = empNo;

        var outParam = new Oracle.ManagedDataAccess.Client.OracleParameter(
            "result_msg",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2,
            500)
        {
            Direction = ParameterDirection.Output,
        };
        cmd.Parameters.Add(outParam);

        cmd.ExecuteNonQuery();

        return outParam.Value?.ToString()?.Trim() ?? string.Empty;
    }
}