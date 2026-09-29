using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;
using WPF_MES.Shared.Cache;

namespace WPF_MES.Module.SAJET.Services;

internal static class SmtReelInfoService
{
    // ============ 缓存参数 ============

    private const string CacheFile = "SmtReel";
    private const string KeySites  = "sites";
    private const string KeyLines  = "lines";

    private static readonly object _refreshLock = new();

    /// <summary>是否两部分缓存都存在</summary>
    public static bool HasCache()
        => FileCache.Exists(CacheFile, KeySites)
        && FileCache.Exists(CacheFile, KeyLines);

    /// <summary>读取现有缓存（UI 线程用，无网络）</summary>
    public static (List<string> Sites, List<string> Lines) GetCached()
    {
        var sites = FileCache.Load<string>(CacheFile, KeySites) ?? new();
        var lines = FileCache.Load<string>(CacheFile, KeyLines) ?? new();
        return (sites, lines);
    }

    /// <summary>
    /// 从 DB 重新拉取并写缓存。内部并行查询。
    /// 同步阻塞，调用方需放到 Task.Run 里。
    /// </summary>
    public static (List<string> Sites, List<string> Lines) Refresh()
    {
        lock (_refreshLock)
        {
            Logger.Info("[SMTREEL] Refreshing sites/lines from DB...");

            var sitesTask = Task.Run(LoadSitesFromDb);
            var linesTask = Task.Run(LoadLinesFromDb);
            Task.WaitAll(sitesTask, linesTask);

            var sites = sitesTask.Result;
            var lines = linesTask.Result;

            if (sites.Count > 0) FileCache.Save(CacheFile, KeySites, sites);
            if (lines.Count > 0) FileCache.Save(CacheFile, KeyLines, lines);

            Logger.Info($"[SMTREEL] Refreshed: sites={sites.Count}, lines={lines.Count}");
            return (sites, lines);
        }
    }

    /// <summary>有缓存直接返回；否则从 DB 加载。</summary>
    public static (List<string> Sites, List<string> Lines) LoadOrRefresh()
        => HasCache() ? GetCached() : Refresh();

    // ============ DB 查询 ============

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