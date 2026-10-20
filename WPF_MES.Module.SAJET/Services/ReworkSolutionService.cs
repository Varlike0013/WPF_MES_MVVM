using System.Text.RegularExpressions;
using WPF_MES.Module.SAJET.Models;

namespace WPF_MES.Module.SAJET.Services;

public static class ReworkSolutionService
{
    /// <summary>
    /// 根据序号与原因，生成解决建议文字。
    /// </summary>
    public static string Analyze(ReworkProblemItem item)
    {
        if (string.IsNullOrWhiteSpace(item.SerialNumber) && string.IsNullOrWhiteSpace(item.ReasonQuery))
            return "无有效信息，无法生成建议。";

        string reason = item.ReasonQuery.ToUpperInvariant();
        string sn = item.SerialNumber;

        // 1. 从 SERIAL_NUMBER 识别参数（提取纯数字、字母混合的流水码，或特定格式）
        string paramFromSn = ExtractParameterFromSn(sn);

        // 2. 从 REASON || QUERY 识别如何处理及是否包含参数
        var (actionDesc, paramFromReason) = ParseReason(reason);

        // 3. 组装最终建议
        var lines = new List<string>();

        lines.Add($"【识别参数】");
        lines.Add($"- 原始序号：{sn}");
        if (!string.IsNullOrEmpty(paramFromSn))
            lines.Add($"- 序号参数：{paramFromSn} (需核对此参数是否匹配工单)");
        if (!string.IsNullOrEmpty(paramFromReason))
            lines.Add($"- 原因参数：{paramFromReason}");

        lines.Add("");
        lines.Add($"【处理建议】");
        lines.Add($"- 触发动作：{actionDesc}");
        lines.Add("- 操作步骤：");

        if (reason.Contains("CLEAR") || reason.Contains("清除"))
        {
            lines.Add("  1. 进入对应的工站（与线别相关）");
            lines.Add("  2. 扫描或输入序号，执行【清除】操作");
            lines.Add("  3. 确认系统回传 OK");
        }
        else if (reason.Contains("RETURN") || reason.Contains("退料"))
        {
            lines.Add("  1. 确认物料是否已退库");
            lines.Add("  2. 在系统中执行退料操作，关联参数：" + (paramFromReason ?? paramFromSn));
            lines.Add("  3. 核对库存数量");
        }
        else if (reason.Contains("REWORK") || reason.Contains("重工"))
        {
            lines.Add("  1. 将产品放入重工线");
            lines.Add("  2. 执行重工，并在系统中录入重工单号");
            lines.Add("  3. 检查重工后的状态是否为 RPASS");
        }
        else
        {
            lines.Add("  1. 检查该序号的当前工站状态");
            lines.Add("  2. 联系工程师确认是否需要人工介入");
            lines.Add("  3. 处理完成后在 ECS 系统中标记为已处理（DUALTIME）");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// 从 SERIAL_NUMBER 提取参数：
    /// 假设规则为：以字母开头+数字，或者取后8位作为核心流水码。
    /// </summary>
    private static string ExtractParameterFromSn(string sn)
    {
        if (string.IsNullOrEmpty(sn)) return string.Empty;

        // 例如：提取以字母开头的数字段，或提取最后 8 个字符
        var match = Regex.Match(sn, @"[A-Z0-9]{6,}");
        if (match.Success) return match.Value;

        return sn.Length > 8 ? sn[^8..] : sn;
    }

    /// <summary>
    /// 从 REASON || QUERY 中解析动作及可能附带的参数。
    /// 示例： "REWORK_DEFECT: 03" -> 动作=REWORK_DEFECT, 参数=03
    /// </summary>
    private static (string actionDesc, string? param) ParseReason(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return ("未知", null);

        // 匹配冒号、等号、空格后的值作为参数
        var match = Regex.Match(reason, @"(?:[:=]|\s{1,})([A-Z0-9_\-]{2,})$");
        string? param = match.Success ? match.Groups[1].Value : null;

        string action = reason;
        if (match.Success)
        {
            action = reason.Replace(match.Value, "").Trim();
            action = action.TrimEnd(':', '=', ' ');
        }

        string desc = action switch
        {
            var a when a.Contains("CLEAR") => "清除特定数据（如料件/箱号/重工号）",
            var a when a.Contains("RETURN") => "执行退料",
            var a when a.Contains("REWORK") => "执行重工",
            _ => action
        };

        return (desc, param);
    }
}