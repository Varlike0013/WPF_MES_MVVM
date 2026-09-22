using System.Data;
using WPF_MES.Contracts;
using WPF_MES.Contracts.Models;

namespace WPF_MES.Module.SAJET.Services;

internal static class StatusTagService
{
    /// <summary>
    /// 按输入条件找序列号
    /// </summary>
    /// <param name="inputType">0=序号 1=料件 2=卡号 3=SSN 4=出货序号 5=新序号</param>
    public static List<string> GetSerialNumbers(int inputType, string input)
    {
        string sql;
        switch (inputType)
        {
            case 0:
                sql = "SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S WHERE S.SERIAL_NUMBER = :input";
                break;
            case 1:
                sql = "SELECT K.SERIAL_NUMBER FROM SAJET.G_SN_KEYPARTS K WHERE K.ITEM_PART_SN = :input";
                break;
            case 2:
                sql = "SELECT M.SERIAL_NUMBER FROM SAJET.G_WO_MAC M WHERE M.MAC = :input";
                break;
            case 3:
                sql = "SELECT M.SERIAL_NUMBER FROM SAJET.G_WO_MAC M WHERE M.CUSTOMER_SN = :input";
                break;
            case 4:
                sql = "SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S WHERE S.CUSTOMER_SN = :input";
                break;
            case 5:
                sql = "SELECT C.OLD_SERIAL_NUMBER FROM SAJET.G_SN_CHANGE C WHERE C.NEW_SERIAL_NUMBER = :input";
                break;
            default:
                throw new ArgumentException("未知的查询类型：" + inputType);
        }

        var ps = new Dictionary<string, object> { { "input", input } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<string>();
        foreach (DataRow r in dt.Rows)
        {
            if (r[0] != DBNull.Value)
            {
                string s = r[0].ToString()!.Trim();
                if (s.Length > 0) list.Add(s);
            }
        }
        return list;
    }

    public static StatusTagInfo? GetStatusTagInfo(string serialNumber)
    {
        const string sql = @"
            SELECT S.WORK_ORDER, P.PART_NO, P.SPEC1,
                   PR.PROCESS_NAME, S.WORK_FLAG, S.CUSTOMER_SN,
                   S.CARTON_NO, R.ROUTE_NAME, M.MAC,
                   M.CUSTOMER_SN AS SSN, PP.PCB_QRCODE
            FROM SAJET.G_SN_STATUS S
            LEFT JOIN SAJET.SYS_PART P ON P.PART_ID = S.MODEL_ID
            LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = S.WIP_PROCESS
            LEFT JOIN SAJET.SYS_ROUTE R ON R.ROUTE_ID = S.ROUTE_ID
            LEFT JOIN SAJET.G_WO_MAC M ON M.SERIAL_NUMBER = S.SERIAL_NUMBER
            LEFT JOIN SAJET.ECS_PPID_PCB_CODE PP ON PP.STRSMTSN = S.SERIAL_NUMBER
            WHERE S.SERIAL_NUMBER = :sn AND ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "sn", serialNumber } };
        var dt = OracleHelper.QueryDataTable(sql, ps);
        if (dt.Rows.Count == 0) return null;

        var r = dt.Rows[0];
        int workFlag = GetInt(r, "WORK_FLAG");

