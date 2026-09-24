using System.Data;
using WPF_MES.Contracts.Models;
using WPF_MES.Contracts.Models.SnItem;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 重工相关数据库操作
/// </summary>
internal static class ReworkService
{
    // ============ 条件列名映射 ============

    private static readonly Dictionary<ConditionType, string> ColumnMap = new()
    {
        { ConditionType.SerialNumber, "S.SERIAL_NUMBER" },
        { ConditionType.Carton,       "S.CARTON_NO" },
        { ConditionType.Rework,       "S.REWORK_NO" },
        { ConditionType.WorkOrder,    "S.WORK_ORDER" },
        { ConditionType.QcNo,         "S.QC_NO" },
    };

    /// <summary>
    /// 根据条件构建 WHERE 子句（OR 连接多个 IN）
    /// </summary>
    public static (string Where, Dictionary<string, object> Ps) BuildWhere(
        ConditionManager conditions)
    {
        var parts = new List<string>();
        var ps = new Dictionary<string, object>();
        int idx = 0;

        foreach (var kv in conditions.GetAll())
        {
            if (kv.Value.Count == 0) continue;
            if (!ColumnMap.TryGetValue(kv.Key, out var col)) continue;

            var paramNames = new List<string>();
            foreach (var v in kv.Value)
            {
                string p = $"c{idx++}";
                paramNames.Add(":" + p);
                ps[p] = v;
            }

            parts.Add($"{col} IN ({string.Join(",", paramNames)})");
        }

        return (string.Join(" OR ", parts), ps);
    }

    // ============ 查询 SN 列表 ============

