using System.Data;
using WPF_MES.Contracts;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 出行记录查询/导出
/// </summary>
internal static class ExportTravelsService
{
    // ============ 加载产线 ============

    public static List<ComboStringItem> LoadPdLines()
    {
        const string sql = @"
            SELECT PDLINE_NAME FROM SAJET.SYS_PDLINE
            WHERE ENABLED = 'Y'
            ORDER BY PDLINE_NAME";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<ComboStringItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            string name = OracleHelper.GetStr(r, "PDLINE_NAME");
            if (name.Length > 0)
                list.Add(new ComboStringItem(name, name));
        }
        return list;
    }

    // ============ 按产线加载工序 ============

    public static List<ComboStringItem> LoadProcessesByLine(string lineName)
    {
        const string sql = @"
            SELECT DISTINCT PR.PROCESS_NAME
            FROM SAJET.SYS_TERMINAL PT
            LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = PT.PROCESS_ID
            WHERE PT.ENABLED = 'Y' AND PT.CONTROL_ID <> 0
            AND PT.PDLINE_ID = (SELECT PL.PDLINE_ID FROM SAJET.SYS_PDLINE PL WHERE PL.PDLINE_NAME = :line)
            ORDER BY PR.PROCESS_NAME";

        var ps = new Dictionary<string, object> { { "line", lineName } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<ComboStringItem>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            string name = OracleHelper.GetStr(r, "PROCESS_NAME");
            if (name.Length > 0)
                list.Add(new ComboStringItem(name, name));
        }
        return list;
    }

    // ============ 按产线+工序加载机台 ============

    /// <summary>
    /// 按产线 + 工序加载机台
    /// </summary>
    public static List<ComboStringItem> LoadTerminals(string lineName, string processName)
    {
        const string sql = @"
        SELECT DISTINCT PT.TERMINAL_NAME
        FROM SAJET.SYS_TERMINAL PT
        LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = PT.PROCESS_ID
        WHERE PT.ENABLED = 'Y'
          AND PT.CONTROL_ID <> 0
          AND PT.PDLINE_ID = (SELECT PL.PDLINE_ID FROM SAJET.SYS_PDLINE PL WHERE PL.PDLINE_NAME = :line)
          AND PR.PROCESS_NAME = :process
        ORDER BY PT.TERMINAL_NAME";

        var ps = new Dictionary<string, object>
    {
        { "line", lineName },
        { "process", processName },
    };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<ComboStringItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            string name = OracleHelper.GetStr(r, "TERMINAL_NAME");
            if (name.Length > 0)
                list.Add(new ComboStringItem(name, name));
        }
        return list;
    }

    // ============ 查询流程记录 ============
    /// <summary>
    /// 按条件查询流程记录
    /// </summary>
    public static List<TravelRecord> Query(
        string workOrder,
        string partNo,
        string lineName,
        string processName,
        string terminalName,
        DateTime startTime,
        DateTime endTime)
    {
        var whereClauses = new List<string>();
        var ps = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(workOrder))
        {
            whereClauses.Add("T.WORK_ORDER = :wo");
            ps["wo"] = workOrder;
        }

        if (!string.IsNullOrEmpty(partNo))
        {
            whereClauses.Add("T.MODEL_ID = (SELECT PART_ID FROM SAJET.SYS_PART WHERE PART_NO = :part)");
            ps["part"] = partNo;
        }

        if (!string.IsNullOrEmpty(lineName))
        {
            whereClauses.Add("T.PDLINE_ID = (SELECT PDLINE_ID FROM SAJET.SYS_PDLINE WHERE PDLINE_NAME = :line)");
            ps["line"] = lineName;
        }

        if (!string.IsNullOrEmpty(processName))
        {
            whereClauses.Add("T.PROCESS_ID = (SELECT PROCESS_ID FROM SAJET.SYS_PROCESS WHERE PROCESS_NAME = :process)");
            ps["process"] = processName;
        }

        if (!string.IsNullOrEmpty(terminalName))
        {
            whereClauses.Add(
                "T.TERMINAL_ID = (SELECT TERMINAL_ID FROM SAJET.SYS_TERMINAL " +
                "WHERE TERMINAL_NAME = :terminal AND PDLINE_ID = T.PDLINE_ID AND PROCESS_ID = T.PROCESS_ID)");
            ps["terminal"] = terminalName;
        }

        whereClauses.Add("T.OUT_PROCESS_TIME BETWEEN :startTime AND :endTime");
        ps["startTime"] = startTime;
        ps["endTime"] = endTime;

        string whereStr = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        string sql = $@"
        SELECT T.WORK_ORDER, P.PART_NO, T.SERIAL_NUMBER,
               PL.PDLINE_NAME, PR.PROCESS_NAME, T.CURRENT_STATUS,
               TO_CHAR(T.OUT_PROCESS_TIME, 'YYYY/MM/DD HH24:MI:SS') AS OUT_PROCESS_TIME,
               TE.TERMINAL_NAME, E.EMP_NAME, C.CUSTOMER_NAME,
               T.CUSTOMER_SN, T.QC_NO, T.REWORK_NO, T.PANEL_NO
        FROM SAJET.G_SN_TRAVEL T
        LEFT JOIN SAJET.SYS_PART     P  ON P.PART_ID    = T.MODEL_ID
        LEFT JOIN SAJET.SYS_PDLINE   PL ON PL.PDLINE_ID = T.PDLINE_ID
        LEFT JOIN SAJET.SYS_PROCESS  PR ON PR.PROCESS_ID = T.PROCESS_ID
        LEFT JOIN SAJET.SYS_TERMINAL TE ON TE.TERMINAL_ID = T.TERMINAL_ID
        LEFT JOIN SAJET.SYS_EMP      E  ON E.EMP_ID     = T.EMP_ID
        LEFT JOIN SAJET.SYS_CUSTOMER C  ON C.CUSTOMER_ID = T.CUSTOMER_ID
        {whereStr}
        ORDER BY T.OUT_PROCESS_TIME DESC";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<TravelRecord>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
            list.Add(MapRow(r));

        return list;
    }

    // ============ 映射 ============

    private static TravelRecord MapRow(DataRow r)
    {
        int status = OracleHelper.GetInt(r, "CURRENT_STATUS");
        string statusText = status switch
        {
            0 => "OK",
            1 => "NG",
            _ => status.ToString(),
        };

        return new TravelRecord
        {
            WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
            PartNo = OracleHelper.GetStr(r, "PART_NO"),
            SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),
            PdlineName = OracleHelper.GetStr(r, "PDLINE_NAME"),
            ProcessName = OracleHelper.GetStr(r, "PROCESS_NAME"),
            Status = status,
            StatusText = statusText,
            OutProcessTime = OracleHelper.GetStr(r, "OUT_PROCESS_TIME"),
            TerminalName = OracleHelper.GetStr(r, "TERMINAL_NAME"),
            EmpName = OracleHelper.GetStr(r, "EMP_NAME"),
            CustomerName = OracleHelper.GetStr(r, "CUSTOMER_NAME"),
            CustomerSN = OracleHelper.GetStr(r, "CUSTOMER_SN"),
            QcNo = OracleHelper.GetStr(r, "QC_NO"),
            ReworkNo = OracleHelper.GetStr(r, "REWORK_NO"),
            PanelNo = OracleHelper.GetStr(r, "PANEL_NO"),
        };
    }
}