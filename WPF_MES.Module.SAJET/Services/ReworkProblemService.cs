using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

internal static class ReworkProblemService
{
    // ============ 查询未处理问题 ============
    public static List<ReworkProblemItem> QueryUnresolvedProblems()
    {
        const string sql = @"
            SELECT R.NUMBERINDEX, R.PDLINE_NAME, R.SERIAL_NUMBER, R.REASON || R.QUERY AS REASON_QUERY,
                   EQ.EMP_NAME AS SUBMITTER_NAME, 
                   EC.EMP_NAME AS CONFIRMER_NAME,
                   R.EMP_SFIS, R.REWORK_NO, R.DUALTIME
            FROM SAJET.ECS_SN_REWORK R
            LEFT JOIN SAJET.SYS_EMP EC ON EC.EMP_NO = R.CEMP
            LEFT JOIN SAJET.SYS_EMP EQ ON EQ.EMP_NO = R.QEMP
            WHERE R.CHECKED = 'Y' 
              AND R.CREATEDATE > SYSDATE - 14 
              AND R.DUALTIME IS NULL
            ORDER BY R.NUMBERINDEX DESC";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<ReworkProblemItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new ReworkProblemItem
            {
                NumberIndex = OracleHelper.GetInt(r, "NUMBERINDEX"),
                PdLineName = OracleHelper.GetStr(r, "PDLINE_NAME"),
                SerialNumber = OracleHelper.GetStr(r, "SERIAL_NUMBER"),
                ReasonQuery = OracleHelper.GetStr(r, "REASON_QUERY"),
                SubmitterName = OracleHelper.GetStr(r, "SUBMITTER_NAME"),
                ConfirmerName = OracleHelper.GetStr(r, "CONFIRMER_NAME"),
                EmpSfis = OracleHelper.GetStr(r, "EMP_SFIS"),
                ReworkNo = OracleHelper.GetStr(r, "REWORK_NO"),
                DualTime = OracleHelper.GetDate(r, "DUALTIME"),
            });
        }

        return list;
    }

    // ============ 更新处理结果 ============
    public static int CompleteProblem(int numberIndex, string empName, string result)
    {
        const string sql = @"
            UPDATE SAJET.ECS_SN_REWORK
            SET EMP_SFIS = :empName,
                REWORK_NO = :result,
                DUALTIME = SYSDATE
            WHERE NUMBERINDEX = :numIndex";

        var ps = new Dictionary<string, object>
        {
            { "empName", empName },
            { "result", result },
            { "numIndex", numberIndex }
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }
}