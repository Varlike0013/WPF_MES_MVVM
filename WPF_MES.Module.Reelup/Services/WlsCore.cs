using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Net.Sockets;
using WPF_MES.Module.ReelUp.Models;
using WPF_MES.Module.ReelUp.Services.Devices;
using WPF_MES.Shared;

namespace WPF_MES.Module.ReelUp.Services;

public sealed class WlsCore
{
    private readonly ReelUpDao _reelUpDao;

    public WlsCore(ReelUpDao reelUpDao)
    {
        _reelUpDao = reelUpDao;
    }

    /// <summary>
    /// 执行核心状态机逻辑
    /// </summary>
    /// <returns>返回存储过程生成的反馈信息（cst[3]）</returns>
    public string CoreAction(int id, MagicBean magic, NetworkStream stream, IDeviceProtocol dataTrans, string data)
    {
        try
        {
            // 1. 解析当前状态码 (cst[1])
            int status = int.TryParse(magic.Cst[1], out var s) ? s : 0;

            // 2. 查路由表获取存储过程名 (修复：参数化查询)
            string? procName = _reelUpDao.GetProcName(magic.Cst[6], magic.Cst[7], status);

            if (string.IsNullOrEmpty(procName))
            {
                return $"NG:NO PROC FOR {magic.Cst[7]}-{status}";
            }

            // 3. 调用存储过程
            using var conn = ReelUpDbHelper.GetConnection();
            using var cmd = new OracleCommand(procName, conn) { CommandType = CommandType.StoredProcedure };

            // 入参绑定 (注意：C# 端 string[] 长度为 100/50，会自动映射到 TABLE OF)
            cmd.Parameters.Add(new OracleParameter("HISIN", OracleDbType.Array)
            {
                UdtTypeName = "WLS.STRINGARRAY",
                Value = magic.His
            });
            cmd.Parameters.Add(new OracleParameter("CSTIN", OracleDbType.Array)
            {
                UdtTypeName = "WLS.STRINGARRAY",
                Value = magic.Cst
            });

            // 出参绑定
            cmd.Parameters.Add(new OracleParameter("HISOUT", OracleDbType.Array)
            {
                UdtTypeName = "WLS.STRINGARRAY",
                Direction = ParameterDirection.Output,
                Size = magic.His.Length
            });
            cmd.Parameters.Add(new OracleParameter("CSTOUT", OracleDbType.Array)
            {
                UdtTypeName = "WLS.STRINGARRAY",
                Direction = ParameterDirection.Output,
                Size = magic.Cst.Length
            });

            cmd.ExecuteNonQuery();

            // 4. 收回出参并更新 MagicBean
            if (cmd.Parameters["HISOUT"].Value is string[] outHis) magic.His = outHis;
            if (cmd.Parameters["CSTOUT"].Value is string[] outCst) magic.Cst = outCst;

            // 5. 发送显示数据给设备
            string displayData = magic.Cst[3] ?? string.Empty;
            bool isComplexMode = magic.Cst[2] != "0";

            byte[] sendBytes = dataTrans.Encode(displayData, id, isComplexMode);

            // 发送至 TCP 流
            stream.Write(sendBytes, 0, sendBytes.Length);
            stream.Flush();

            return displayData;
        }
        catch (Exception ex)
        {
            Logger.Error($"[WlsCore] CoreAction Exception: {ex.Message}");
            return $"ERROR: {ex.Message}";
        }
    }
}