    /// <summary>
    /// 查询符合条件的 SN 列表
    /// </summary>
    public static List<ReworkSnItem> QuerySnItems(string whereClause,
        Dictionary<string, object> ps)
    {
        string sql = $@"
            SELECT S.SERIAL_NUMBER, S.WORK_ORDER, P.PART_NO, L.PDLINE_NAME,
                   P1.PROCESS_NAME AS WIP_PROCESS,
                   P2.PROCESS_NAME AS CURRENT_PROCESS,
                   T.TERMINAL_NAME, S.REWORK_NO,
                   S.CUSTOMER_SN, S.PALLET_NO, S.CARTON_NO, S.CONTAINER,
                   TO_CHAR(S.OUT_PDLINE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS OUT_PDLINE_TIME,
                   R.ROUTE_NAME
            FROM SAJET.G_SN_STATUS S
            LEFT JOIN SAJET.SYS_PART     P  ON P.PART_ID     = S.MODEL_ID
            LEFT JOIN SAJET.SYS_PDLINE   L  ON L.PDLINE_ID   = S.PDLINE_ID
            LEFT JOIN SAJET.SYS_PROCESS  P1 ON P1.PROCESS_ID = S.WIP_PROCESS
            LEFT JOIN SAJET.SYS_PROCESS  P2 ON P2.PROCESS_ID = S.PROCESS_ID
            LEFT JOIN SAJET.SYS_TERMINAL T  ON T.TERMINAL_ID = S.TERMINAL_ID
            LEFT JOIN SAJET.SYS_ROUTE    R  ON R.ROUTE_ID    = S.ROUTE_ID
            WHERE S.CURRENT_STATUS = 0 AND S.WORK_FLAG = 0
              AND ({whereClause})
            ORDER BY S.SERIAL_NUMBER";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<ReworkSnItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
            list.Add(MapRow(r));

        return list;
    }

    public static int ExecuteRework(
        string whereClause,
        Dictionary<string, object> ps,
        int routeId,
        int processId,
        string? newWorkOrder,
        string reworkNo,
        string empNo,
        string? condition,
        string? remark,
        bool clearCustomerSN,
        bool clearMac,
        bool clearPack,
        bool clearQc,
        bool clearParts)
    {
        // ============ 0. 校验 + 生成重工号 ============

        if (string.IsNullOrWhiteSpace(empNo))
            throw new Exception("操作员工号不能为空");

        int empId = SajetCommonService.GetEmpIdByNo(empNo);
        if (empId == 0)
            throw new Exception($"员工工号 [{empNo}] 不存在");

        Logger.Info($"[REWORK] Execute start: reworkNo={reworkNo}, empNo={empNo}");

        // ============ 1. 构建 SET 子句 ============

        var sets = new List<string>
    {
        "S.ROUTE_ID    = :routeId",
        "S.WIP_PROCESS = :processId",
        "S.REWORK_NO   = :reworkNo",
    };

        ps["routeId"] = routeId;
        ps["processId"] = processId;
        ps["reworkNo"] = reworkNo;

        bool hasNewWo = !string.IsNullOrWhiteSpace(newWorkOrder);
        if (hasNewWo)
        {
            sets.Add("S.WORK_ORDER = :newWo");
            ps["newWo"] = newWorkOrder!;
        }

        if (clearCustomerSN) sets.Add("S.CUSTOMER_SN = 'N/A'");
        if (clearPack) sets.Add("S.CARTON_NO = 'N/A', S.PALLET_NO = 'N/A', S.CONTAINER = 'N/A'");
        if (clearQc) sets.Add("S.QC_NO = 'N/A', S.QC_RESULT = 'N/A'");

        string setClause = string.Join(", ", sets);

        var statements = new List<(string, Dictionary<string, object>?)>();

        // ============ 2. 备份 SN 状态（更新前） ============
        string updateReworkNoSql = $@"
            UPDATE SAJET.G_SN_STATUS S
            SET S.REWORK_NO = :reworkNo
            WHERE S.CURRENT_STATUS = 0 AND S.WORK_FLAG = 0
              AND ({whereClause})";

        statements.Add((updateReworkNoSql, ps));

        string backupSql = $@"
        INSERT INTO SAJET.G_REWORK_LOG
        SELECT S.*
        FROM SAJET.G_SN_STATUS S
        WHERE S.CURRENT_STATUS = 0 AND S.WORK_FLAG = 0
          AND ({whereClause})";

        statements.Add((backupSql, ps));

        // ============ 3. 主 UPDATE ============

        string mainSql = $@"
        UPDATE SAJET.G_SN_STATUS S
        SET {setClause}
        WHERE S.CURRENT_STATUS = 0 AND S.WORK_FLAG = 0
          AND ({whereClause})";

        statements.Add((mainSql, ps));

        // ============ 4. 插入 G_REWORK_NO（1 条） ============

        string logSql = @"
        INSERT INTO SAJET.G_REWORK_NO 
            (CONDITION, EMP_ID, REMARK, REWORK_NO, UPDATE_TIME)
        VALUES 
            (:condition, :empId, :remark, :reworkNo, SYSDATE)";

        var logPs = new Dictionary<string, object>
    {
        { "condition", condition ?? string.Empty },
        { "empId",     empId },
        { "remark",    remark ?? string.Empty },
        { "reworkNo",  reworkNo },
    };

        statements.Add((logSql, logPs));

        // ============ 5. 清除料件（备份 + 删除） ============

        if (clearParts)
        {
            string insertParts = $@"
            INSERT INTO SAJET.G_HT_SN_KEYPARTS
            SELECT K.*
            FROM SAJET.G_SN_KEYPARTS K
            WHERE K.SERIAL_NUMBER IN (
                SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
                WHERE S.CURRENT_STATUS = 0 AND S.WORK_FLAG = 0
                  AND ({whereClause})
            )";

            statements.Add((insertParts, ps));

            string delParts = $@"
            DELETE FROM SAJET.G_SN_KEYPARTS
            WHERE SERIAL_NUMBER IN (
                SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
                WHERE S.CURRENT_STATUS = 0 AND S.WORK_FLAG = 0
                  AND ({whereClause})
            )";

            statements.Add((delParts, ps));
        }

        // ============ 6. 清除 MAC（备份 + 删除） ============

        if (clearMac)
        {
            string insertMac = $@"
            INSERT INTO SAJET.G_HT_WO_MAC
            SELECT M.*
            FROM SAJET.G_WO_MAC M
            WHERE M.SERIAL_NUMBER IN (
                SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
                WHERE S.WIP_PROCESS = (
                    SELECT PROCESS_ID FROM SAJET.SYS_PROCESS 
                    WHERE PROCESS_NAME = 'F1Test'
                )
                  AND ({whereClause})
            )";

            statements.Add((insertMac, ps));

            string delMac = $@"
            DELETE FROM SAJET.G_WO_MAC
            WHERE SERIAL_NUMBER IN (
                SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
                WHERE S.WIP_PROCESS = (
                    SELECT PROCESS_ID FROM SAJET.SYS_PROCESS 
                    WHERE PROCESS_NAME = 'F1Test'
                )
                  AND ({whereClause})
            )";

            statements.Add((delMac, ps));
        }

        // ============ 7. 执行事务 ============

        int affected = OracleHelper.ExecuteInTransaction(statements);

        Logger.Info($"[REWORK] Execute OK: reworkNo={reworkNo}, affected={affected}");

        return affected;
    }
    public static ReworkSnItem MapRow(DataRow r)
    {
        return new ReworkSnItem
        {
            SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),
            WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
            PartNo = OracleHelper.GetStr(r, "PART_NO"),
            PdlineName = OracleHelper.GetStr(r, "PDLINE_NAME"),
            WipProcess = OracleHelper.GetStr(r, "WIP_PROCESS"),
            CurrentProcess = OracleHelper.GetStr(r, "CURRENT_PROCESS"),
            TerminalName = OracleHelper.GetStr(r, "TERMINAL_NAME"),
            CustomerSN = OracleHelper.GetStr(r, "CUSTOMER_SN"),
            PalletNo = OracleHelper.GetStr(r, "PALLET_NO"),
            CartonNo = OracleHelper.GetStr(r, "CARTON_NO"),
            Container = OracleHelper.GetStr(r, "CONTAINER"),
            OutPdlineTime = OracleHelper.GetStr(r, "OUT_PDLINE_TIME"),
            RouteName = OracleHelper.GetStr(r, "ROUTE_NAME"),
            ReworkNo = OracleHelper.GetStr(r, "REWORK_NO"),
        };
    }
}