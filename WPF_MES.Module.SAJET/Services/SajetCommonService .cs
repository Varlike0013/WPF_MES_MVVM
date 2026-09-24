using Oracle.ManagedDataAccess.Client;
using System.Data;
using WPF_MES.Contracts.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// SAJET 通用业务查询
/// </summary>
internal static class SajetCommonService
{
    /// <summary>
    /// 调用存储过程 sj_chk_emp_pwd，返回 (是否成功, 原始返回消息)
    /// </summary>
    public static (bool ok, string msg) CheckEmpPwd(string empNo, string password)
    {
        using var conn = OracleHelper.GetConnection();
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

        var result = OracleHelper.ExecuteScalar(sql,
            new Dictionary<string, object> { { "userNo", userNo } });

        return result == null || result == DBNull.Value
            ? string.Empty
            : result.ToString()!.Trim();
    }
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

        var dt = OracleHelper.QueryDataTable(sql);
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

        var dt = OracleHelper.QueryDataTable(sql);
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
        var dt = OracleHelper.QueryDataTable(sql, ps);

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
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<(int, string)>();
        foreach (DataRow r in dt.Rows)
        {
            if (r[1] == DBNull.Value) continue;
            list.Add((Convert.ToInt32(r[0]), r[1].ToString()!.Trim()));
        }
        return list;
    }
    /// <summary>
    /// 获取所有启用的客户（ID + 编码 + 名称）
    /// </summary>
    public static List<CustomerInfo> GetCustomers()
    {
        const string sql = @"
        SELECT C.CUSTOMER_ID, C.CUSTOMER_CODE, C.CUSTOMER_NAME
        FROM SAJET.SYS_CUSTOMER C
        WHERE C.ENABLED = 'Y'
        ORDER BY C.CUSTOMER_NAME";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<CustomerInfo>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new CustomerInfo
            {
                CustomerId = OracleHelper.GetInt(r, "CUSTOMER_ID"),
                CustomerCode = OracleHelper.GetStr(r, "CUSTOMER_CODE"),
                CustomerName = OracleHelper.GetStr(r, "CUSTOMER_NAME"),
            });
        }

        return list;
    }
    /// <summary>
    /// 根据流程名获取工序列表
    /// </summary>
    public static List<ProcessInfo> GetProcessesByRoute(string routeName)
    {
        const string sql = @"
        SELECT U.PROCESS_ID, U.PROCESS_NAME
        FROM SAJET.SYS_ROUTE_DETAIL D
        INNER JOIN SAJET.SYS_ROUTE R ON D.ROUTE_ID = R.ROUTE_ID
        INNER JOIN SAJET.SYS_PROCESS U ON D.NEXT_PROCESS_ID = U.PROCESS_ID
        WHERE R.ROUTE_NAME = :route AND SEQ = STEP
        ORDER BY D.SEQ ASC";

        var ps = new Dictionary<string, object> { { "route", routeName } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<ProcessInfo>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new ProcessInfo(
                OracleHelper.GetInt(r, "PROCESS_ID"),
                OracleHelper.GetStr(r, "PROCESS_NAME")));
        }
        return list;
    }

    /// <summary>
    /// 根据工单查流程名
    /// </summary>
    public static string GetRouteByWorkOrder(string workOrder)
    {
        const string sql = @"
        SELECT R.ROUTE_NAME
        FROM SAJET.G_WO_BASE W
        LEFT JOIN SAJET.SYS_ROUTE R ON R.ROUTE_ID = W.ROUTE_ID
        WHERE W.WORK_ORDER = :wo";

        var ps = new Dictionary<string, object> { { "wo", workOrder } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        if (dt.Rows.Count == 0) return string.Empty;
        return OracleHelper.GetStr(dt.Rows[0], "ROUTE_NAME");
    }
    /// <summary>
    /// 获取工单的数量信息 (target, input, output)
    /// </summary>
    public static (int Target, int Input, int Output)? GetWoQty(string workOrder)
    {
        const string sql = @"
        SELECT TARGET_QTY, INPUT_QTY, OUTPUT_QTY
        FROM SAJET.G_WO_BASE
        WHERE WORK_ORDER = :wo";

        var ps = new Dictionary<string, object> { { "wo", workOrder } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        if (dt.Rows.Count == 0) return null;

        var r = dt.Rows[0];
        return (
            OracleHelper.GetInt(r, "TARGET_QTY"),
            OracleHelper.GetInt(r, "INPUT_QTY"),
            OracleHelper.GetInt(r, "OUTPUT_QTY"));
    }

    /// <summary>
    /// 增加工单的投入数量
    /// </summary>
    public static int AddInputQty(string workOrder, int qty)
    {
        const string sql = @"
        UPDATE SAJET.G_WO_BASE
        SET INPUT_QTY = INPUT_QTY + :qty
        WHERE WORK_ORDER = :wo";

        var ps = new Dictionary<string, object>
    {
        { "qty", qty },
        { "wo", workOrder },
    };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }
    /// <summary>
    /// 统计工单的 SN 总数（G_SN_STATUS + G_SN_TRAVEL 去重）
    /// </summary>
    public static int GetDistinctSnCount(string workOrder)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM (
                SELECT SERIAL_NUMBER FROM SAJET.G_SN_STATUS WHERE WORK_ORDER = :wo
                UNION
                SELECT SERIAL_NUMBER FROM SAJET.G_SN_TRAVEL WHERE WORK_ORDER = :wo
            )";

        var ps = new Dictionary<string, object> { { "wo", workOrder } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 更新工单的投入数量（覆盖）
    /// </summary>
    public static int UpdateInputQty(string workOrder, int qty)
    {
        const string sql = @"
            UPDATE SAJET.G_WO_BASE
            SET INPUT_QTY = :qty
            WHERE WORK_ORDER = :wo";

        var ps = new Dictionary<string, object>
        {
            { "qty", qty },
            { "wo", workOrder },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    /// <summary>
    /// 统计并覆盖更新工单投入数量
    /// </summary>
    /// <returns>更新后的数量</returns>
    public static int SyncInputQty(string workOrder)
    {
        int count = GetDistinctSnCount(workOrder);
        UpdateInputQty(workOrder, count);
        return count;
    }
    /// <summary>
    /// 根据工序名获取工序 ID。不存在返回 0。
    /// </summary>
    public static int GetProcessIdByName(string processName)
    {
        const string sql = @"
        SELECT PROCESS_ID FROM SAJET.SYS_PROCESS
        WHERE PROCESS_NAME = :name AND ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "name", processName } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 根据流程名获取流程 ID。不存在返回 0。
    /// </summary>
    public static int GetRouteIdByName(string routeName)
    {
        const string sql = @"
        SELECT ROUTE_ID FROM SAJET.SYS_ROUTE
        WHERE ROUTE_NAME = :name AND ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "name", routeName } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt32(result);
    }
    // ============ 条件存在性校验 ============
    public static bool ExistsSerialNumber(string sn)
    => OracleHelper.Exists("SAJET.G_SN_STATUS", "SERIAL_NUMBER", sn);
    public static bool ExistsCarton(string carton)
        => OracleHelper.Exists("SAJET.G_SN_STATUS", "CARTON_NO", carton);

    public static bool ExistsRework(string rework)
        => OracleHelper.Exists("SAJET.G_SN_STATUS", "REWORK_NO", rework);
    public static bool ExistsWorkOrder(string wo)
        => OracleHelper.Exists("SAJET.G_SN_STATUS", "WORK_ORDER", wo);
    public static bool ExistsQcNo(string qcNo)
        => OracleHelper.Exists("SAJET.G_SN_STATUS", "QC_NO", qcNo);

    /// <summary>
    /// 生成新重工号：RWV + YYMMDD + 4位序列。
    /// </summary>
    public static string GenerateNewReworkNo()
    {
        const string sql = "SELECT SAJET.SEQ_REVWORK.NEXTVAL FROM DUAL";
        var result = OracleHelper.ExecuteScalar(sql);

        if (result == null || result == DBNull.Value)
            throw new Exception("无法生成重工号序列");

        int seq = Convert.ToInt32(result);
        string today = DateTime.Now.ToString("yyMMdd");

        return $"RWV{today}{seq:D4}";
    }
    /// <summary>
    /// 根据员工工号获取 EMP_ID。不存在返回 -1。
    /// </summary>
    public static int GetEmpIdByNo(string empNo)
    {
        const string sql = @"
        SELECT EMP_ID FROM SAJET.SYS_EMP
        WHERE EMP_NO = :no AND ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "no", empNo } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return -1;
        return Convert.ToInt32(result);
    }
}