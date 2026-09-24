using System.Data;
using WPF_MES.Contracts.Models.SnItem;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// SN 重工前后对比 + 恢复
/// </summary>
internal static class SnRecoveryService
{
    // ============ 公共 SELECT 列 ============

    private const string SelectColumns = @"
        S.SERIAL_NUMBER, S.WORK_ORDER, P.PART_NO, L.PDLINE_NAME,
        P1.PROCESS_NAME AS WIP_PROCESS,
        P2.PROCESS_NAME AS CURRENT_PROCESS,
        T.TERMINAL_NAME,
        S.CUSTOMER_SN, S.PALLET_NO, S.CARTON_NO, S.CONTAINER,
        TO_CHAR(S.OUT_PDLINE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS OUT_PDLINE_TIME,
        R.ROUTE_NAME,
        S.REWORK_NO";

    private const string Joins = @"
        LEFT JOIN SAJET.SYS_PART     P  ON P.PART_ID     = S.MODEL_ID
        LEFT JOIN SAJET.SYS_PDLINE   L  ON L.PDLINE_ID   = S.PDLINE_ID
        LEFT JOIN SAJET.SYS_PROCESS  P1 ON P1.PROCESS_ID = S.WIP_PROCESS
        LEFT JOIN SAJET.SYS_PROCESS  P2 ON P2.PROCESS_ID = S.PROCESS_ID
        LEFT JOIN SAJET.SYS_TERMINAL T  ON T.TERMINAL_ID = S.TERMINAL_ID
        LEFT JOIN SAJET.SYS_ROUTE    R  ON R.ROUTE_ID    = S.ROUTE_ID";

    // ============ 根据重工号查 SN 列表 ============

    /// <summary>
    /// 获取某重工号涉及的所有 SN（去重）
    /// </summary>
    public static List<string> GetSnListByReworkNo(string reworkNo)
    {
        const string sql = @"
            SELECT DISTINCT SERIAL_NUMBER FROM SAJET.G_REWORK_LOG
            WHERE REWORK_NO = :no
            ORDER BY SERIAL_NUMBER";

        var ps = new Dictionary<string, object> { { "no", reworkNo } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<string>();
        foreach (DataRow r in dt.Rows)
        {
            if (r[0] != DBNull.Value)
                list.Add(r[0].ToString()!.Trim());
        }
        return list;
    }

    // ============ 根据重工号查当前状态 ============

    /// <summary>
    /// 查该重工号涉及的所有 SN 的当前状态（G_SN_STATUS）
    /// </summary>
    public static List<ReworkSnItem> GetCurrentByReworkNo(string reworkNo)
    {
        string sql = $@"
            SELECT {SelectColumns}
            FROM SAJET.G_SN_STATUS S
            {Joins}
            WHERE S.SERIAL_NUMBER IN (
                SELECT DISTINCT SERIAL_NUMBER FROM SAJET.G_REWORK_LOG
                WHERE REWORK_NO = :no
            )
            ORDER BY S.SERIAL_NUMBER";

        var ps = new Dictionary<string, object> { { "no", reworkNo } };
        return QueryList(sql, ps);
    }

    /// <summary>
    /// 查该重工号对应的重工前状态（G_REWORK_LOG）
    /// </summary>
    public static List<ReworkSnItem> GetBackupByReworkNo(string reworkNo)
    {
        string sql = $@"
            SELECT {SelectColumns}
            FROM SAJET.G_REWORK_LOG S
            {Joins}
            WHERE S.REWORK_NO = :no
            ORDER BY S.SERIAL_NUMBER";

        var ps = new Dictionary<string, object> { { "no", reworkNo } };
        return QueryList(sql, ps);
    }

    // ============ 根据单个 SN 查 ============

    /// <summary>
    /// 查某个 SN 的当前状态
    /// </summary>
    public static List<ReworkSnItem> GetCurrentBySn(string sn)
    {
        string sql = $@"
            SELECT {SelectColumns}
            FROM SAJET.G_SN_STATUS S
            {Joins}
            WHERE S.SERIAL_NUMBER = :sn";

        var ps = new Dictionary<string, object> { { "sn", sn } };
        return QueryList(sql, ps);
    }

    /// <summary>
    /// 查某个 SN 的所有重工前状态（可能多条）
    /// </summary>
    public static List<ReworkSnItem> GetBackupBySn(string sn)
    {
        string sql = $@"
            SELECT {SelectColumns}
            FROM SAJET.G_REWORK_LOG S
            {Joins}
            WHERE S.SERIAL_NUMBER = :sn
            ORDER BY S.REWORK_NO";

        var ps = new Dictionary<string, object> { { "sn", sn } };
        return QueryList(sql, ps);
    }

    /// <summary>
    /// 查某个 SN 最近一条重工备份
    /// </summary>
    public static ReworkSnItem? GetLatestBackupBySn(string sn)
    {
        string sql = $@"
            SELECT * FROM (
                SELECT {SelectColumns}
                FROM SAJET.G_REWORK_LOG S
                {Joins}
                WHERE S.SERIAL_NUMBER = :sn
                ORDER BY S.REWORK_NO DESC
            ) WHERE ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "sn", sn } };
        var list = QueryList(sql, ps);
        return list.Count > 0 ? list[0] : null;
    }