        return new StatusTagInfo
        {
            SerialNumber = serialNumber,
            WorkOrder = GetStr(r, "WORK_ORDER"),
            PartNo = GetStr(r, "PART_NO"),
            PartDesc = GetStr(r, "SPEC1"),
            NextProcess = GetStr(r, "PROCESS_NAME"),
            WorkFlag = workFlag,
            WorkFlagText = workFlag switch
            {
                0 => "Good",
                1 => "Repair",
                2 => "Hold",
                _ => workFlag.ToString(),
            },
            CustomerSN = GetStr(r, "CUSTOMER_SN"),
            CartonNo = GetStr(r, "CARTON_NO"),
            RouteName = GetStr(r, "ROUTE_NAME"),
            Mac = GetStr(r, "MAC"),
            SSN = GetStr(r, "SSN"),
            PcbQrCode = GetStr(r, "PCB_QRCODE"),
        };
    }

    public static List<TravelRecord> GetTravelRecords(string serialNumber)
    {
        const string sql = @"
            SELECT T.WORK_ORDER, P.PART_NO, PL.PDLINE_NAME, PR.PROCESS_NAME, T.CURRENT_STATUS,
                   TO_CHAR(T.OUT_PROCESS_TIME, 'YYYY/MM/DD HH24:MI:SS') AS OUT_PROCESS_TIME,
                   TE.TERMINAL_NAME, E.EMP_NAME, C.CUSTOMER_NAME, T.CUSTOMER_SN,
                   T.QC_NO, T.REWORK_NO, T.PANEL_NO
            FROM SAJET.G_SN_TRAVEL T
            LEFT JOIN SAJET.SYS_PART P ON P.PART_ID = T.MODEL_ID
            LEFT JOIN SAJET.SYS_PDLINE PL ON PL.PDLINE_ID = T.PDLINE_ID
            LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = T.PROCESS_ID
            LEFT JOIN SAJET.SYS_TERMINAL TE ON TE.TERMINAL_ID = T.TERMINAL_ID
            LEFT JOIN SAJET.SYS_EMP E ON E.EMP_ID = T.EMP_ID
            LEFT JOIN SAJET.SYS_CUSTOMER C ON C.CUSTOMER_ID = T.CUSTOMER_ID
            WHERE T.SERIAL_NUMBER = :sn
            ORDER BY T.OUT_PROCESS_TIME";

        var ps = new Dictionary<string, object> { { "sn", serialNumber } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<TravelRecord>();
        foreach (DataRow r in dt.Rows)
        {
            int status = GetInt(r, "CURRENT_STATUS");
            list.Add(new TravelRecord
            {
                WorkOrder = GetStr(r, "WORK_ORDER"),
                PartNo = GetStr(r, "PART_NO"),
                PdlineName = GetStr(r, "PDLINE_NAME"),
                ProcessName = GetStr(r, "PROCESS_NAME"),
                Status = status,
                StatusText = status switch { 0 => "OK", 1 => "NG", _ => status.ToString() },
                OutProcessTime = GetStr(r, "OUT_PROCESS_TIME"),
                TerminalName = GetStr(r, "TERMINAL_NAME"),
                EmpName = GetStr(r, "EMP_NAME"),
                CustomerName = GetStr(r, "CUSTOMER_NAME"),
                CustomerSN = GetStr(r, "CUSTOMER_SN"),
                QcNo = GetStr(r, "QC_NO"),
                ReworkNo = GetStr(r, "REWORK_NO"),
                PanelNo = GetStr(r, "PANEL_NO"),
            });
        }
        return list;
    }

    public static List<PartRecord> GetPartRecords(string serialNumber)
    {
        const string sql = @"
            SELECT P.PART_NO, K.VERSION, P.SPEC1, K.ITEM_PART_SN, P.PART_TYPE,
                   PR.PROCESS_NAME, E.EMP_NAME,
                   TO_CHAR(K.UPDATE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS UPDATE_TIME
            FROM SAJET.G_SN_KEYPARTS K
            LEFT JOIN SAJET.SYS_PART P ON P.PART_ID = K.ITEM_PART_ID
            LEFT JOIN SAJET.SYS_EMP E ON E.EMP_ID = K.UPDATE_USERID
            LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = K.PROCESS_ID
            WHERE K.SERIAL_NUMBER = :sn
            ORDER BY K.UPDATE_TIME";

        var ps = new Dictionary<string, object> { { "sn", serialNumber } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<PartRecord>();
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new PartRecord
            {
                PartNo = GetStr(r, "PART_NO"),
                Version = GetStr(r, "VERSION"),
                Spec = GetStr(r, "SPEC1"),
                ItemPartSn = GetStr(r, "ITEM_PART_SN"),
                PartType = GetStr(r, "PART_TYPE"),
                ProcessName = GetStr(r, "PROCESS_NAME"),
                EmpName = GetStr(r, "EMP_NAME"),
                UpdateTime = GetStr(r, "UPDATE_TIME"),
            });
        }
        return list;
    }
    /// <summary>
    /// 根据序列号获取重工记录
    /// </summary>
    public static List<ReworkRecord> GetReworkRecords(string serialNumber)
    {
        const string sql = @"
        SELECT R.REWORK_NO, E.EMP_NAME,
               TO_CHAR(R.UPDATE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS UPDATE_TIME,
               R.REMARK
        FROM SAJET.G_REWORK_NO R
        LEFT JOIN SAJET.SYS_EMP E ON E.EMP_ID = R.EMP_ID
        WHERE R.REWORK_NO IN (
            SELECT S.REWORK_NO
            FROM SAJET.G_SN_STATUS S
            WHERE S.SERIAL_NUMBER = :sn
              AND S.REWORK_NO IS NOT NULL
        )
        ORDER BY R.UPDATE_TIME";

        var ps = new Dictionary<string, object> { { "sn", serialNumber } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<ReworkRecord>();
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new ReworkRecord
            {
                ReworkNo = GetStr(r, "REWORK_NO"),
                EmpName = GetStr(r, "EMP_NAME"),
                UpdateTime = GetStr(r, "UPDATE_TIME"),
                Remark = GetStr(r, "REMARK"),
            });
        }
        return list;
    }
    // ============ 辅助 ============

    private static string GetStr(DataRow r, string col)
        => r.Table.Columns.Contains(col) && r[col] != DBNull.Value
            ? r[col].ToString()!.Trim()
            : string.Empty;

    private static int GetInt(DataRow r, string col)
        => r.Table.Columns.Contains(col) && r[col] != DBNull.Value
            ? Convert.ToInt32(r[col])
            : 0;
}