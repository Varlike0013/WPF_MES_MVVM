using System.Data;
using Oracle.ManagedDataAccess.Client;
using WPF_MES.Module.AOI.Models;
using WPF_MES.Shared;

namespace WPF_MES.Module.AOI.Services;

internal sealed class AoiUploadService
{
    private readonly string _apiCheck;
    private readonly string _apiTravel;
    private readonly int _terminalId;

    public AoiUploadService(string apiCheck, string apiTravel, int terminalId)
    {
        _apiCheck = apiCheck;
        _apiTravel = apiTravel;
        _terminalId = terminalId;
    }

    /// <summary>检查 + 上传。成功返回 true。</summary>
    public bool Upload(AoiRecord record, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrEmpty(_apiCheck)) { error = "api_check is empty"; return false; }
        if (string.IsNullOrEmpty(_apiTravel)) { error = "api_travel is empty"; return false; }

        bool checkOk = _apiCheck == "SJ_CHK_SVI_VAR"
            ? CallSJ_CHK_SVI_VAR(record, out error)
            : Fail($"Unknown api_check: {_apiCheck}", out error);

        if (!checkOk) return false;

        return _apiTravel == "SJ_SVI_TRANSFER_VAR"
            ? CallSJ_SVI_TRANSFER_VAR(record, out error)
            : Fail($"Unknown api_travel: {_apiTravel}", out error);
    }

    private static bool Fail(string msg, out string error)
    {
        error = msg;
        return false;
    }

    private bool CallSJ_CHK_SVI_VAR(AoiRecord record, out string error)
    {
        error = string.Empty;
        try
        {
            using var conn = AoiDbHelper.GetConnection();
            using var cmd = new OracleCommand(
                "BEGIN SAJET.SJ_CHK_SVI_VAR(:tid, :trev, :temp, :tres); END;", conn)
            {
                BindByName = true
            };

            cmd.Parameters.Add(":tid", OracleDbType.Varchar2).Value = _terminalId.ToString();
            cmd.Parameters.Add(":trev", OracleDbType.Varchar2).Value = record.SerialNumber;
            cmd.Parameters.Add(":temp", OracleDbType.Varchar2).Value = "Auto UpLoad";
            cmd.Parameters.Add(new OracleParameter(":tres", OracleDbType.Varchar2, 4000)
            {
                Direction = ParameterDirection.Output
            });

            cmd.ExecuteNonQuery();
            string result = cmd.Parameters[":tres"].Value?.ToString()?.Trim() ?? string.Empty;
            Logger.Info($"[AOI] SJ_CHK_SVI_VAR: {result}");

            if (result == "OK" || string.IsNullOrEmpty(result)) return true;
            error = result;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private bool CallSJ_SVI_TRANSFER_VAR(AoiRecord record, out string error)
    {
        error = string.Empty;
        try
        {
            string defectInfo = "N/A";
            if (record.PassStatus != "RPASS")
            {
                foreach (var pt in record.Points)
                {
                    if (pt.Status != "OK") { defectInfo = pt.Status; break; }
                }
            }

            using var conn = AoiDbHelper.GetConnection();
            using var cmd = new OracleCommand(
                "BEGIN SAJET.SJ_SVI_TRANSFER_VAR(:tid, :trev, :tdefect, :temp, :tres); END;", conn)
            {
                BindByName = true
            };

            cmd.Parameters.Add(":tid", OracleDbType.Varchar2).Value = _terminalId.ToString();
            cmd.Parameters.Add(":trev", OracleDbType.Varchar2).Value = record.SerialNumber;
            cmd.Parameters.Add(":tdefect", OracleDbType.Varchar2).Value = defectInfo;
            cmd.Parameters.Add(":temp", OracleDbType.Varchar2).Value = "Auto UpLoad";
            cmd.Parameters.Add(new OracleParameter(":tres", OracleDbType.Varchar2, 4000)
            {
                Direction = ParameterDirection.Output
            });

            cmd.ExecuteNonQuery();
            string result = cmd.Parameters[":tres"].Value?.ToString()?.Trim() ?? string.Empty;
            Logger.Info($"[AOI] SJ_SVI_TRANSFER_VAR: {result}");

            if (result == "OK" || string.IsNullOrEmpty(result)) return true;
            error = result;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}