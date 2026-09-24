using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 箱号管理相关操作
/// </summary>
internal static class CartonInfoService
{
    // ============ 查询 ============

    /// <summary>
    /// 按箱号/工单条件查询箱号信息
    /// </summary>
    /// <param name="cartons">箱号列表</param>
    /// <param name="workOrders">工单列表</param>
    public static List<CartonInfoItem> QueryCartons(
        List<string> cartons,
        List<string> workOrders)
    {
        if (cartons.Count == 0 && workOrders.Count == 0)
            return new List<CartonInfoItem>();

        // 构建 WHERE 参数
        var conditions = new List<string>();
        var ps = new Dictionary<string, object>();
        int idx = 0;

        if (workOrders.Count > 0)
        {
            var paramNames = new List<string>();
            foreach (var wo in workOrders)
            {
                string p = $"w{idx++}";
                paramNames.Add(":" + p);
                ps[p] = wo;
            }
            conditions.Add($"W.WORK_ORDER IN ({string.Join(",", paramNames)})");
        }

        if (cartons.Count > 0)
        {
            var paramNames = new List<string>();
            foreach (var c in cartons)
            {
                string p = $"c{idx++}";
                paramNames.Add(":" + p);
                ps[p] = c;
            }
            conditions.Add($"W.CARTON_NO IN ({string.Join(",", paramNames)})");
        }

        string whereStr = string.Join(" OR ", conditions);

        string sql = $@"
            SELECT W.WORK_ORDER,
                   E1.EMP_NAME AS UPDATE_EMP,
                   TO_CHAR(W.UPDATE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS UPDATE_TIME,
                   W.QTY,
                   C.WORK_ORDER AS PACK_ORDER,
                   P.PART_NO,
                   W.CARTON_NO,
                   C.CLOSE_FLAG,
                   CASE C.CLOSE_FLAG
                       WHEN 'Y' THEN '关闭'
                       WHEN 'N' THEN '打开'
                       WHEN 'K' THEN '缴库'
                       ELSE C.CLOSE_FLAG
                   END AS CLOSE_FLAG_DESC,
                   T.TERMINAL_NAME,
                   E.EMP_NAME AS CREATE_EMP,
                   TO_CHAR(C.CREATE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS CREATE_TIME
            FROM SAJET.G_WO_CARTON W
            LEFT JOIN SAJET.G_PACK_CARTON C ON W.CARTON_NO = C.CARTON_NO
            LEFT JOIN SAJET.SYS_PART P ON P.PART_ID = C.MODEL_ID
            LEFT JOIN SAJET.SYS_TERMINAL T ON T.TERMINAL_ID = C.TERMINAL_ID
            LEFT JOIN SAJET.SYS_EMP E ON E.EMP_ID = C.CREATE_EMP_ID
            LEFT JOIN SAJET.SYS_EMP E1 ON E1.EMP_ID = W.UPDATE_USERID
            WHERE {whereStr}
            ORDER BY W.CARTON_NO";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<CartonInfoItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
            list.Add(MapRow(r));

        return list;
    }

    // ============ 更新单条 ============

    /// <summary>
    /// 更新箱号信息（工单、包装工单、数量、状态）。
    /// 两张表用事务保证一致。
    /// </summary>
    /// <param name="cartonNo">箱号（条件）</param>
    /// <param name="newWorkOrder">新工单</param>
    /// <param name="newPackWorkOrder">新包装工单</param>
    /// <param name="newQty">新数量</param>
    /// <param name="newStatus">新状态 N/Y/K</param>
    /// <param name="oldWorkOrder">原工单（判断是否需要更新）</param>
    /// <param name="oldPackWorkOrder">原包装工单</param>
    /// <param name="oldQty">原数量</param>
    /// <param name="oldStatus">原状态</param>
    /// <returns>受影响行数之和</returns>
    public static int UpdateCarton(
        string cartonNo,
        string newWorkOrder,
        string newPackWorkOrder,
        int newQty,
        string newStatus,
        string oldWorkOrder,
        string oldPackWorkOrder,
        int oldQty,
        string oldStatus)
    {
        var statements = new List<(string, Dictionary<string, object>?)>();

        bool woChanged = newWorkOrder != oldWorkOrder;
        bool qtyChanged = newQty != oldQty;
        bool packWoChanged = newPackWorkOrder != oldPackWorkOrder;
        bool statusChanged = newStatus != oldStatus;

        // ---------- 更新 G_WO_CARTON（工单、数量） ----------
        if (woChanged || qtyChanged)
        {
            var setClauses = new List<string>();
            var ps = new Dictionary<string, object>();

            if (woChanged)
            {
                setClauses.Add("WORK_ORDER = :wo");
                ps["wo"] = newWorkOrder;
            }
            if (qtyChanged)
            {
                setClauses.Add("QTY = :qty");
                ps["qty"] = newQty;
            }

            ps["carton"] = cartonNo;

            string sql = $@"
                UPDATE SAJET.G_WO_CARTON
                SET {string.Join(", ", setClauses)}
                WHERE CARTON_NO = :carton";

            statements.Add((sql, ps));
        }

        // ---------- 更新 G_PACK_CARTON（包装工单、状态） ----------
        if (packWoChanged || statusChanged)
        {
            var setClauses = new List<string>();
            var ps = new Dictionary<string, object>();

            if (packWoChanged)
            {
                setClauses.Add("WORK_ORDER = :packWo");
                ps["packWo"] = newPackWorkOrder;
            }
            if (statusChanged)
            {
                setClauses.Add("CLOSE_FLAG = :status");
                ps["status"] = newStatus;
            }

            ps["carton"] = cartonNo;

            string sql = $@"
                UPDATE SAJET.G_PACK_CARTON
                SET {string.Join(", ", setClauses)}
                WHERE CARTON_NO = :carton";

            statements.Add((sql, ps));
        }

        if (statements.Count == 0) return 0;

        return OracleHelper.ExecuteInTransaction(statements);
    }

    // ============ 批量更新状态 ============

    /// <summary>
    /// 根据箱号/工单条件，批量更新状态
    /// </summary>
    /// <param name="cartons">箱号列表</param>
    /// <param name="workOrders">工单列表</param>
    /// <param name="newStatus">新状态 N/Y/K</param>
    /// <returns>受影响行数</returns>
    public static int BatchUpdateStatus(
        List<string> cartons,
        List<string> workOrders,
        string newStatus)
    {
        if (cartons.Count == 0 && workOrders.Count == 0) return 0;

        // 构建 W 表的 WHERE
        var conditions = new List<string>();
        var ps = new Dictionary<string, object>();
        int idx = 0;

        if (workOrders.Count > 0)
        {
            var names = new List<string>();
            foreach (var wo in workOrders)
            {
                string p = $"w{idx++}";
                names.Add(":" + p);
                ps[p] = wo;
            }
            conditions.Add($"W.WORK_ORDER IN ({string.Join(",", names)})");
        }

        if (cartons.Count > 0)
        {
            var names = new List<string>();
            foreach (var c in cartons)
            {
                string p = $"c{idx++}";
                names.Add(":" + p);
                ps[p] = c;
            }
            conditions.Add($"W.CARTON_NO IN ({string.Join(",", names)})");
        }

        string whereStr = string.Join(" OR ", conditions);
        ps["status"] = newStatus;

        string sql = $@"
            UPDATE SAJET.G_PACK_CARTON C
            SET C.CLOSE_FLAG = :status
            WHERE C.CARTON_NO IN (
                SELECT W.CARTON_NO FROM SAJET.G_WO_CARTON W
                WHERE {whereStr}
            )";

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 箱号退出 QC（调存储过程） ============

    /// <summary>
    /// 调用存储过程：箱号退出 QC
    /// </summary>
    /// <returns>存储过程返回的消息</returns>
    public static string ExitQcCarton(string cartonNo)
    {
        using var conn = OracleHelper.GetConnection();
        using var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(
            "SAJET.SJ_CHK_WQC_CARTON", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };

        cmd.Parameters.Add("trev",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = cartonNo;

        var outParam = new Oracle.ManagedDataAccess.Client.OracleParameter(
            "tres",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2,
            4000)
        {
            Direction = ParameterDirection.Output,
        };
        cmd.Parameters.Add(outParam);

        cmd.ExecuteNonQuery();

        return outParam.Value?.ToString()?.Trim() ?? string.Empty;
    }

    // ============ 私有：映射 ============

    private static CartonInfoItem MapRow(DataRow r)
    {
        return new CartonInfoItem
        {
            WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
            UpdateEmp = OracleHelper.GetStr(r, "UPDATE_EMP"),
            UpdateTime = OracleHelper.GetStr(r, "UPDATE_TIME"),
            Qty = OracleHelper.GetInt(r, "QTY"),
            PackWorkOrder = OracleHelper.GetStr(r, "PACK_ORDER"),
            PartNo = OracleHelper.GetStr(r, "PART_NO"),
            CartonNo = OracleHelper.GetStr(r, "CARTON_NO"),
            CloseFlag = OracleHelper.GetStr(r, "CLOSE_FLAG"),
            StatusDesc = OracleHelper.GetStr(r, "CLOSE_FLAG_DESC"),
            TerminalName = OracleHelper.GetStr(r, "TERMINAL_NAME"),
            CreateEmp = OracleHelper.GetStr(r, "CREATE_EMP"),
            CreateTime = OracleHelper.GetStr(r, "CREATE_TIME"),
        };
    }
}