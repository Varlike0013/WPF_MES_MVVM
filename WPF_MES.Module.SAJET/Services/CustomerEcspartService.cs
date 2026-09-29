using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 客户料号映射相关操作
/// </summary>
internal static class CustomerEcspartService
{
    /// <summary>远程表（通过 DBLINK）</summary>
    private const string RemoteTable = "LCRECSM.ECS_CUS_PART_MAPPING@smt";

    // ============ 加载客户代码下拉 ============

    public static List<CusCodeOption> LoadCusCodes()
    {
        string sql = $@"
            SELECT DISTINCT L.CUS_CODE
            FROM {RemoteTable} L
            WHERE L.CUS_CODE IS NOT NULL
            ORDER BY L.CUS_CODE";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<CusCodeOption>
        {
            new("全部", ""),   // 默认项
        };

        foreach (DataRow r in dt.Rows)
        {
            string code = OracleHelper.GetStr(r, "CUS_CODE");
            if (code.Length > 0)
                list.Add(new CusCodeOption(code, code));
        }
        return list;
    }

    // ============ 查询 ============

    /// <summary>
    /// 按条件查询（模糊匹配，时间可选）
    /// </summary>
    public static List<EcsCusPartItem> Query(
        string ecsPart,
        string cusPart,
        string cusCode,
        bool useTime,
        DateTime? time)
    {
        var whereClauses = new List<string>();
        var ps = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(ecsPart))
        {
            whereClauses.Add("L.ECS_PART LIKE :ecs");
            ps["ecs"] = ecsPart + "%";
        }

        if (!string.IsNullOrEmpty(cusPart))
        {
            whereClauses.Add("L.CUS_PART LIKE :cus");
            ps["cus"] = cusPart + "%";
        }

        if (!string.IsNullOrEmpty(cusCode))
        {
            whereClauses.Add("L.CUS_CODE = :code");
            ps["code"] = cusCode;
        }

        if (useTime && time.HasValue)
        {
            whereClauses.Add("L.UPDATE_DATE > :time");
            ps["time"] = time.Value;
        }

        string whereStr = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        string sql = $@"
            SELECT L.ECS_PART, L.CUS_PART, L.CUS_CODE,
                   TO_CHAR(L.UPDATE_DATE, 'YYYY-MM-DD HH24:MI:SS') AS UPDATE_DATE
            FROM {RemoteTable} L
            {whereStr}
            ORDER BY L.UPDATE_DATE DESC";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<EcsCusPartItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new EcsCusPartItem
            {
                EcsPart = OracleHelper.GetStr(r, "ECS_PART"),
                CusPart = OracleHelper.GetStr(r, "CUS_PART"),
                CusCode = OracleHelper.GetStr(r, "CUS_CODE"),
                UpdateDate = OracleHelper.GetStr(r, "UPDATE_DATE"),
            });
        }
        return list;
    }

    // ============ 添加 ============

    public static int Add(string ecsPart, string cusPart, string cusCode)
    {
        string sql = $@"
            INSERT INTO {RemoteTable} 
                (ECS_PART, CUS_PART, CUS_CODE, UPDATE_DATE)
            VALUES 
                (:ecs, :cus, :code, SYSDATE)";

        var ps = new Dictionary<string, object>
        {
            { "ecs",  ecsPart },
            { "cus",  cusPart },
            { "code", cusCode },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 删除 ============

    public static int Delete(string ecsPart, string cusPart, string cusCode)
    {
        string sql = $@"
            DELETE FROM {RemoteTable}
            WHERE ECS_PART = :ecs
              AND CUS_PART = :cus
              AND CUS_CODE = :code";

        var ps = new Dictionary<string, object>
        {
            { "ecs",  ecsPart },
            { "cus",  cusPart },
            { "code", cusCode },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }
}