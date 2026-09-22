using System.Data;
using WPF_MES.Contracts.Models.SnItem;
using WPF_MES.Contracts.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// G_SN_STATUS 相关查询
/// </summary>
internal static class SnStatusService
{
    private const string SelectColumns = @"
    SELECT S.WORK_ORDER,
           S.SERIAL_NUMBER,

           -- 料号
           S.MODEL_ID      AS PART_ID,
           P.PART_NO,
           P.SPEC1         AS PART_DESC,

           S.ROUTE_ID,
           S.PDLINE_ID,
           S.STAGE_ID,
           S.TERMINAL_ID,
           S.CURRENT_STATUS,
           S.WORK_FLAG,
           S.OUT_PROCESS_TIME AS UPDATE_TIME,
           S.PALLET_NO,
           S.CARTON_NO,
           S.CONTAINER,
           S.QC_NO,
           S.QC_RESULT,
           S.CUSTOMER_ID,
           S.REWORK_NO,
           S.EMP_ID,
           S.CUSTOMER_SN,

           -- 三个工序
           S.PROCESS_ID,
           PR.PROCESS_NAME AS PROCESS_NAME,
           S.NEXT_PROCESS  AS NEXT_PROCESS_ID,
           PN.PROCESS_NAME AS NEXT_PROCESS_NAME,
           S.WIP_PROCESS   AS WIP_PROCESS_ID,
           PW.PROCESS_NAME AS WIP_PROCESS_NAME
    FROM SAJET.G_SN_STATUS S
    LEFT JOIN SAJET.SYS_PART    P  ON P.PART_ID    = S.MODEL_ID
    LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = S.PROCESS_ID
    LEFT JOIN SAJET.SYS_PROCESS PN ON PN.PROCESS_ID = S.NEXT_PROCESS
    LEFT JOIN SAJET.SYS_PROCESS PW ON PW.PROCESS_ID = S.WIP_PROCESS";

    /// <summary>
    /// 根据序列号获取单个 SnStatus
    /// </summary>
    public static SnStatus? GetBySerialNumber(string serialNumber)
    {
        string sql = SelectColumns + @"
            WHERE SERIAL_NUMBER = :sn AND ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "sn", serialNumber } };
        var dt = OracleHelper.QueryDataTable(sql, ps);
        if (dt.Rows.Count == 0) return null;

        return MapSnStatus(dt.Rows[0]);
    }

    /// <summary>
    /// 根据工单号获取所有 SnStatus
    /// </summary>
    public static List<SnStatus> GetByWorkOrder(string workOrderNo)
    {
        string sql = SelectColumns + @"
            WHERE WORK_ORDER = :wo
            ORDER BY SERIAL_NUMBER";

        var ps = new Dictionary<string, object> { { "wo", workOrderNo } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<SnStatus>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
            list.Add(MapSnStatus(r));

        return list;
    }

    // ============ 手写映射 ============

    private static SnStatus MapSnStatus(DataRow r)
    {
        return new SnStatus
        {
            WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
            SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),

            // 料号
            Part = new PartInfo(
                OracleHelper.GetInt(r, "PART_ID"),
                OracleHelper.GetStr(r, "PART_NO"),
                OracleHelper.GetStr(r, "PART_DESC")),

            // 三个工序
            Process = new ProcessInfo(
                OracleHelper.GetInt(r, "PROCESS_ID"),
                OracleHelper.GetStr(r, "PROCESS_NAME")),

            NextProcess = new ProcessInfo(
                OracleHelper.GetInt(r, "NEXT_PROCESS_ID"),
                OracleHelper.GetStr(r, "NEXT_PROCESS_NAME")),

            WipProcess = new ProcessInfo(
                OracleHelper.GetInt(r, "WIP_PROCESS_ID"),
                OracleHelper.GetStr(r, "WIP_PROCESS_NAME")),

            RouteId = OracleHelper.GetInt(r, "ROUTE_ID"),
            PdlineId = OracleHelper.GetInt(r, "PDLINE_ID"),
            StageId = OracleHelper.GetInt(r, "STAGE_ID"),
            TerminalId = OracleHelper.GetInt(r, "TERMINAL_ID"),
            CurrentStatus = OracleHelper.GetStr(r, "CURRENT_STATUS"),
            WorkFlag = OracleHelper.GetStr(r, "WORK_FLAG"),
            UpdateTime = OracleHelper.GetDate(r, "UPDATE_TIME"),
            PalletNo = OracleHelper.GetStr(r, "PALLET_NO"),
            CartonNo = OracleHelper.GetStr(r, "CARTON_NO"),
            Container = OracleHelper.GetStr(r, "CONTAINER"),
            QcNo = OracleHelper.GetStr(r, "QC_NO"),
            QcResult = OracleHelper.GetStr(r, "QC_RESULT"),
            CustomerId = OracleHelper.GetInt(r, "CUSTOMER_ID"),
            ReworkNo = OracleHelper.GetStr(r, "REWORK_NO"),
            EmpId = OracleHelper.GetInt(r, "EMP_ID"),
            CustomerSn = OracleHelper.GetStr(r, "CUSTOMER_SN"),
        };
    }
}