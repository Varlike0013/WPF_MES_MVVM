using System.Globalization;
using System.IO;
using WPF_MES.Module.AOI.Models;

namespace WPF_MES.Module.AOI.Services;

internal static class AoiFileProcessor
{
    public static bool ProcessTxtBga(string filePath, out AoiRecord record, out string error)
        => Process(filePath, minLines: 13, hasBgaSerial: true, out record, out error);

    public static bool ProcessTxtNbga(string filePath, out AoiRecord record, out string error)
        => Process(filePath, minLines: 12, hasBgaSerial: false, out record, out error);

    private static bool Process(string filePath, int minLines, bool hasBgaSerial,
        out AoiRecord record, out string error)
    {
        record = new AoiRecord();
        error = string.Empty;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].Trim('\uFEFF', ' ', '\t', '\r').Trim();
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        if (lines.Length < minLines)
        {
            error = "行数不足，无法解析";
            return false;
        }

        int idx = 0;
        record.PartNo = lines[idx++];
        record.SerialNumber = lines[idx++];
        record.LineName = lines[idx++];
        record.Field1 = ParseInt(lines[idx++]);
        record.Field2 = lines[idx++];
        record.Field3 = ParseInt(lines[idx++]);
        record.InputTime = ParseDateTime(lines[idx++]);
        record.OutputTime = ParseDateTime(lines[idx++]);
        record.PassStatus = lines[idx++];
        record.Flag = lines[idx++];
        record.Number = ParseInt(lines[idx++]);

        if (hasBgaSerial)
            record.BgaSerial = lines[idx++];

        record.AbnormalCount = ParseInt(lines[idx++]);

        for (; idx < lines.Length; idx++)
        {
            var parts = lines[idx].Split(';');
            if (parts.Length >= 4)
            {
                record.Points.Add(new PointRecord
                {
                    Position = parts[0].Trim(),
                    PartNo = parts[1].Trim(),
                    Status = parts[2].Trim(),
                    Value = ParseInt(parts[3].Trim()),
                });
            }
        }
        return true;
    }

    private static int ParseInt(string s)
        => int.TryParse(s, out var v) ? v : 0;

    private static DateTime ParseDateTime(string s)
        => DateTime.TryParseExact(s, "yyyy/MM/dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt : DateTime.MinValue;
}