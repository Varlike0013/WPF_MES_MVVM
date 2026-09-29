using System.Data;
using Oracle.ManagedDataAccess.Client;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// TGS 行为组测试相关操作
/// </summary>
internal static class TgsGroupTestService
{
    // ============ 加载产线 ============

    public static List<string> LoadPdLines()
        => SajetCommonService.GetPDLines().Select(x => x.Name).ToList();

    // ============ 按产线加载工序 ============

    public static List<string> LoadProcesses(string lineName)
    {
        // 和 ServerIp / ExportTravels 一样的逻辑
        const string sql = @"
            SELECT DISTINCT PR.PROCESS_NAME
            FROM SAJET.SYS_TERMINAL PT
            LEFT JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = PT.PROCESS_ID
            WHERE PT.ENABLED = 'Y' AND PT.CONTROL_ID <> 0
              AND PT.PDLINE_ID = (
                  SELECT PL.PDLINE_ID FROM SAJET.SYS_PDLINE PL
                  WHERE PL.PDLINE_NAME = :line
              )
            ORDER BY PR.PROCESS_NAME";

        var ps = new Dictionary<string, object> { { "line", lineName } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<string>();
        foreach (DataRow r in dt.Rows)
        {
            string name = OracleHelper.GetStr(r, "PROCESS_NAME");
            if (name.Length > 0) list.Add(name);
        }
        return list;
    }

    // ============ 按产线+工序加载终端 ============

    public static List<TgsTerminalItem> LoadTerminals(string lineName, string processName)
    {
        const string sql = @"
            SELECT T.TERMINAL_ID, T.TERMINAL_NAME, T.PDLINE_ID,
                   T.PROCESS_ID, T.STAGE_ID
            FROM SAJET.SYS_TERMINAL T
            JOIN SAJET.SYS_PROCESS PR ON PR.PROCESS_ID = T.PROCESS_ID
            WHERE T.PDLINE_ID = (
                SELECT P.PDLINE_ID FROM SAJET.SYS_PDLINE P
                WHERE P.PDLINE_NAME = :line
            )
              AND T.ENABLED = 'Y'
              AND PR.PROCESS_NAME = :process
              AND T.CONTROL_ID <> 0
            ORDER BY T.TERMINAL_NAME";

        var ps = new Dictionary<string, object>
        {
            { "line",    lineName },
            { "process", processName },
        };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<TgsTerminalItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new TgsTerminalItem
            {
                TerminalId = OracleHelper.GetStr(r, "TERMINAL_ID"),
                TerminalName = OracleHelper.GetStr(r, "TERMINAL_NAME"),
                PdlineId = OracleHelper.GetStr(r, "PDLINE_ID"),
                ProcessId = OracleHelper.GetStr(r, "PROCESS_ID"),
                StageId = OracleHelper.GetStr(r, "STAGE_ID"),
            });
        }
        return list;
    }

    // ============ 按行为 ID 查 Job 列表 ============

    public static List<GroupJobInfo> LoadGroupJobs(int groupId)
    {
        const string sql = @"
            SELECT T.GROUP_ID, T.GROUP_DESC_E,
                   G.JOB_ID, G.GROUP_SEQ, G.SEQ_ELSE, G.SEQ_OTHER, G.VALUE_KIND,
                   J.JOB_DESC_E, J.TYPE_ID,
                   B.TYPE_NAME_E, B.PROC_CALL_NAME
            FROM SAJET.TGS_GROUP_BASE T
            INNER JOIN SAJET.TGS_GROUP_LINK G ON G.GROUP_ID = T.GROUP_ID
            INNER JOIN SAJET.TGS_JOB_BASE J ON J.JOB_ID = G.JOB_ID
            INNER JOIN SAJET.TGS_JOB_TYPE_BASE B ON B.TYPE_ID = J.TYPE_ID
            WHERE T.GROUP_ID = :id
            ORDER BY G.GROUP_SEQ";

        var ps = new Dictionary<string, object> { { "id", groupId } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<GroupJobInfo>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new GroupJobInfo
            {
                GroupId = OracleHelper.GetStr(r, "GROUP_ID"),
                GroupName = OracleHelper.GetStr(r, "GROUP_DESC_E"),
                JobId = OracleHelper.GetStr(r, "JOB_ID"),
                JobDesc = OracleHelper.GetStr(r, "JOB_DESC_E"),
                GroupSeq = OracleHelper.GetStr(r, "GROUP_SEQ"),
                SeqElse = OracleHelper.GetStr(r, "SEQ_ELSE"),
                SeqOther = OracleHelper.GetStr(r, "SEQ_OTHER"),
                ValueKind = OracleHelper.GetStr(r, "VALUE_KIND"),
                TypeId = OracleHelper.GetStr(r, "TYPE_ID"),
                TypeNameE = OracleHelper.GetStr(r, "TYPE_NAME_E"),
                ProcCallName = OracleHelper.GetStr(r, "PROC_CALL_NAME"),
            });
        }
        return list;
    }

    // ============ 按 GroupId + JobId 查存储过程列表 ============

    public static List<JobDetailInfo> LoadJobDetails(string groupId, string jobId)
    {
        const string sql = @"
            SELECT L.JOB_SEQ, L.SPROC_NAME
            FROM SAJET.TGS_GROUP_BASE T
            INNER JOIN SAJET.TGS_GROUP_LINK G ON G.GROUP_ID = T.GROUP_ID
            INNER JOIN SAJET.TGS_JOB_BASE J ON J.JOB_ID = G.JOB_ID
            INNER JOIN SAJET.TGS_JOB_LINK L ON L.JOB_ID = J.JOB_ID
            WHERE T.GROUP_ID = :groupid AND J.JOB_ID = :jobid
            ORDER BY L.JOB_SEQ";

        var ps = new Dictionary<string, object>
        {
            { "groupid", groupId },
            { "jobid",   jobId },
        };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<JobDetailInfo>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new JobDetailInfo
            {
                JobSeq = OracleHelper.GetStr(r, "JOB_SEQ"),
                SprocName = OracleHelper.GetStr(r, "SPROC_NAME"),
            });
        }
        return list;
    }

    // ============ 执行存储过程（按参数名，值从 dataMap / TREV / TNOW） ============

    /// <summary>
    /// 执行一个存储过程。
    /// </summary>
    /// <param name="procName">OWNER.NAME</param>
    /// <param name="dataMap">全局键值对</param>
    /// <param name="trev">当前 TREV</param>
    /// <param name="tresValue">TRES 输出值</param>
    /// <param name="outParams">所有输出参数（名 -> 值），除 TRES 外可用于保存</param>
    /// <returns>日志文本</returns>
    public static string ExecuteProcedure(
        string procName,
        Dictionary<string, string> dataMap,
        string trev,
        out string tresValue,
        out Dictionary<string, string> outParams)
    {
        tresValue = string.Empty;
        outParams = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(procName))
            return "[WARNING][Procedure name is empty, skipped]";

        var parts = procName.Split('.');
        if (parts.Length < 2)
            return $"[WARNING][Invalid proc name: {procName}]";

        string owner = parts[0].Trim();
        string name = parts[1].Trim();

        // 1. 获取参数列表
        var (inParams, outParamNames) = TgsGroupService.GetProcedureParams(procName);
        if (inParams.Count == 0 && outParamNames.Count == 0)
            return $"[WARNING][{procName}] No parameter info (insufficient privileges or not a procedure)";

        // 2. 构建调用语句
        int total = inParams.Count + outParamNames.Count;
        var placeholders = new List<string>();
        for (int i = 0; i < total; i++)
            placeholders.Add($":p{i}");

        string procCall = $"BEGIN {owner}.{name}({string.Join(", ", placeholders)}); END;";

        using var conn = OracleHelper.GetConnection();
        using var cmd = new OracleCommand(procCall, conn);

        // 3. 绑定 IN 参数
        var inParamLog = new List<string>();
        for (int i = 0; i < inParams.Count; i++)
        {
            string pName = inParams[i];
            string value;

            if (pName == "TREV")
                value = trev;
            else if (pName == "TNOW")
                value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            else
                value = dataMap.TryGetValue(pName, out var v) ? v : string.Empty;

            cmd.Parameters.Add($"p{i}", OracleDbType.Varchar2).Value =
                string.IsNullOrEmpty(value) ? DBNull.Value : value;

            inParamLog.Add($"{pName}={(string.IsNullOrEmpty(value) ? "空" : value)}");
        }

        // 4. 绑定 OUT 参数
        for (int i = 0; i < outParamNames.Count; i++)
        {
            string pName = $"p{inParams.Count + i}";
            var outParam = new OracleParameter(pName, OracleDbType.Varchar2, 4000)
            {
                Direction = ParameterDirection.Output,
            };
            cmd.Parameters.Add(outParam);
        }

        // 5. 执行
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            return $"[ERROR][Execute failed: {ex.Message}]";
        }

        // 6. 收集输出
        var outParamLog = new List<string>();
        for (int i = 0; i < outParamNames.Count; i++)
        {
            string pName = $"p{inParams.Count + i}";
            var p = cmd.Parameters[pName];
            string value = p.Value?.ToString()?.Trim() ?? string.Empty;

            if (outParamNames[i] == "TRES")
                tresValue = value;

            outParams[outParamNames[i]] = value;
            outParamLog.Add($"{outParamNames[i]}={(string.IsNullOrEmpty(value) ? "空" : value)}");
        }

        // 7. 组装日志
        string log = $"[INFO][Procedure: {procName} | IN: {string.Join(", ", inParamLog)}";
        if (outParamLog.Count > 0)
            log += $" | OUT: {string.Join(", ", outParamLog)}";
        log += "]";

        return log;
    }
}