    // ============ 恢复：用 G_REWORK_LOG 覆盖 G_SN_STATUS ============

    /// <summary>
    /// 将某个 SN 恢复为重工前状态（用最新一条备份覆盖）
    /// </summary>
    /// <returns>受影响行数</returns>
    /// <summary>
    /// 将某个 SN 恢复为重工前状态（用最新一条备份覆盖）
    /// </summary>
    /// <returns>受影响行数</returns>
    public static int RecoverFromLatestBackup(string sn)
    {
        const string sql = @"
        MERGE INTO SAJET.G_SN_STATUS T
        USING (
            SELECT * FROM (
                SELECT * FROM SAJET.G_REWORK_LOG
                WHERE SERIAL_NUMBER = :sn
                ORDER BY REWORK_NO DESC
            ) WHERE ROWNUM = 1
        ) L
        ON (T.SERIAL_NUMBER = L.SERIAL_NUMBER)
        WHEN MATCHED THEN UPDATE SET
            T.WORK_ORDER   = L.WORK_ORDER,
            T.MODEL_ID     = L.MODEL_ID,
            T.ROUTE_ID     = L.ROUTE_ID,
            T.WIP_PROCESS  = L.WIP_PROCESS,
            T.NEXT_PROCESS = L.NEXT_PROCESS,
            T.CUSTOMER_SN  = L.CUSTOMER_SN,
            T.CARTON_NO    = L.CARTON_NO,
            T.PALLET_NO    = L.PALLET_NO,
            T.CONTAINER    = L.CONTAINER,
            T.QC_NO        = L.QC_NO,
            T.QC_RESULT    = L.QC_RESULT";

        var ps = new Dictionary<string, object> { { "sn", sn } };
        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    /// <summary>
    /// 将某重工号下的所有 SN 恢复为重工前状态。
    /// </summary>
    /// <returns>受影响行数</returns>
    public static int RecoverAllByReworkNo(string reworkNo)
    {
        const string sql = @"
        MERGE INTO SAJET.G_SN_STATUS T
        USING (
            SELECT * FROM SAJET.G_REWORK_LOG
            WHERE REWORK_NO = :reworkNo
        ) S
        ON (T.SERIAL_NUMBER = S.SERIAL_NUMBER)
        WHEN MATCHED THEN UPDATE SET
            T.WORK_ORDER   = S.WORK_ORDER,
            T.MODEL_ID     = S.MODEL_ID,
            T.ROUTE_ID     = S.ROUTE_ID,
            T.WIP_PROCESS  = S.WIP_PROCESS,
            T.NEXT_PROCESS = S.NEXT_PROCESS,
            T.CUSTOMER_SN  = S.CUSTOMER_SN,
            T.CARTON_NO    = S.CARTON_NO,
            T.PALLET_NO    = S.PALLET_NO,
            T.CONTAINER    = S.CONTAINER,
            T.QC_NO        = S.QC_NO,
            T.QC_RESULT    = S.QC_RESULT";

        var ps = new Dictionary<string, object> { { "reworkNo", reworkNo } };
        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 私有辅助 ============

    private static List<ReworkSnItem> QueryList(string sql,
        Dictionary<string, object>? ps = null)
    {
        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<ReworkSnItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
            list.Add(ReworkService.MapRow(r));

        return list;
    }
}