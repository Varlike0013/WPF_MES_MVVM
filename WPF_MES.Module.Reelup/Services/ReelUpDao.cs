using System.Data;
using Oracle.ManagedDataAccess.Client;
using WPF_MES.Module.ReelUp.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.ReelUp.Services;

public sealed class ReelUpDao
{
    // ========== getIPInfo ==========
    public List<IDInfo> GetIpInfo(string floor)
    {
        string sql = "select * from V_WLS_ALLID_INFO where group_name in ('ASSY', 'DIP') "
                   + Floors.BuildLineFilter(floor)
                   + "order by length(LINE) DESC, LINE, ID";

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
                GunType = rd["TYPE"]?.ToString()?.Trim() ?? string.Empty,
            });
        }
        return list;
    }

    // ========== getIOIPInfo ==========
    public List<IDInfo> GetIoIpInfo(string floor)
    {
        string sql = "select * from V_WLS_ALLID_INFO where (group_name = 'INPUT' or group_name = 'OUTPUT') "
                   + Floors.BuildLineFilter(floor)
                   + "order by LINE";

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
                GunType = rd["TYPE"]?.ToString()?.Trim() ?? string.Empty,
            });
        }
        return list;
    }

    // ========== getFloorID (核心通讯数据源) ==========
    public MagicBean[] GetFloorId(string floor)
    {
        var magic = new MagicBean[255];
        for (int i = 0; i < magic.Length; i++) magic[i] = new MagicBean();

        string sql = "SELECT ID,IP,LINE,TYPE,GROUP_NAME FROM V_WLS_ALLID_INFO WHERE GROUP_NAME in ('ASSY', 'DIP') "
                   + Floors.BuildLineFilter(floor);
        string sqlCmd = "SELECT ID,CMD_NAME,STATUS FROM V_WLS_ALLID_CMD_INFO where 1=1 "
                      + Floors.BuildLineFilter(floor);

        using var conn = ReelUpDbHelper.GetConnection();

        // 1. 读取终端静态配置
        using (var cmd = new OracleCommand(sql, conn))
        using (var rd = cmd.ExecuteReader())
        {
            while (rd.Read())
            {
                int id = Convert.ToInt32(rd["ID"]);
                if (id < 0 || id >= magic.Length) continue;
                var m = magic[id];
                m.Cst[0] = string.Empty;
                m.Cst[1] = "0";
                m.Cst[2] = "0";
                m.Cst[3] = string.Empty;
                m.Cst[4] = rd["ID"]?.ToString()?.Trim() ?? string.Empty;
                m.Cst[5] = rd["IP"]?.ToString()?.Trim() ?? string.Empty;
                m.Cst[6] = rd["LINE"]?.ToString()?.Trim() ?? string.Empty;
                m.Cst[7] = rd["GROUP_NAME"]?.ToString()?.Trim() ?? string.Empty;
                m.Cst[8] = rd["TYPE"]?.ToString()?.Trim() ?? string.Empty;
            }
        }

        // 2. 读取终端可用命令（用于 checkCMD）
        using (var cmd = new OracleCommand(sqlCmd, conn))
        using (var rd = cmd.ExecuteReader())
        {
            while (rd.Read())
            {
                int id = Convert.ToInt32(rd["ID"]);
                int status = Convert.ToInt32(rd["STATUS"]);
                if (id < 0 || id >= magic.Length) continue;
                if (status < 0 || status >= MagicBean.MaxCmdStatus) continue;
                magic[id].Cmd[status] = rd["CMD_NAME"]?.ToString()?.Trim() ?? string.Empty;
            }
        }

        return magic;
    }

    // ========== getLine ==========
    public List<string> GetLine(string floor)
    {
        string sql = "select distinct LINE from WLS_TERMIAL where 1=1 "
                   + Floors.BuildLineFilter(floor)
                   + "order by length(LINE) DESC, LINE";

        var list = new List<string>();
        using var conn = ReelUpDbHelper.GetConnection();
        using var cmd = new OracleCommand(sql, conn);
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            var v = rd[0]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(v)) list.Add(v);
        }
        return list;
    }

    // ========== getNextTerminalSeq (修复空表异常) ==========
    public int GetNextTerminalSeq()
    {
        const string sql = "select coalesce(max(seq), 0) + 1 from WLS_TERMIAL";
        using var conn = ReelUpDbHelper.GetConnection();
        using var cmd = new OracleCommand(sql, conn);
        var v = cmd.ExecuteScalar();
        return v is null or DBNull ? 1 : Convert.ToInt32(v);
    }

    // ========== addTerminal (修复无事务隐患) ==========
    public void AddTerminal(IDInfo info)
    {
        int seq = GetNextTerminalSeq();
        string group = info.Group ?? string.Empty;

        using var conn = ReelUpDbHelper.GetConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;

            if (group.Equals("ASSY", StringComparison.OrdinalIgnoreCase)
                || group.Equals("DIP", StringComparison.OrdinalIgnoreCase))
            {
                // 1. 线别序列
                cmd.CommandText = "insert into smtuser.tbl_smt_seq(seq, strlineid) " +
                                  "select (select min(seq)-1 from smtuser.tbl_smt_seq), :line from dual " +
                                  "where not exists (select * from smtuser.tbl_smt_seq where strlineid = :line)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(":line", OracleDbType.Varchar2).Value = info.Line;
                cmd.ExecuteNonQuery();

                // 2. 线别作业组
                int numIndex = group.Equals("ASSY", StringComparison.OrdinalIgnoreCase) ? 3 : 2;
                cmd.CommandText = "insert into WLS.WLS_LINE_JOBGROUP " +
                                  "select (select max(num_index)+1 from WLS.WLS_LINE_JOBGROUP), :idx, :line from dual " +
                                  "where not exists (select * from WLS.WLS_LINE_JOBGROUP where line_id = :line)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(":idx", OracleDbType.Int32).Value = numIndex;
                cmd.Parameters.Add(":line", OracleDbType.Varchar2).Value = info.Line;
                cmd.ExecuteNonQuery();

                // 3. 终端
                int type = string.Equals(info.GunType, "M101", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
                cmd.CommandText = "insert into WLS_TERMIAL (SEQ, IP, ID, LINE, TYPE) values (:seq, :ip, :id, :line, :type)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(":seq", OracleDbType.Int32).Value = seq;
                cmd.Parameters.Add(":ip", OracleDbType.Varchar2).Value = info.Ip;
                cmd.Parameters.Add(":id", OracleDbType.Varchar2).Value = info.Id;
                cmd.Parameters.Add(":line", OracleDbType.Varchar2).Value = info.Line;
                cmd.Parameters.Add(":type", OracleDbType.Int32).Value = type;
                cmd.ExecuteNonQuery();

                // 4. 终端组关系
                int groupSeq = group.Equals("ASSY", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
                cmd.CommandText = "insert into WLS_TERMIAL_GROUP_RELATION (SEQ, GROUP_SEQ, TERMIAL_SEQ) " +
                                  "values (:seq, :gseq, :tseq)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(":seq", OracleDbType.Int32).Value = 1;
                cmd.Parameters.Add(":gseq", OracleDbType.Int32).Value = groupSeq;
                cmd.Parameters.Add(":tseq", OracleDbType.Int32).Value = seq;
                cmd.ExecuteNonQuery();
            }
            else if (group.Equals("INPUT", StringComparison.OrdinalIgnoreCase)
                  || group.Equals("OUTPUT", StringComparison.OrdinalIgnoreCase))
            {
                int type = group.Equals("INPUT", StringComparison.OrdinalIgnoreCase) ? 3 : 4;
                int groupSeq = type;

                cmd.CommandText = "insert into WLS_TERMIAL (SEQ, IP, ID, LINE, TYPE) values (:seq, :ip, 0, :line, :type)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(":seq", OracleDbType.Int32).Value = seq;
                cmd.Parameters.Add(":ip", OracleDbType.Varchar2).Value = info.Ip;
                cmd.Parameters.Add(":line", OracleDbType.Varchar2).Value = info.Line;
                cmd.Parameters.Add(":type", OracleDbType.Int32).Value = type;
                cmd.ExecuteNonQuery();

                cmd.CommandText = "insert into WLS_TERMIAL_GROUP_RELATION (SEQ, GROUP_SEQ, TERMIAL_SEQ) " +
                                  "values (:seq, :gseq, :tseq)";
                cmd.Parameters.Clear();
                cmd.Parameters.Add(":seq", OracleDbType.Int32).Value = 1;
                cmd.Parameters.Add(":gseq", OracleDbType.Int32).Value = groupSeq;
                cmd.Parameters.Add(":tseq", OracleDbType.Int32).Value = seq;
                cmd.ExecuteNonQuery();
            }
            else
            {
                throw new ArgumentException($"未知业务组: {group}");
            }

            tx.Commit();
        }
        catch
        {
            try { tx.Rollback(); } catch { /* ignore */ }
            throw;
        }
    }

    // ========== getReelupConfigInfo ==========
    public List<IDInfo> GetReelupConfigInfo()
    {
        string sql =
            "select t.LINE, t.IP, t.ID, n.GUN_NAME, g.GROUP_NAME " +
            "from WLS_TERMIAL_GROUP_RELATION r, WLS_TERMIAL t, WLS_GROUP g, WLS_GUN n " +
            "where r.GROUP_SEQ = g.SEQ and r.TERMIAL_SEQ = t.SEQ and t.TYPE = n.GUN_ID " +
            "and g.GROUP_NAME in ('ASSY', 'DIP') " +
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

    // ========== updateConfigInfo ==========
    public bool UpdateConfigInfo(string type, string line, string ip, string id)
    {
        const string sql =
            "update WLS_TERMIAL set TYPE = :type " +
            "where LINE = :line and IP = :ip and ID = :id and rownum = 1";

        try
        {
            using var conn = ReelUpDbHelper.GetConnection();
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(":type", OracleDbType.Varchar2).Value = type;
            cmd.Parameters.Add(":line", OracleDbType.Varchar2).Value = line;
            cmd.Parameters.Add(":ip", OracleDbType.Varchar2).Value = ip;
            cmd.Parameters.Add(":id", OracleDbType.Varchar2).Value = id;
            return cmd.ExecuteNonQuery() > 0;
        }
        catch
        {
            return false;
        }
    }

    // ========== GetProcName (修复 SQL 注入) ==========
    /// <summary>
    /// 根据线别、业务组和状态码，获取对应的 Oracle 存储过程名称。
    /// </summary>
    public string? GetProcName(string line, string groupName, int status)
    {
        const string sql = @"
            SELECT t.PROC_NAME 
            FROM WLS_LINE_JOBGROUP l, WLS_JOBGROUP_TYPE t
            WHERE l.TYPE_ID = t.TYPE_ID
              AND l.LINE_ID = :line 
              AND t.GROUP_NAME = :groupName 
              AND t.STATUS = :status";

        try
        {
            using var conn = ReelUpDbHelper.GetConnection();
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(":line", OracleDbType.Varchar2).Value = line;
            cmd.Parameters.Add(":groupName", OracleDbType.Varchar2).Value = groupName;
            cmd.Parameters.Add(":status", OracleDbType.Int32).Value = status;

            var result = cmd.ExecuteScalar();
            return result?.ToString()?.Trim();
        }
        catch (Exception ex)
        {
            Logger.Error($"[ReelUpDao] GetProcName 失败: {ex.Message}");
            return null;
        }
    }
}