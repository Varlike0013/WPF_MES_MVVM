using System.Data;
using WPF_MES.Contracts.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 工单相关数据库操作。所有工单相关的 SQL 都在这里。
/// 产线、工序的查询已移到 OracleHelper。
/// </summary>
internal static class WorkOrderService
{
    /// <summary>
    /// 查询工单
    /// </summary>
    /// <param name="input">查询内容</param>
    /// <param name="inputType">0=工单 1=料号 2=流程 3=线别 4=序号</param>
    /// <param name="woStatus">0~6 状态码；7 或 -1 表示全部</param>
    public static List<WorkOrder> QueryWorkOrders(string input, int inputType, int woStatus)
    {
        string inputClause;
        switch (inputType)
        {
            case 0: inputClause = "W.WORK_ORDER = :input"; break;
            case 1: inputClause = "P.PART_NO = :input"; break;
            case 2: inputClause = "R.ROUTE_NAME = :input"; break;
            case 3: inputClause = "PD.PDLINE_NAME = :input"; break;
            case 4:
                inputClause =
                    "W.WORK_ORDER IN (SELECT DISTINCT T.WORK_ORDER " +
                    "FROM SAJET.G_SN_TRAVEL T WHERE T.SERIAL_NUMBER = :input)";
                break;
            default:
                throw new ArgumentException("未知的筛选类型：" + inputType);
        }

        bool needStatus = woStatus >= 0 && woStatus <= 6;
        string whereStr = "WHERE " + inputClause +
                          (needStatus ? " AND W.WO_STATUS = :status" : "");

        string sql = @"
            SELECT W.WORK_ORDER,
                   P.PART_NO,
                   W.WO_RULE,
                   W.VERSION,
                   W.WO_STATUS,
                   CASE WHEN W.WO_STATUS = 0 THEN 'initial'
                        WHEN W.WO_STATUS = 1 THEN 'prepare'
                        WHEN W.WO_STATUS = 2 THEN 'release'
                        WHEN W.WO_STATUS = 3 THEN 'work in process'
                        WHEN W.WO_STATUS = 4 THEN 'hold'
                        WHEN W.WO_STATUS = 5 THEN 'cancel'
                        WHEN W.WO_STATUS = 6 THEN 'complete'
                        ELSE 'unknown' END AS WO_STATUS_DESC,
                   W.TARGET_QTY,
                   W.INPUT_QTY,
                   W.OUTPUT_QTY,
                   W.ROUTE_ID,
                   R.ROUTE_NAME,
                   W.START_PROCESS_ID,
                   PE.PROCESS_NAME AS START_PROCESS,
                   W.END_PROCESS_ID,
                   PA.PROCESS_NAME AS END_PROCESS,
                   W.DEFAULT_PDLINE_ID,
                   PD.PDLINE_NAME
            FROM SAJET.G_WO_BASE W
            LEFT JOIN SAJET.SYS_PART    P  ON P.PART_ID  = W.MODEL_ID
            LEFT JOIN SAJET.SYS_ROUTE   R  ON R.ROUTE_ID = W.ROUTE_ID
            LEFT JOIN SAJET.SYS_PROCESS PE ON PE.PROCESS_ID = W.START_PROCESS_ID
            LEFT JOIN SAJET.SYS_PROCESS PA ON PA.PROCESS_ID = W.END_PROCESS_ID
            LEFT JOIN SAJET.SYS_PDLINE  PD ON PD.PDLINE_ID  = W.DEFAULT_PDLINE_ID
            " + whereStr + @"
            ORDER BY W.WORK_ORDER";

        var ps = new Dictionary<string, object> { { "input", input } };
        if (needStatus) ps["status"] = woStatus;

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<WorkOrder>();

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new WorkOrder
            {
                WorkOrderNo = GetStr(r, "WORK_ORDER"),
                PartNo = GetStr(r, "PART_NO"),
                WoRule = GetStr(r, "WO_RULE"),
                Version = GetStr(r, "VERSION"),
                StatusCode = GetInt(r, "WO_STATUS"),
                StatusDesc = GetStr(r, "WO_STATUS_DESC"),
                TargetQty = GetDec(r, "TARGET_QTY"),
                InputQty = GetDec(r, "INPUT_QTY"),
                OutputQty = GetDec(r, "OUTPUT_QTY"),

                RouteId = GetInt(r, "ROUTE_ID"),
                RouteName = GetStr(r, "ROUTE_NAME"),

                StartProcessId = GetInt(r, "START_PROCESS_ID"),
                StartProcess = GetStr(r, "START_PROCESS"),

                EndProcessId = GetInt(r, "END_PROCESS_ID"),
                EndProcess = GetStr(r, "END_PROCESS"),

                PdlineId = GetInt(r, "DEFAULT_PDLINE_ID"),
                PdlineName = GetStr(r, "PDLINE_NAME"),
            });
        }

        return list;
    }
    /// <summary>
    /// 更新工单状态、流程、起止工序、产线。
    /// 用 ID 更新，避免名称拼写不一致导致子查询返回 NULL。
    /// </summary>
    /// <returns>受影响行数；0 表示没找到</returns>
    public static int UpdateWorkOrder(string workOrderNo, int statusCode,
        int routeId, int startProcessId, int endProcessId, int pdlineId)
    {
        const string sql = @"
            UPDATE SAJET.G_WO_BASE
            SET WO_STATUS         = :status,
                ROUTE_ID          = :routeId,
                START_PROCESS_ID  = :startProcessId,
                END_PROCESS_ID    = :endProcessId,
                DEFAULT_PDLINE_ID = :pdlineId
            WHERE WORK_ORDER = :wo";

        var ps = new Dictionary<string, object>
        {
            { "status",         statusCode },
            { "routeId",        routeId },
            { "startProcessId", startProcessId },
            { "endProcessId",   endProcessId },
            { "pdlineId",       pdlineId },
            { "wo",             workOrderNo },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }
    /// <summary>
    /// 用名称更新工单。仅在调用方只有名称、没有 ID 时使用。
    /// 依赖 PDLINE_NAME / ROUTE_NAME / PROCESS_NAME 唯一。
    /// </summary>
    public static int UpdateWorkOrderByName(string workOrderNo, int statusCode,
        string routeName, string startProcess, string endProcess, string pdlineName)
    {
        const string sql = @"
            UPDATE SAJET.G_WO_BASE
            SET WO_STATUS         = :status,
                ROUTE_ID          = (SELECT ROUTE_ID   FROM SAJET.SYS_ROUTE   WHERE ROUTE_NAME   = :route),
                START_PROCESS_ID  = (SELECT PROCESS_ID FROM SAJET.SYS_PROCESS WHERE PROCESS_NAME = :startp),
                END_PROCESS_ID    = (SELECT PROCESS_ID FROM SAJET.SYS_PROCESS WHERE PROCESS_NAME = :endp),
                DEFAULT_PDLINE_ID = (SELECT PDLINE_ID  FROM SAJET.SYS_PDLINE  WHERE PDLINE_NAME  = :line)
            WHERE WORK_ORDER = :wo";

        var ps = new Dictionary<string, object>
        {
            { "status", statusCode },
            { "route",  routeName },
            { "startp", startProcess },
            { "endp",   endProcess },
            { "line",   pdlineName },
            { "wo",     workOrderNo },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    private static string GetStr(DataRow r, string col)
        => r.Table.Columns.Contains(col) && r[col] != DBNull.Value
            ? r[col].ToString()!.Trim()
            : string.Empty;

    private static int GetInt(DataRow r, string col)
        => r.Table.Columns.Contains(col) && r[col] != DBNull.Value
            ? Convert.ToInt32(r[col])
            : 0;

    private static decimal GetDec(DataRow r, string col)
        => r.Table.Columns.Contains(col) && r[col] != DBNull.Value
            ? Convert.ToDecimal(r[col])
            : 0m;
}