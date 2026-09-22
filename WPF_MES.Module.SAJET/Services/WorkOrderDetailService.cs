using System.Data;
using WPF_MES.Contracts.Models;
using WPF_MES.Contracts.Models.SnItem;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 工单详情相关 SQL
/// </summary>
internal static class WorkOrderDetailService
{
    /// <summary>
    /// 获取不重复 SN 数量
    /// </summary>
    public static int GetSnCount(string workOrderNo)
    {
        const string sql = @"
            SELECT COUNT(DISTINCT S.SERIAL_NUMBER)
            FROM SAJET.G_SN_STATUS S
            WHERE S.WORK_ORDER = :wo";

        var ps = new Dictionary<string, object> { { "wo", workOrderNo } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 获取工单下的 SN 列表
    /// </summary>
    public static List<SnListItem> GetSnRecords(string workOrderNo)
    {
        const string sql = @"
        SELECT S.SERIAL_NUMBER,
               S.WORK_ORDER,
               P.PART_NO,
               PR.PROCESS_NAME,
               S.WORK_FLAG,
               S.CUSTOMER_SN,
               S.CARTON_NO,
               R.ROUTE_NAME,
               S.QC_NO,
               S.REWORK_NO,
               TO_CHAR(S.OUT_PROCESS_TIME, 'YYYY/MM/DD HH24:MI:SS') AS UPDATE_TIME
        FROM SAJET.G_SN_STATUS S
        LEFT JOIN SAJET.SYS_PART P ON P.PART_ID = S.MODEL_ID
        LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = S.WIP_PROCESS
        LEFT JOIN SAJET.SYS_ROUTE R ON R.ROUTE_ID = S.ROUTE_ID
        WHERE S.WORK_ORDER = :wo
        ORDER BY S.SERIAL_NUMBER";

        var ps = new Dictionary<string, object> { { "wo", workOrderNo } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<SnListItem>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new SnListItem
            {
                SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),
                WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
                PartNo = OracleHelper.GetStr(r, "PART_NO"),
                ProcessName = OracleHelper.GetStr(r, "PROCESS_NAME"),
                WorkFlag = OracleHelper.GetStr(r, "WORK_FLAG"),
                CustomerSN = OracleHelper.GetStr(r, "CUSTOMER_SN"),
                CartonNo = OracleHelper.GetStr(r, "CARTON_NO"),
                RouteName = OracleHelper.GetStr(r, "ROUTE_NAME"),
                QcNo = OracleHelper.GetStr(r, "QC_NO"),
                ReworkNo = OracleHelper.GetStr(r, "REWORK_NO"),
                UpdateTime = OracleHelper.GetStr(r, "UPDATE_TIME"),
            });
        }

        return list;
    }
    /// <summary>
    /// 获取途径的 SN 数量（不重复）
    /// </summary>
    public static int GetTravelSnCount(string workOrderNo)
    {
        const string sql = @"
        SELECT COUNT(DISTINCT T.SERIAL_NUMBER)
        FROM SAJET.G_SN_TRAVEL T
        WHERE T.WORK_ORDER = :wo";

        var ps = new Dictionary<string, object> { { "wo", workOrderNo } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 获取途径的 SN 列表（按 SN 去重）
    /// </summary>
    /// <summary>
    /// 获取途经的 SN 列表。
    /// 逻辑：G_SN_STATUS 里、且曾在 G_SN_TRAVEL 里出现过的 SN。
    /// </summary>
    public static List<SnListItem> GetTravelSnRecords(string workOrderNo)
    {
        const string sql = @"
        SELECT S.SERIAL_NUMBER,
               S.WORK_ORDER,
               P.PART_NO,
               PR.PROCESS_NAME,
               S.WORK_FLAG,
               S.CUSTOMER_SN,
               S.CARTON_NO,
               R.ROUTE_NAME,
               S.QC_NO,
               S.REWORK_NO,
               TO_CHAR(S.OUT_PROCESS_TIME, 'YYYY/MM/DD HH24:MI:SS') AS UPDATE_TIME
        FROM SAJET.G_SN_STATUS S
        LEFT JOIN SAJET.SYS_PART    P  ON P.PART_ID    = S.MODEL_ID
        LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = S.WIP_PROCESS
        LEFT JOIN SAJET.SYS_ROUTE   R  ON R.ROUTE_ID    = S.ROUTE_ID
        WHERE S.SERIAL_NUMBER IN (
            SELECT DISTINCT T.SERIAL_NUMBER
            FROM SAJET.G_SN_TRAVEL T
            WHERE T.WORK_ORDER = :wo
        )
        ORDER BY S.SERIAL_NUMBER";

        var ps = new Dictionary<string, object> { { "wo", workOrderNo } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<SnListItem>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new SnListItem
            {
                SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),
                WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
                PartNo = OracleHelper.GetStr(r, "PART_NO"),
                ProcessName = OracleHelper.GetStr(r, "PROCESS_NAME"),
                WorkFlag = OracleHelper.GetStr(r, "WORK_FLAG"),
                CustomerSN = OracleHelper.GetStr(r, "CUSTOMER_SN"),
                CartonNo = OracleHelper.GetStr(r, "CARTON_NO"),
                RouteName = OracleHelper.GetStr(r, "ROUTE_NAME"),
                QcNo = OracleHelper.GetStr(r, "QC_NO"),
                ReworkNo = OracleHelper.GetStr(r, "REWORK_NO"),
                UpdateTime = OracleHelper.GetStr(r, "UPDATE_TIME"),
            });
        }

        return list;
    }
}