using System.Data;
using WPF_MES.Shared;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 流程查询相关操作
/// </summary>
internal static class CheckRouteService
{
    // ============ 固定的工序列表 ============

    private static readonly string[] FixedProcessList = new[]
    {
        "AOI", "BAOI", "BSPI", "BSVI", "PCB_INPUT", "SMT_INPUT", "SPI", "SVI",
        "S_AOI", "BottomVI", "CHANGE_SN", "CHECK_BAT", "CHECK_CPU", "CHECK_FAN",
        "DAOI", "DICT", "DInput", "DOA_VI", "FQC-CHK", "HEATSINK", "MDA", "PLATE",
        "TopVI", "CHK_LABEL1", "Cutboard", "F1Test", "F2Test", "F3Test", "F4Test",
        "GLUE_SVI", "Power On Test", "CHECKSN", "CHECK_BOX", "CHKPART", "CHKSSN",
        "CHK_MAC", "CHK_PART", "CQC", "ColorCheck", "OQC", "OQC_F1", "PACKING",
        "PBottomVI", "PK_AOI", "PK_VBATT", "PTopVI", "Packing1", "PrintLabel",
        "QC_CHK", "SOCPT0PVl"
    };

    // ============ 加载工序列表 ============

    /// <summary>
    /// 加载固定的工序列表（含站位段）
    /// </summary>
    public static List<ProcessItem> LoadProcessList()
    {
        // 构建 IN 参数 :p0, :p1, ...
        var paramNames = new List<string>();
        var ps = new Dictionary<string, object>();

        for (int i = 0; i < FixedProcessList.Length; i++)
        {
            string p = $"p{i}";
            paramNames.Add(":" + p);
            ps[p] = FixedProcessList[i];
        }

        string sql = $@"
            SELECT S.STAGE_NAME, P.PROCESS_NAME, P.PROCESS_ID
            FROM SAJET.SYS_PROCESS P
            JOIN SAJET.SYS_STAGE S ON S.STAGE_ID = P.STAGE_ID
            WHERE P.ENABLED = 'Y'
              AND P.PROCESS_NAME IN ({string.Join(",", paramNames)})
            ORDER BY P.STAGE_ID, P.PROCESS_NAME";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<ProcessItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new ProcessItem
            {
                StageName = OracleHelper.GetStr(r, "STAGE_NAME"),
                ProcessName = OracleHelper.GetStr(r, "PROCESS_NAME"),
                ProcessId = OracleHelper.GetInt(r, "PROCESS_ID"),
                State = false,
            });
        }
        return list;
    }

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

    // ============ 查询某流程的步骤 ============

    /// <summary>
    /// 获取指定流程的所有步骤
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

    // ============ 按"必过/必不过"查询流程 ============

    /// <summary>
    /// 根据必过、必不过工序查询符合条件的流程
    /// </summary>
    /// <param name="mustHave">必过工序名列表（不能为空）</param>
    /// <param name="mustNot">必不过工序名列表（可为空）</param>
    public static List<string> QueryRoutesByProcess(
        List<string> mustHave,
        List<string> mustNot)
    {
        if (mustHave.Count == 0)
            return new List<string>();

        // 构建参数
        var ps = new Dictionary<string, object>();
        int idx = 0;

        // 必过参数
        var mhNames = new List<string>();
        foreach (var p in mustHave)
        {
            string name = $"mh{idx++}";
            mhNames.Add(":" + name);
            ps[name] = p;
        }

        // 必不过参数
        var mnNames = new List<string>();
        foreach (var p in mustNot)
        {
            string name = $"mn{idx++}";
            mnNames.Add(":" + name);
            ps[name] = p;
        }

        string mhStr = string.Join(",", mhNames);
        string mnStr = mnNames.Count > 0 ? string.Join(",", mnNames) : "''";

        string mhCountValue = mustHave.Count.ToString();

        string sql = $@"
            SELECT R.ROUTE_NAME
            FROM SAJET.SYS_ROUTE R
            INNER JOIN SAJET.SYS_ROUTE_DETAIL D ON R.ROUTE_ID = D.ROUTE_ID
            INNER JOIN SAJET.SYS_PROCESS P ON D.PROCESS_ID = P.PROCESS_ID
            GROUP BY R.ROUTE_NAME
            HAVING
                COUNT(DISTINCT CASE WHEN P.PROCESS_NAME IN ({mhStr}) THEN P.PROCESS_NAME END) = {mhCountValue}
                AND COUNT(DISTINCT CASE WHEN P.PROCESS_NAME IN ({mnStr}) THEN P.PROCESS_NAME END) = 0
            ORDER BY R.ROUTE_NAME";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<string>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            if (r[0] != DBNull.Value)
                list.Add(r[0].ToString()!.Trim());
        }
        return list;
    }

    // ============ 复制流程（调存储过程） ============

    /// <summary>
    /// 调用存储过程复制流程
    /// </summary>
    /// <param name="oldRoute">源流程名</param>
    /// <param name="newRoute">新流程名</param>
    /// <param name="empNo">操作员工号</param>
    /// <param name="overwrite">是否覆盖：Y/N</param>
    /// <returns>存储过程返回的结果消息</returns>
    public static string CopyRoute(
        string oldRoute,
        string newRoute,
        string empNo,
        string overwrite)
    {
        using var conn = OracleHelper.GetConnection();
        using var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(
            "SAJET.SJ_COPY_ROUTE", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };

        cmd.Parameters.Add("old",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = oldRoute;
        cmd.Parameters.Add("new",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = newRoute;
        cmd.Parameters.Add("emp",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = empNo;
        cmd.Parameters.Add("overwrite",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = overwrite;

        var outParam = new Oracle.ManagedDataAccess.Client.OracleParameter(
            "result",
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