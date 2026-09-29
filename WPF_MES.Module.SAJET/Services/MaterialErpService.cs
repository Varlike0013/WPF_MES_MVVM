using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// ERP 物料维护相关操作
/// </summary>
internal static class MaterialErpService
{
    /// <summary>类型下拉的固定三项</summary>
    public static readonly string[] TypeOptions =
    {
        "FAN SET",
        "HEAT PIPE",
        "PLATE",
    };

    // ============ 查询 ============

    /// <summary>
    /// 按工单号查询物料信息
    /// </summary>
    public static List<ErpMaterialItem> Query(string workOrder)
    {
        const string sql = @"
            SELECT M.WORK_ORDER, M.ECS_PN, M.ECS_PN_DESC,
                   M.KPARTS_NO,
                   TO_CHAR(M.CREATE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS CREATE_TIME
            FROM SAJET.ERP_WO_MATERIAL M
            WHERE M.WORK_ORDER = :wo
            ORDER BY M.CREATE_TIME";

        var ps = new Dictionary<string, object> { { "wo", workOrder } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var list = new List<ErpMaterialItem>(dt.Rows.Count);
        foreach (DataRow r in dt.Rows)
        {
            list.Add(new ErpMaterialItem
            {
                WorkOrder = OracleHelper.GetStr(r, "WORK_ORDER"),
                EcsPn = OracleHelper.GetStr(r, "ECS_PN"),
                EcsPnDesc = OracleHelper.GetStr(r, "ECS_PN_DESC"),
                KpartsNo = OracleHelper.GetStr(r, "KPARTS_NO"),
                CreateTime = OracleHelper.GetStr(r, "CREATE_TIME"),
            });
        }
        return list;
    }
    // ============ 添加（调存储过程） ============

    /// <summary>
    /// 调用存储过程 INSERT_WO_MATERIA 添加物料
    /// </summary>
    /// <returns>存储过程返回的结果消息</returns>
    public static string Add(
        string workOrder,
        string ecsPn,
        string ecsPnDesc,
        string kpartsNo)
    {
        using var conn = OracleHelper.GetConnection();
        using var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(
            "SAJET.INSERT_WO_MATERIA", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };

        cmd.Parameters.Add("wo_part",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = workOrder;
        cmd.Parameters.Add("ecs_part",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = ecsPn;
        cmd.Parameters.Add("decs",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = ecsPnDesc;
        cmd.Parameters.Add("kpart",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = kpartsNo;

        var outParam = new Oracle.ManagedDataAccess.Client.OracleParameter(
            "result",
            Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2,
            500)
        {
            Direction = ParameterDirection.Output,
        };
        cmd.Parameters.Add(outParam);

        cmd.ExecuteNonQuery();

        return outParam.Value?.ToString()?.Trim() ?? string.Empty;
    }

    // ============ 删除 ============

    /// <summary>
    /// 删除物料记录（三字段精确定位）
    /// </summary>
    /// <returns>受影响行数</returns>
    public static int Delete(
        string workOrder,
        string ecsPn,
        string kpartsNo)
    {
        const string sql = @"
            DELETE FROM SAJET.ERP_WO_MATERIAL
            WHERE WORK_ORDER = :wo
              AND ECS_PN = :ecs
              AND KPARTS_NO = :kpart";

        var ps = new Dictionary<string, object>
        {
            { "wo",    workOrder },
            { "ecs",   ecsPn },
            { "kpart", kpartsNo },
        };

        return OracleHelper.ExecuteNonQuery(sql, ps);
    }
}