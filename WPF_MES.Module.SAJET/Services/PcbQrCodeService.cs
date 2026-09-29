using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// PCB QR Code 相关操作
/// </summary>
internal static class PcbQrCodeService
{
    /// <summary>创建时间来源的固定工序 ID</summary>
    private const int CreateTimeProcessId = 200204; //pcb_input

    // ============ 查询 ============

    /// <summary>
    /// 按 SN / QRCode 条件查询
    /// </summary>
    public static List<PcbQrCodeItem> Query(
        List<string> serials,
        List<string> qrcodes,
        List<string> reworks)
    {
        if (serials.Count == 0 && qrcodes.Count == 0 && reworks.Count == 0)
            return new List<PcbQrCodeItem>();

        var subConditions = new List<string>();
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
            subConditions.Add($"E.STRSMTSN IN ({string.Join(",", names)})");
        }

        if (qrcodes.Count > 0)
        {
            var names = new List<string>();
            foreach (var qr in qrcodes)
            {
                string p = $"q{idx++}";
                names.Add(":" + p);
                ps[p] = qr;
            }
            subConditions.Add($"E.PCB_QRCODE IN ({string.Join(",", names)})");
        }

        // 新增：重工号 → 通过 G_SN_STATUS 桥接
        if (reworks.Count > 0)
        {
            var names = new List<string>();
            foreach (var rw in reworks)
            {
                string p = $"r{idx++}";
                names.Add(":" + p);
                ps[p] = rw;
            }
            subConditions.Add(
                $"E.STRSMTSN IN (" +
                $"SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S " +
                $"WHERE S.REWORK_NO IN ({string.Join(",", names)}))");
        }

        string sql = $@"
            SELECT E.ECS_PART_NO, E.PCB_CUST_PN, E.PCB_SN,
                   E.STRSMTSN, E.PCB_QRCODE,
                   TO_CHAR(E.CREATE_TIME, 'YYYY-MM-DD HH24:MI:SS') AS CREATE_TIME
            FROM SAJET.ECS_PPID_PCB_CODE E
            WHERE {string.Join(" OR ", subConditions)}
            ORDER BY E.CREATE_TIME";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<PcbQrCodeItem>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
            list.Add(MapRow(r));
        return list;
    }

    // ============ 删除（按条件） ============

    /// <summary>
    /// 按 SN / QRCode 条件删除
    /// </summary>
    public static int Delete(
        List<string> serials,
        List<string> qrcodes,
        List<string> reworks)
    {
        if (serials.Count == 0 && qrcodes.Count == 0 && reworks.Count == 0)
            return 0;

        var subConditions = new List<string>();
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
            subConditions.Add($"E.STRSMTSN IN ({string.Join(",", names)})");
        }

        if (qrcodes.Count > 0)
        {
            var names = new List<string>();
            foreach (var qr in qrcodes)
            {
                string p = $"q{idx++}";
                names.Add(":" + p);
                ps[p] = qr;
            }
            subConditions.Add($"E.PCB_QRCODE IN ({string.Join(",", names)})");
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
            subConditions.Add(
                $"E.STRSMTSN IN (" +
                $"SELECT S.SERIAL_NUMBER FROM SAJET.G_SN_STATUS S " +
                $"WHERE S.REWORK_NO IN ({string.Join(",", names)}))");
        }

        string sql = $@"
        DELETE FROM SAJET.ECS_PPID_PCB_CODE E
        WHERE {string.Join(" OR ", subConditions)}";

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 添加 ============

    public static int Add(
        string ecsPartNo,
        string pcbCustPn,
        string pcbSn,
        string strSmtsn,
        string pcbQrcode,
        DateTime createTime)
    {
        const string sql = @"
            INSERT INTO SAJET.ECS_PPID_PCB_CODE
                (ECS_PART_NO, PCB_CUST_PN, PCB_SN, STRSMTSN, PCB_QRCODE, CREATE_TIME)
            VALUES
                (:ecs, :custpn, :pcbsn, :strsmtsn, :qrcode, :createTime)";

        var ps = new Dictionary<string, object>
        {
            { "ecs",        string.IsNullOrEmpty(ecsPartNo) ? (object)DBNull.Value : ecsPartNo },
            { "custpn",     string.IsNullOrEmpty(pcbCustPn) ? (object)DBNull.Value : pcbCustPn },
            { "pcbsn",      string.IsNullOrEmpty(pcbSn) ? (object)DBNull.Value : pcbSn },
            { "strsmtsn",   string.IsNullOrEmpty(strSmtsn) ? (object)DBNull.Value : strSmtsn },
            { "qrcode",     string.IsNullOrEmpty(pcbQrcode) ? (object)DBNull.Value : pcbQrcode },
            { "createTime", createTime },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 修改 ============

    public static int Update(
        string ecsPartNo,
        string pcbCustPn,
        string pcbSn,
        string strSmtsn,
        string pcbQrcode)
    {
        const string sql = @"
            UPDATE SAJET.ECS_PPID_PCB_CODE
            SET ECS_PART_NO = :ecs,
                PCB_CUST_PN = :cust,
                PCB_SN      = :sn,
                PCB_QRCODE  = :qrcode
            WHERE STRSMTSN = :str";

        var ps = new Dictionary<string, object>
        {
            { "ecs",    string.IsNullOrEmpty(ecsPartNo) ? (object)DBNull.Value : ecsPartNo },
            { "cust",   string.IsNullOrEmpty(pcbCustPn) ? (object)DBNull.Value : pcbCustPn },
            { "sn",     string.IsNullOrEmpty(pcbSn) ? (object)DBNull.Value : pcbSn },
            { "str",    strSmtsn },
            { "qrcode", pcbQrcode },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 查创建时间（按 SN） ============

    /// <summary>
    /// 从 G_SN_TRAVEL 查某 SN 的创建时间。
    /// 找不到返回 null。
    /// </summary>
    public static DateTime? GetCreateTimeBySn(string sn)
    {
        const string sql = @"
            SELECT MAX(T.OUT_PROCESS_TIME) FROM SAJET.G_SN_TRAVEL T
            WHERE T.SERIAL_NUMBER = :sn
              AND T.PROCESS_ID = :pid";

        var ps = new Dictionary<string, object>
        {
            { "sn",  sn },
            { "pid", CreateTimeProcessId },
        };

        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return null;
        return Convert.ToDateTime(result);
    }

    // ============ 私有映射 ============

    private static PcbQrCodeItem MapRow(DataRow r)
    {
        return new PcbQrCodeItem
        {
            EcsPartNo = OracleHelper.GetStr(r, "ECS_PART_NO"),
            PcbCustPn = OracleHelper.GetStr(r, "PCB_CUST_PN"),
            PcbSn = OracleHelper.GetStr(r, "PCB_SN"),
            StrSmtsn = OracleHelper.GetStr(r, "STRSMTSN"),
            PcbQrcode = OracleHelper.GetStr(r, "PCB_QRCODE"),
            CreateTime = OracleHelper.GetStr(r, "CREATE_TIME"),
        };
    }
}