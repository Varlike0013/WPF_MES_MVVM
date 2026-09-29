using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// MAC 管理相关操作
/// </summary>
internal static class CheckMacService
{
    /// <summary>
    /// 删除时限定在制工序的固定值
    /// </summary>
    private const string DeleteWipProcess = "200018"; //F1Test

    // ============ 查询 ============

    /// <summary>
    /// 按 SN / REWORK_NO 条件查询 MAC 信息
    /// </summary>
    public static List<MacInfoItem> QueryMacs(
        List<string> serials,
        List<string> reworks)
    {
        if (serials.Count == 0 && reworks.Count == 0)
            return new List<MacInfoItem>();

        // 构建 WHERE 参数
        var conditions = new List<string>();
        var ps = new Dictionary<string, object>();
        int idx = 0;

        if (serials.Count > 0)
        {
            var names = new List<string>();
            foreach (var sn in serials)
            {
                string p = $"s{idx++}";
                names.Add(":" + p);
                ps[p] = sn;
            }
            conditions.Add($"S.SERIAL_NUMBER IN ({string.Join(",", names)})");
        }

        if (reworks.Count > 0)
        {
            var names = new List<string>();
            foreach (var rw in reworks)
            {
                string p = $"r{idx++}";
                names.Add(":" + p);
                ps[p] = rw;
            }
            conditions.Add($"S.REWORK_NO IN ({string.Join(",", names)})");
        }

        string whereStr = string.Join(" OR ", conditions);

        string sql = $@"
            SELECT M.WORK_ORDER,
                   M.SERIAL_NUMBER,
                   M.MAC,
                   NVL(P.PROCESS_NAME, 'None') AS CURRENT_PROCESS,
                   E.EMP_NAME AS UPDATE_EMP,
                   TO_CHAR(M.UPDATE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS UPDATE_TIME,
                   M.UUID,
                   M.CUSTOMER_SN
            FROM SAJET.G_WO_MAC M
            LEFT JOIN SAJET.G_SN_STATUS S ON S.SERIAL_NUMBER = M.SERIAL_NUMBER
            LEFT JOIN SAJET.SYS_EMP E ON E.EMP_ID = M.UPDATE_USERID
            LEFT JOIN SAJET.SYS_PROCESS P ON P.PROCESS_ID = S.WIP_PROCESS
            WHERE M.SERIAL_NUMBER IN (
                SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
                WHERE {whereStr}
            )
            ORDER BY M.WORK_ORDER, M.SERIAL_NUMBER";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<MacInfoItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
            list.Add(MapRow(r));

        return list;
    }

    // ============ 备份 + 删除 ============

    /// <summary>
    /// 备份到 G_HT_WO_MAC 后删除 G_WO_MAC 中符合条件的记录。
    /// 删除时只针对 WIP_PROCESS = 200018 的 SN。
    /// </summary>
    /// <param name="serials">序号条件</param>
    /// <param name="reworks">重工号条件</param>
    /// <returns>受影响行数之和</returns>
    public static int BackupAndDelete(
        List<string> serials,
        List<string> reworks)
    {
        if (serials.Count == 0 && reworks.Count == 0)
            return 0;

        // 构建子查询条件
        var conditions = new List<string>();
        var ps = new Dictionary<string, object>();
        int idx = 0;

        if (serials.Count > 0)
        {
            var names = new List<string>();
            foreach (var sn in serials)
            {
                string p = $"s{idx++}";
                names.Add(":" + p);
                ps[p] = sn;
            }
            conditions.Add($"S.SERIAL_NUMBER IN ({string.Join(",", names)})");
        }

        if (reworks.Count > 0)
        {
            var names = new List<string>();
            foreach (var rw in reworks)
            {
                string p = $"r{idx++}";
                names.Add(":" + p);
                ps[p] = rw;
            }
            conditions.Add($"S.REWORK_NO IN ({string.Join(",", names)})");
        }

        string whereStr = string.Join(" OR ", conditions);
        ps["wipProcess"] = DeleteWipProcess;

        // 子查询：符合条件且 WIP_PROCESS = 200018 的 SN
        string subQuery = $@"
            SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
            WHERE S.WIP_PROCESS = :wipProcess
              AND ({whereStr})";

        var statements = new List<(string, Dictionary<string, object>?)>
        {
            // 1. 备份
            ($@"
                INSERT INTO SAJET.G_HT_WO_MAC
                SELECT M.* FROM SAJET.G_WO_MAC M
                WHERE M.SERIAL_NUMBER IN ({subQuery})", ps),

            // 2. 删除
            ($@"
                DELETE FROM SAJET.G_WO_MAC
                WHERE SERIAL_NUMBER IN ({subQuery})", ps),
        };

        return OracleHelper.ExecuteInTransaction(statements);
    }

    // ============ 私有：映射 ============

    private static MacInfoItem MapRow(DataRow r)
    {
        return new MacInfoItem
        {
            WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
            SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),
            Mac = OracleHelper.GetStr(r, "MAC"),
            CurrentProcess = OracleHelper.GetStr(r, "CURRENT_PROCESS"),
            UpdateEmp = OracleHelper.GetStr(r, "UPDATE_EMP"),
            UpdateTime = OracleHelper.GetStr(r, "UPDATE_TIME"),
            Uuid = OracleHelper.GetStr(r, "UUID"),
            CustomerSN = OracleHelper.GetStr(r, "CUSTOMER_SN"),
        };
    }
}