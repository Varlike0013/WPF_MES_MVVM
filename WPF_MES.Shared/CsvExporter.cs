using System.IO;
using System.Text;

namespace WPF_MES.Shared;

/// <summary>
/// CSV 导出工具
/// </summary>
public static class CsvExporter
{
    /// <summary>
    /// 把数据导出为 CSV 文件。
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="filePath">保存路径</param>
    /// <param name="headers">列标题</param>
    /// <param name="rows">每行的字段值（和 headers 一一对应）</param>
    public static void Export<T>(string filePath,
        string[] headers,
        IEnumerable<T> rows,
        Func<T, string[]> rowSelector)
    {
        // UTF-8 with BOM：Excel 打开中文不乱码
        using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));

        // 表头
        writer.WriteLine(string.Join(",", headers.Select(Escape)));

        // 数据行
        foreach (var item in rows)
        {
            var fields = rowSelector(item);
            writer.WriteLine(string.Join(",", fields.Select(Escape)));
        }
    }

    /// <summary>
    /// CSV 字段转义：包含逗号、引号、换行时用双引号包裹，内部引号转义为两个
    /// </summary>
    private static string Escape(string? field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;

        bool needQuote = field.Contains(',')
                      || field.Contains('"')
                      || field.Contains('\n')
                      || field.Contains('\r');

        if (!needQuote) return field;

        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }
}