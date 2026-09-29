using System.Data;
using Oracle.ManagedDataAccess.Client;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// TGS 行为组相关操作
/// </summary>
internal static class TgsGroupService
{
    // ============ 查询行为组 ============

    /// <summary>
    /// 按条件查询行为组下的流程
    /// </summary>
    /// <param name="queryType">0=行为ID 1=行为名称</param>
    public static (string GroupId, string GroupName, List<TgsJobItem> Items) Query(
        int queryType, string input)
    {
        string whereClause = queryType == 0
            ? "T.GROUP_ID = :input"
            : "T.GROUP_DESC_E = :input";

        string sql = $@"
            SELECT T.GROUP_ID, T.GROUP_DESC_E, T.GROUP_DESC_C, T.ENABLED,
                   G.JOB_ID, B.TYPE_NAME_E, B.PROC_CALL_NAME,
                   J.JOB_DESC_E, J.JOB_DESC_C,
                   L.JOB_SEQ, L.SPROC_NAME
            FROM SAJET.TGS_GROUP_BASE T
            INNER JOIN SAJET.TGS_GROUP_LINK G ON G.GROUP_ID = T.GROUP_ID
            INNER JOIN SAJET.TGS_JOB_BASE J ON J.JOB_ID = G.JOB_ID
            INNER JOIN SAJET.TGS_JOB_LINK L ON L.JOB_ID = J.JOB_ID
            INNER JOIN SAJET.TGS_JOB_TYPE_BASE B ON B.TYPE_ID = J.TYPE_ID
            WHERE {whereClause}
            ORDER BY G.GROUP_SEQ, L.JOB_SEQ";

        var ps = new Dictionary<string, object> { { "input", input } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        string groupId = string.Empty;
        string groupName = string.Empty;
        var items = new List<TgsJobItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            if (groupId.Length == 0)
            {
                groupId = OracleHelper.GetStr(r, "GROUP_ID");
                groupName = OracleHelper.GetStr(r, "GROUP_DESC_E");
            }

            items.Add(new TgsJobItem
            {
                JobId = OracleHelper.GetStr(r, "JOB_ID"),
                TypeNameE = OracleHelper.GetStr(r, "TYPE_NAME_E"),
                ProcCallName = OracleHelper.GetStr(r, "PROC_CALL_NAME"),
                SprocName = OracleHelper.GetStr(r, "SPROC_NAME"),
            });
        }

        return (groupId, groupName, items);
    }

    // ============ 查存储过程源码 ============

    /// <summary>
    /// 获取存储过程源码（从 ALL_SOURCE 查）。
    /// 传入格式："OWNER.NAME"，返回完整源代码。
    /// </summary>
    public static string GetProcedureSource(string procName)
    {
        if (string.IsNullOrEmpty(procName)) return string.Empty;

        var parts = procName.Split('.');
        if (parts.Length < 2)
            return $"存储过程解析失败：{procName}";

        string owner = parts[0].Trim();
        string name = parts[1].Trim();

        const string sql = @"
        SELECT TEXT FROM ALL_SOURCE
        WHERE OWNER = :owner
          AND NAME = :name
          AND TYPE = 'PROCEDURE'
        ORDER BY LINE";

        var ps = new Dictionary<string, object>
    {
        { "owner", owner },
        { "name",  name },
    };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        if (dt.Rows.Count == 0)
            return $"未找到存储过程 {procName} 的源代码（可能权限不足或名称错误）";

        var sb = new System.Text.StringBuilder();

        foreach (DataRow r in dt.Rows)
        {
            if (r[0] == DBNull.Value) continue;

            // 关键1：不要 Trim！保留缩进
            string line = r[0].ToString() ?? string.Empty;

            // 关键2：每行末尾加换行符
            sb.Append(line);
            sb.Append("\r\n");
        }

        return sb.ToString();
    }

    // ============ 查存储过程参数 ============

    /// <summary>
    /// 获取存储过程的输入/输出参数名
    /// </summary>
    public static (List<string> InParams, List<string> OutParams) GetProcedureParams(
        string procName)
    {
        var inParams = new List<string>();
        var outParams = new List<string>();

        if (string.IsNullOrEmpty(procName)) return (inParams, outParams);

        var parts = procName.Split('.');
        if (parts.Length < 2) return (inParams, outParams);

        string owner = parts[0].Trim();
        string name = parts[1].Trim();

        const string sql = @"
            SELECT ARGUMENT_NAME, IN_OUT FROM ALL_ARGUMENTS
            WHERE OWNER = :owner
              AND OBJECT_NAME = :name
              AND PACKAGE_NAME IS NULL
              AND ARGUMENT_NAME IS NOT NULL
              AND POSITION > 0
            ORDER BY POSITION";

        var ps = new Dictionary<string, object>
        {
            { "owner", owner },
            { "name",  name },
        };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        foreach (DataRow r in dt.Rows)
        {
            string argName = OracleHelper.GetStr(r, "ARGUMENT_NAME").ToUpper().Trim();
            string inOut = OracleHelper.GetStr(r, "IN_OUT").ToUpper().Trim();

            if (inOut == "IN") inParams.Add(argName);
            else if (inOut == "OUT") outParams.Add(argName);
        }

        return (inParams, outParams);
    }

    // ============ 执行存储过程 ============

    /// <summary>
    /// 执行存储过程，返回输出参数的值（按顺序）
    /// </summary>
    /// <param name="procName">存储过程名 OWNER.NAME</param>
    /// <param name="inParams">输入参数名（顺序）</param>
    /// <param name="inValues">输入参数值（对应顺序）</param>
    /// <param name="outParamNames">输出参数名（顺序）</param>
    /// <returns>输出参数值（顺序对应 outParamNames）</returns>
    public static List<string> ExecuteProcedure(
        string procName,
        List<string> inParams,
        List<string> inValues,
        List<string> outParamNames)
    {
        var parts = procName.Split('.');
        if (parts.Length < 2)
            throw new Exception($"存储过程解析失败：{procName}");

        string owner = parts[0].Trim();
        string name = parts[1].Trim();

        int totalParams = inParams.Count + outParamNames.Count;

        // 构建占位符 :p0, :p1, ...
        var placeholders = new List<string>();
        for (int i = 0; i < totalParams; i++)
            placeholders.Add($":p{i}");

        string procCall = $"BEGIN {owner}.{name}({string.Join(", ", placeholders)}); END;";

        using var conn = OracleHelper.GetConnection();
        using var cmd = new OracleCommand(procCall, conn);

        // 绑定 IN 参数
        for (int i = 0; i < inParams.Count; i++)
        {
            string pName = $"p{i}";
            string val = inValues[i];
            cmd.Parameters.Add(pName, OracleDbType.Varchar2).Value =
                string.IsNullOrEmpty(val) ? DBNull.Value : val;
        }

        // 绑定 OUT 参数
        var outParamResults = new List<string>();
        for (int i = 0; i < outParamNames.Count; i++)
        {
            string pName = $"p{inParams.Count + i}";
            var outParam = new OracleParameter(pName, OracleDbType.Varchar2, 4000)
            {
                Direction = ParameterDirection.Output,
            };
            cmd.Parameters.Add(outParam);
        }

        cmd.ExecuteNonQuery();

        // 收集输出值
        for (int i = 0; i < outParamNames.Count; i++)
        {
            string pName = $"p{inParams.Count + i}";
            var p = cmd.Parameters[pName];
            outParamResults.Add(p.Value?.ToString()?.Trim() ?? string.Empty);
        }

        return outParamResults;
    }
}