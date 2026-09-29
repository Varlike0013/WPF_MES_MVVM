using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;
using WPF_MES.Shared.Cache;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// SMT 料盘信息相关操作
/// </summary>
internal static class SmtReelInfoService
{
    internal const string CacheKeySites = "smt_sites";
    internal const string CacheKeyLines = "smt_lines";

    // ============ 缓存状态 ============

    /// <summary>两个下拉框的缓存是否都已存在</summary>
    public static bool HasCache()
        => FileCache.Exists(CacheKeySites) && FileCache.Exists(CacheKeyLines);

    // ============ 站点：加载（带缓存） ============

    /// <summary>
    /// 加载站点。优先读缓存；reload=true 时强制刷新。
    /// 注意：缓存未命中时会同步查询数据库（约 20 秒），调用方需自行放到后台线程。
    /// </summary>
    public static List<string> LoadSites(bool reload = false)
    {
        if (!reload)
        {
            var cached = FileCache.Load<string>(CacheKeySites);
            if (cached != null && cached.Count > 0)
            {
                Logger.Debug($"[SMTREEL] Sites cache hit: {cached.Count}");
                return cached;
            }
        }

        Logger.Info("[SMTREEL] Loading sites from DB...");
        var data = LoadSitesFromDb();
        if (data.Count > 0) FileCache.Save(CacheKeySites, data);
        return data;
    }

    private static List<string> LoadSitesFromDb()
    {
        const string sql = @"
            SELECT DISTINCT STRSITE
            FROM TBL_SMT_REELUPINFO@smt
            WHERE STRSITE IS NOT NULL
            ORDER BY STRSITE";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<string>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            string s = OracleHelper.GetStr(r, "STRSITE");
            if (s.Length > 0) list.Add(s);
        }
        return list;
    }

    // ============ 线别：加载（带缓存） ============

    /// <summary>
    /// 加载线别（STRLINEID LIKE 'B%'）。优先读缓存；reload=true 时强制刷新。
    /// 缓存未命中时会同步查询数据库（约 20 秒），调用方需自行放到后台线程。
    /// </summary>
    public static List<string> LoadLines(bool reload = false)
    {
        if (!reload)
        {
            var cached = FileCache.Load<string>(CacheKeyLines);
            if (cached != null && cached.Count > 0)
            {
                Logger.Debug($"[SMTREEL] Lines cache hit: {cached.Count}");
                return cached;
            }
        }

        Logger.Info("[SMTREEL] Loading lines from DB...");
        var data = LoadLinesFromDb();
        if (data.Count > 0) FileCache.Save(CacheKeyLines, data);
        return data;
    }

    private static List<string> LoadLinesFromDb()
    {
        const string sql = @"
            SELECT DISTINCT STRLINEID
            FROM TBL_SMT_REELUPINFO@smt
            WHERE STRLINEID LIKE 'B%'
            ORDER BY STRLINEID";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<string>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            string s = OracleHelper.GetStr(r, "STRLINEID");
            if (s.Length > 0) list.Add(s);
        }
        return list;
    }

    // ============ 按站点 + 线别 + 上料时间查询 ============

    public static List<ReelInfo> QueryReelInfo(string site, string line, DateTime inputTime)
    {
        const string sql = @"
            SELECT S.NUMINDEX, S.STRSITE, S.STRLINEID, S.STRREELUPSN,
                   E.EMP_NAME, S.NUMREMAINQTY, S.LOADRELLDATE, S.OVERDATE,
                   S.STROLDREELUPSN, S.NUMQTY, S.STRACTIVE
            FROM TBL_SMT_REELUPINFO@smt S
            LEFT JOIN SAJET.SYS_EMP E ON E.EMP_NO = S.STRLOADUSER
            WHERE S.STRSITE = :site
              AND S.STRLINEID = :line
              AND S.LOADRELLDATE > :inputTime
            ORDER BY S.LOADRELLDATE DESC";

        var ps = new Dictionary<string, object>
        {
            { "site",      site },
            { "line",      line },
            { "inputTime", inputTime },
        };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<ReelInfo>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new ReelInfo
            {
                NumIndex = OracleHelper.GetInt(r, "NUMINDEX"),
                Site = OracleHelper.GetStr(r, "STRSITE"),
                LineId = OracleHelper.GetStr(r, "STRLINEID"),
                ReelUpSn = OracleHelper.GetStr(r, "STRREELUPSN"),
                EmpName = OracleHelper.GetStr(r, "EMP_NAME"),
                RemainQty = r["NUMREMAINQTY"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["NUMREMAINQTY"]),
                LoadReelDate = OracleHelper.GetDate(r, "LOADRELLDATE"),
                OverDate = OracleHelper.GetDate(r, "OVERDATE"),
                OldReelUpSn = OracleHelper.GetStr(r, "STROLDREELUPSN"),
                Qty = r["NUMQTY"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["NUMQTY"]),
                Active = OracleHelper.GetStr(r, "STRACTIVE"),
            });
        }
        return list;
    }

    // ============ 修改状态 ============

    public static int UpdateActive(int numIndex, string reelUpSn, string active)
    {
        const string sql = @"
            UPDATE TBL_SMT_REELUPINFO@smt
               SET STRACTIVE = :active
             WHERE NUMINDEX = :numIndex AND STRREELUPSN = :sn";

        var ps = new Dictionary<string, object>
        {
            { "active",   active },
            { "numIndex", numIndex },
            { "sn",       reelUpSn },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 统计同一 Reel SN 的记录数 ============

    public static int GetReelUpSnCount(string reelUpSn)
    {
        const string sql =
            "SELECT COUNT(*) FROM TBL_SMT_REELUPINFO@smt WHERE STRREELUPSN = :sn";

        var ps = new Dictionary<string, object> { { "sn", reelUpSn } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt32(result);
    }

    // ============ 删除单条记录 ============

    public static int DeleteReel(int numIndex, string reelUpSn)
    {
        const string sql = @"
            DELETE FROM TBL_SMT_REELUPINFO@smt
             WHERE NUMINDEX = :numIndex AND STRREELUPSN = :sn";

        var ps = new Dictionary<string, object>
        {
            { "numIndex", numIndex },
            { "sn",       reelUpSn },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }
}