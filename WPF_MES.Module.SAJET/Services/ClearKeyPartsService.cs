using System.Data;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 清除料件相关数据库操作
/// </summary>
internal static class ClearKeyPartsService
{
    // ============ 根据 SN 列表查询涉及的工单+工序 ============

    /// <summary>
    /// 根据 SN 条件查询涉及的"工单 + 工序"（去重）
    /// </summary>
    /// <param name="whereClause">SN 条件，如 "S.SERIAL_NUMBER IN (:c0)" 或 "S.REWORK_NO IN (:c0)"</param>
    public static List<KeyPartResultItem> QueryResultItems(
        string whereClause,
        Dictionary<string, object> ps)
    {
        string sql = $@"
            SELECT DISTINCT K.WORK_ORDER, P.PROCESS_NAME
            FROM SAJET.G_SN_KEYPARTS K
            LEFT JOIN SAJET.SYS_PROCESS P ON P.PROCESS_ID = K.PROCESS_ID
            WHERE K.SERIAL_NUMBER IN (
                SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
                WHERE ({whereClause})
            )
            ORDER BY K.WORK_ORDER, P.PROCESS_NAME";

        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<KeyPartResultItem>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new KeyPartResultItem
            {
                WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
                ProcessName = OracleHelper.GetStr(r, "PROCESS_NAME"),
                IsChecked = false,
            });
        }
        return list;
    }

    // ============ 根据 SN 查询单 SN 详情 ============

    /// <summary>
    /// 根据序列号查询该 SN 下的所有关键件记录
    /// </summary>
    public static List<KeyPartRecord> GetKeyPartRecords(string sn)
    {
        const string sql = @"
            SELECT K.WORK_ORDER, K.SERIAL_NUMBER, K.PROCESS_ID,
                   P.PROCESS_NAME, K.ITEM_PART_SN, K.ITEM_PART_ID,
                   PA.PART_NO
            FROM SAJET.G_SN_KEYPARTS K
            LEFT JOIN SAJET.SYS_PROCESS P ON P.PROCESS_ID = K.PROCESS_ID
            LEFT JOIN SAJET.SYS_PART PA ON PA.PART_ID = K.ITEM_PART_ID
            WHERE K.SERIAL_NUMBER = :sn
            ORDER BY K.UPDATE_TIME";

        var ps = new Dictionary<string, object> { { "sn", sn } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<KeyPartRecord>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new KeyPartRecord
            {
                WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
                SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),
                ProcessId = OracleHelper.GetInt(r, "PROCESS_ID"),
                ProcessName = OracleHelper.GetStr(r, "PROCESS_NAME"),
                ItemPartSn = OracleHelper.GetStr(r, "ITEM_PART_SN"),
                ItemPartId = OracleHelper.GetInt(r, "ITEM_PART_ID"),
                PartNo = OracleHelper.GetStr(r, "PART_NO"),
            });
        }
        return list;
    }

    // ============ 更新关键件 SN ============

    /// <summary>
    /// 更新某 SN 下某个工序的关键件序列号
    /// </summary>
    /// <returns>受影响行数</returns>
    public static int UpdateItemPartSn(
        string serialNumber,
        string oldItemPartSn,
        string newItemPartSn)
    {
        const string sql = @"
            UPDATE SAJET.G_SN_KEYPARTS
            SET ITEM_PART_SN = :newSn
            WHERE SERIAL_NUMBER = :sn
              AND ITEM_PART_SN = :oldSn";

        var ps = new Dictionary<string, object>
        {
            { "newSn", newItemPartSn },
            { "sn",    serialNumber },
            { "oldSn", oldItemPartSn },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 备份 + 删除指定行 ============

    /// <summary>
    /// 备份 + 删除指定工单+工序的关键件。
    /// 用事务保证一致。
    /// </summary>
    /// <param name="whereClause">SN 条件</param>
    /// <param name="ps">参数</param>
    /// <param name="items">要删除的工单+工序列表</param>
    /// <returns>删除的总行数</returns>
    public static int BackupAndDelete(
        string whereClause,
        Dictionary<string, object> ps,
        List<KeyPartResultItem> items)
    {
        if (items == null || items.Count == 0) return 0;

        var statements = new List<(string, Dictionary<string, object>?)>();

        foreach (var item in items)
        {
            // 每个 item 需要独立的参数字典，避免冲突
            var itemPs = new Dictionary<string, object>(ps)
            {
                { "wo",   item.WorkOrder },
                { "pname", item.ProcessName },
            };

            // 备份
            string backupSql = $@"
                INSERT INTO SAJET.G_HT_SN_KEYPARTS
                SELECT K.*
                FROM SAJET.G_SN_KEYPARTS K
                WHERE K.WORK_ORDER = :wo
                  AND K.PROCESS_ID = (
                      SELECT PROCESS_ID FROM SAJET.SYS_PROCESS
                      WHERE PROCESS_NAME = :pname
                  )
                  AND K.SERIAL_NUMBER IN (
                      SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
                      WHERE ({whereClause})
                  )";

            statements.Add((backupSql, itemPs));

            // 删除
            string deleteSql = $@"
                DELETE FROM SAJET.G_SN_KEYPARTS K
                WHERE K.WORK_ORDER = :wo
                  AND K.PROCESS_ID = (
                      SELECT PROCESS_ID FROM SAJET.SYS_PROCESS
                      WHERE PROCESS_NAME = :pname
                  )
                  AND K.SERIAL_NUMBER IN (
                      SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S
                      WHERE ({whereClause})
                  )";

            statements.Add((deleteSql, itemPs));
        }

        return OracleHelper.ExecuteInTransaction(statements);
    }
}