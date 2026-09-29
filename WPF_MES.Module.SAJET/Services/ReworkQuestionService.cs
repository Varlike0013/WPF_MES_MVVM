using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 重工问题追踪相关操作
/// </summary>
internal static class ReworkQuestionService
{
    // ============ 查询 ============

    /// <summary>
    /// 按条件查询重工问题（最多 50 条）
    /// </summary>
    /// <param name="floorKey">楼层下拉索引：0=ALL, 1=B2, 2=B3, 3=B31, 4=B32, 5=B33, 6=B4, 7=B41, 8=B42, 9=B43</param>
    /// <param name="startTime">起始时间</param>
    /// <param name="keyword">关键词（序号 或 原因）</param>
    /// <param name="showUnfinishedOnly">仅显示未完成</param>
    public static List<ReworkQuestionItem> Query(
        int floorKey,
        DateTime startTime,
        string keyword,
        bool showUnfinishedOnly)
    {
        var where = new List<string>();
        var ps = new Dictionary<string, object>();

        // 1. 时间范围：起始 → 当前
        where.Add("R.CREATEDATE >= :startTime");
        where.Add("R.CREATEDATE <= :endTime");
        ps["startTime"] = startTime;
        ps["endTime"] = DateTime.Now;

        // 2. 楼层条件
        string floorClause = BuildFloorClause(floorKey);
        if (!string.IsNullOrEmpty(floorClause))
            where.Add(floorClause);

        // 3. 未完成筛选
        if (showUnfinishedOnly)
        {
            where.Add("R.CHECKED = 'Y'");
            where.Add("R.CREATEDATE IS NOT NULL");
            where.Add("R.DUALTIME IS NULL");
        }

        // 4. 关键词（序号 或 原因）
        string kw = keyword.Trim();
        if (kw.Length > 0)
        {
            where.Add("(R.SERIAL_NUMBER LIKE :kw OR R.REASON LIKE :kw)");
            ps["kw"] = "%" + kw + "%";
        }

        string whereStr = "WHERE " + string.Join(" AND ", where);

        string sql = $@"
            SELECT * FROM (
                SELECT R.NUMBERINDEX, R.PDLINE_NAME, R.SERIAL_NUMBER,
                       R.REASON, R.QUERY, R.QEMP,
                       TO_CHAR(R.CREATEDATE, 'YYYY-MM-DD HH24:MI:SS') AS CREATEDATE,
                       R.CEMP, R.CHECKED, R.EMP_SFIS, R.REWORK_NO,
                       TO_CHAR(R.DUALTIME, 'YYYY-MM-DD HH24:MI:SS') AS DUALTIME
                FROM SAJET.ECS_SN_REWORK R
                {whereStr}
                ORDER BY R.NUMBERINDEX DESC
            ) WHERE ROWNUM <= 50";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<ReworkQuestionItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
            list.Add(MapRow(r));

        return list;
    }

    // ============ 回复 ============

    /// <summary>
    /// 回复一条重工问题：更新处理人、完成时间、回复内容
    /// </summary>
    /// <returns>受影响行数</returns>
    public static int Reply(int numberIndex, string empNo, string reply)
    {
        const string sql = @"
            UPDATE SAJET.ECS_SN_REWORK
            SET EMP_SFIS = (SELECT EMP_NAME FROM SAJET.SYS_EMP WHERE EMP_NO = :empNo),
                DUALTIME = SYSDATE,
                REWORK_NO = :reply
            WHERE NUMBERINDEX = :idx";

        var ps = new Dictionary<string, object>
        {
            { "empNo", empNo },
            { "reply", reply },
            { "idx",   numberIndex },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }

    // ============ 私有：楼层条件 ============

    private static string BuildFloorClause(int floorKey)
    {
        return floorKey switch
        {
            0 => "(R.PDLINE_NAME LIKE 'B2%' OR R.PDLINE_NAME LIKE 'B3%' OR R.PDLINE_NAME LIKE 'B4%')",
            1 => "R.PDLINE_NAME LIKE 'B2%'",
            2 => "R.PDLINE_NAME LIKE 'B3%'",
            3 => "R.PDLINE_NAME LIKE 'B31%'",
            4 => "R.PDLINE_NAME LIKE 'B32%'",
            5 => "R.PDLINE_NAME LIKE 'B33%'",
            6 => "R.PDLINE_NAME LIKE 'B4%'",
            7 => "R.PDLINE_NAME LIKE 'B41%'",
            8 => "R.PDLINE_NAME LIKE 'B42%'",
            9 => "R.PDLINE_NAME LIKE 'B43%'",
            _ => string.Empty,
        };
    }

    // ============ 私有：映射 ============

    private static ReworkQuestionItem MapRow(DataRow r)
    {
        return new ReworkQuestionItem
        {
            NumberIndex = OracleHelper.GetInt(r, "NUMBERINDEX"),
            PdlineName = OracleHelper.GetStr(r, "PDLINE_NAME"),
            SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),
            Reason = OracleHelper.GetStr(r, "REASON"),
            Query = OracleHelper.GetStr(r, "QUERY"),
            QEmp = OracleHelper.GetStr(r, "QEMP"),
            CreateDate = OracleHelper.GetStr(r, "CREATEDATE"),
            CEmp = OracleHelper.GetStr(r, "CEMP"),
            Checked = OracleHelper.GetStr(r, "CHECKED"),
            EmpSfis = OracleHelper.GetStr(r, "EMP_SFIS"),
            ReworkNo = OracleHelper.GetStr(r, "REWORK_NO"),
            DualTime = OracleHelper.GetStr(r, "DUALTIME"),
        };
    }
}