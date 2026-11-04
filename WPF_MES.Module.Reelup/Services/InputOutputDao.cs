using System.Data;
using Oracle.ManagedDataAccess.Client;
using WPF_MES.Module.ReelUp.Models;

namespace WPF_MES.Module.ReelUp.Services;

public sealed class InputOutputDao
{
    // ========== insertSN ==========
    public void InsertSn(string sn, string line, int site)
    {
        using var conn = ReelUpDbHelper.GetConnection();
        using var cmd = new OracleCommand("SMTUSER.INSERTSN", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        // 注意参数名，尽量与存储过程保持一致
        cmd.Parameters.Add("SN", OracleDbType.Varchar2).Value = sn;
        cmd.Parameters.Add("LINE", OracleDbType.Varchar2).Value = line;
        cmd.Parameters.Add("SITE", OracleDbType.Int32).Value = site;

        cmd.ExecuteNonQuery();
    }

    // ========== deduckMaterial ==========
    private static readonly object _deduckLock = new();

    public void DeduckMaterial(string line)
    {
        // 保持原版语义：串行执行，防止并发扣料导致死锁或负库存
        lock (_deduckLock)
        {
            using var conn = ReelUpDbHelper.GetConnection();
            using var cmd = new OracleCommand("SMTUSER.DECUT_REEL_ALL", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("LINE", OracleDbType.Varchar2).Value = line;
            cmd.ExecuteNonQuery();
        }
    }

    // ========== getIOConfigInfo ==========
    public List<IDInfo> GetIoConfigInfo()
    {
        string sql =
            "select t.LINE, t.IP, t.ID, n.GUN_NAME, g.GROUP_NAME " +
            "from WLS_TERMIAL_GROUP_RELATION r, WLS_TERMIAL t, WLS_GROUP g, WLS_GUN n " +
            "where r.GROUP_SEQ = g.SEQ and r.TERMIAL_SEQ = t.SEQ and t.TYPE = n.GUN_ID " +
            "and g.GROUP_NAME in ('INPUT', 'OUTPUT') " +
            "order by length(t.LINE) desc, t.LINE, t.ID";

        var list = new List<IDInfo>();
        using var conn = ReelUpDbHelper.GetConnection();
        using var cmd = new OracleCommand(sql, conn);
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            list.Add(new IDInfo
            {
                Group = rd["GROUP_NAME"]?.ToString()?.Trim() ?? string.Empty,
                Line = rd["LINE"]?.ToString()?.Trim() ?? string.Empty,
                Ip = rd["IP"]?.ToString()?.Trim() ?? string.Empty,
                Id = rd["ID"]?.ToString()?.Trim() ?? string.Empty,
                GunType = rd["GUN_NAME"]?.ToString()?.Trim() ?? string.Empty,
            });
        }
        return list;
    }
}