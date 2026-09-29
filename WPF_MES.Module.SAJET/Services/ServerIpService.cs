using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;
namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 服务器/网关/IP 相关操作
/// </summary>
internal static class ServerIpService
{
    // ============ 加载树（第一二层） ============

    /// <summary>
    /// 加载所有启用的服务器和网关
    /// </summary>
    public static List<TreeNodeViewModel> LoadServerTree()
    {
        const string sql = @"
            SELECT S.SERVER_ID, S.SERVER_DESC_E,
                   G.GATEWAY_ID, G.DRIVER_ID,
                   G.GAYTEWAY_CONNECT_NUMBER, G.GATEWAY_DESC_E
            FROM SAJET.TGS_SERVER_BASE S
            INNER JOIN SAJET.TGS_GATEWAY_BASE G ON G.SERVER_ID = S.SERVER_ID
            WHERE S.ENABLED = 'Y' AND G.ENABLED = 'Y'
            ORDER BY S.SERVER_DESC_E, G.GATEWAY_DESC_E";

        var dt = OracleHelper.QueryDataTable(sql);

        var serverMap = new Dictionary<string, TreeNodeViewModel>();
        var roots = new List<TreeNodeViewModel>();

        foreach (DataRow r in dt.Rows)
        {
            string serverId = OracleHelper.GetStr(r, "SERVER_ID");
            string serverDesc = OracleHelper.GetStr(r, "SERVER_DESC_E");
            string gatewayId = OracleHelper.GetStr(r, "GATEWAY_ID");
            string driverId = OracleHelper.GetStr(r, "DRIVER_ID");
            int connectNumber = OracleHelper.GetInt(r, "GAYTEWAY_CONNECT_NUMBER");
            string gatewayDesc = OracleHelper.GetStr(r, "GATEWAY_DESC_E");

            // 服务器节点
            if (!serverMap.TryGetValue(serverId, out var serverNode))
            {
                serverNode = new TreeNodeViewModel
                {
                    Type = TreeNodeType.Server,
                    Title = $"{serverDesc} ({serverId})",
                    ServerId = serverId,
                    ServerDesc = serverDesc,
                };
                serverMap[serverId] = serverNode;
                roots.Add(serverNode);
            }

            // 网关节点
            var gwNode = new TreeNodeViewModel
            {
                Type = TreeNodeType.Gateway,
                Title = $"{gatewayDesc} ({gatewayId}) - {connectNumber}",
                ServerId = serverId,
                ServerDesc = serverDesc,
                GatewayId = gatewayId,
                GatewayDesc = gatewayDesc,
                DriverId = driverId,
                ConnectNumber = connectNumber,
                ChildrenLoaded = false,
            };
            serverNode.Children.Add(gwNode);
        }

        return roots;
    }

    // ============ 调用存储过程获取网关 IP ============

    /// <summary>
    /// 调 GET_GATEWAY_IP 存储过程，返回 IP 列表。
    /// </summary>
    public static List<string> GetGatewayIps(
        string serverId, string gatewayId, string driverId)
    {
        using var conn = OracleHelper.GetConnection();
        using var cmd = new OracleCommand("SAJET.GET_GATEWAY_IP", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };

        cmd.Parameters.Add("server", OracleDbType.Varchar2).Value = serverId;
        cmd.Parameters.Add("gateway", OracleDbType.Varchar2).Value = gatewayId;
        cmd.Parameters.Add("driver", OracleDbType.Varchar2).Value = driverId;

        var outTres = new OracleParameter("tres", OracleDbType.Varchar2, 200)
        {
            Direction = ParameterDirection.Output,
        };
        cmd.Parameters.Add(outTres);

        var outIp = new OracleParameter("ip", OracleDbType.Varchar2, 4000)
        {
            Direction = ParameterDirection.Output,
        };
        cmd.Parameters.Add(outIp);

        cmd.ExecuteNonQuery();

        string tres = outTres.Value?.ToString()?.Trim() ?? string.Empty;
        string ipStr = outIp.Value?.ToString() ?? string.Empty;

        if (tres != "OK")
            throw new Exception($"获取 IP 失败：{tres}");

        // 正则提取所有 IPv4 地址
        var list = new List<string>();
        var re = new Regex(@"\b(?:\d{1,3}\.){2,3}\d{1,3}\b");
        foreach (Match m in re.Matches(ipStr))
            list.Add(m.Value);

        return list;
    }

    // ============ 查询终端链接 ============

    public static List<TerminalLinkItem> QueryTerminalLinks(
        string serverId, string gatewayId, int deviceId)
    {
        const string sql = @"
            SELECT S.STAGE_NAME, PL.PDLINE_NAME, G.GATEWAY_DESC_E,
                   T.TERMINAL_ID, T.TERMINAL_NAME, P.PROCESS_NAME,
                   TL.GROUP_ID, T.ENABLED
            FROM SAJET.TGS_TERMINAL_LINK TL
            INNER JOIN SAJET.SYS_TERMINAL T ON TL.TERMINAL_ID = T.TERMINAL_ID
            INNER JOIN SAJET.SYS_PDLINE PL ON PL.PDLINE_ID = T.PDLINE_ID
            INNER JOIN SAJET.SYS_PROCESS P ON P.PROCESS_ID = T.PROCESS_ID
            INNER JOIN SAJET.SYS_STAGE S ON S.STAGE_ID = T.STAGE_ID
            INNER JOIN SAJET.TGS_GATEWAY_BASE G 
                ON G.GATEWAY_ID = TL.GATEWAY_ID AND G.SERVER_ID = TL.SERVER_ID
            WHERE TL.SERVER_ID = :server
              AND TL.GATEWAY_ID = :gateway
              AND TL.DEVICE_ID = :idx";

        var ps = new Dictionary<string, object>
        {
            { "server",  serverId },
            { "gateway", gatewayId },
            { "idx",     deviceId },
        };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<TerminalLinkItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new TerminalLinkItem
            {
                StageName = OracleHelper.GetStr(r, "STAGE_NAME"),
                PdlineName = OracleHelper.GetStr(r, "PDLINE_NAME"),
                GatewayDesc = OracleHelper.GetStr(r, "GATEWAY_DESC_E"),
                TerminalId = OracleHelper.GetInt(r, "TERMINAL_ID"),
                TerminalName = OracleHelper.GetStr(r, "TERMINAL_NAME"),
                ProcessName = OracleHelper.GetStr(r, "PROCESS_NAME"),
                GroupId = OracleHelper.GetInt(r, "GROUP_ID"),
                Enabled = OracleHelper.GetStr(r, "ENABLED"),
            });
        }
        return list;
    }

    /// <summary>
    /// 取某 IP 对应的终端名称（用于树节点显示）。
    /// 多条时用顿号拼接。
    /// </summary>
    public static string GetTerminalNameForIp(
        string serverId, string gatewayId, int deviceId)
    {
        const string sql = @"
            SELECT T.TERMINAL_NAME
            FROM SAJET.TGS_TERMINAL_LINK TL
            INNER JOIN SAJET.SYS_TERMINAL T ON T.TERMINAL_ID = TL.TERMINAL_ID
            WHERE TL.SERVER_ID = :server
              AND TL.GATEWAY_ID = :gateway
              AND TL.DEVICE_ID = :idx";

        var ps = new Dictionary<string, object>
        {
            { "server",  serverId },
            { "gateway", gatewayId },
            { "idx",     deviceId },
        };

        var dt = OracleHelper.QueryDataTable(sql, ps);
        if (dt.Rows.Count == 0) return string.Empty;

        var names = new List<string>();
        foreach (DataRow r in dt.Rows)
        {
            string name = OracleHelper.GetStr(r, "TERMINAL_NAME");
            if (name.Length > 0) names.Add(name);
        }
        return string.Join("、", names);
    }

    // ============ 导出所有网关 IP + 终端信息 ============

    /// <summary>
    /// 后台导出所有启用网关的 IP 及终端信息到 CSV。
    /// 按 IP 排序：先段数少的（3 段），再按每段数字升序。
    /// </summary>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="progress">进度回调（当前, 总数）</param>
    public static void ExportGatewayData(string filePath, Action<int, int>? progress = null)
    {
        // 1. 加载所有服务器/网关
        var gateways = new List<(string ServerId, string ServerDesc,
                                 string GatewayId, string GatewayDesc, string DriverId)>();
        {
            const string sql = @"
            SELECT S.SERVER_ID, S.SERVER_DESC_E,
                   G.GATEWAY_ID, G.GATEWAY_DESC_E, G.DRIVER_ID
            FROM SAJET.TGS_SERVER_BASE S
            INNER JOIN SAJET.TGS_GATEWAY_BASE G ON G.SERVER_ID = S.SERVER_ID
            WHERE S.ENABLED = 'Y' AND G.ENABLED = 'Y'
            ORDER BY S.SERVER_DESC_E, G.GATEWAY_DESC_E";

            var dt = OracleHelper.QueryDataTable(sql);
            foreach (DataRow r in dt.Rows)
            {
                gateways.Add((
                    OracleHelper.GetStr(r, "SERVER_ID"),
                    OracleHelper.GetStr(r, "SERVER_DESC_E"),
                    OracleHelper.GetStr(r, "GATEWAY_ID"),
                    OracleHelper.GetStr(r, "GATEWAY_DESC_E"),
                    OracleHelper.GetStr(r, "DRIVER_ID")
                ));
            }
        }

        // 2. 遍历网关 → 拿 IP → 每个 IP 查终端 → 展开成多行
        var records = new List<ExportRow>();

        int current = 0;
        int total = gateways.Count;

        foreach (var gw in gateways)
        {
            current++;
            progress?.Invoke(current, total);

            List<string> ips;
            try
            {
                ips = GetGatewayIps(gw.ServerId, gw.GatewayId, gw.DriverId);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SERVER-IP] 网关 {gw.GatewayId} 取 IP 失败：{ex.Message}");
                continue;
            }

            if (ips.Count == 0)
            {
                records.Add(new ExportRow
                {
                    ServerId = gw.ServerId,
                    ServerDesc = gw.ServerDesc,
                    GatewayId = gw.GatewayId,
                    GatewayDesc = gw.GatewayDesc,
                    Ip = string.Empty,
                    DeviceIndex = 0,
                });
                continue;
            }

            for (int i = 0; i < ips.Count; i++)
            {
                int deviceIdx = i + 1;
                string ip = ips[i];

                List<TerminalLinkItem> terms;
                try
                {
                    terms = QueryTerminalLinks(gw.ServerId, gw.GatewayId, deviceIdx);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[SERVER-IP] 设备 {deviceIdx} 查询终端失败：{ex.Message}");
                    terms = new List<TerminalLinkItem>();
                }

                if (terms.Count == 0)
                {
                    records.Add(new ExportRow
                    {
                        ServerId = gw.ServerId,
                        ServerDesc = gw.ServerDesc,
                        GatewayId = gw.GatewayId,
                        GatewayDesc = gw.GatewayDesc,
                        Ip = ip,
                        DeviceIndex = deviceIdx,
                    });
                }
                else
                {
                    foreach (var t in terms)
                    {
                        records.Add(new ExportRow
                        {
                            ServerId = gw.ServerId,
                            ServerDesc = gw.ServerDesc,
                            GatewayId = gw.GatewayId,
                            GatewayDesc = gw.GatewayDesc,
                            Ip = ip,
                            DeviceIndex = deviceIdx,

                            StageName = t.StageName,
                            PdlineName = t.PdlineName,
                            TerminalId = t.TerminalId,
                            TerminalName = t.TerminalName,
                            ProcessName = t.ProcessName,
                            GroupId = t.GroupId,
                            Enabled = t.Enabled,
                        });
                    }
                }
            }
        }

        // 3. 排序
        records.Sort((a, b) =>
        {
            bool aEmpty = string.IsNullOrEmpty(a.Ip);
            bool bEmpty = string.IsNullOrEmpty(b.Ip);
            if (aEmpty && bEmpty) return 0;
            if (aEmpty) return 1;
            if (bEmpty) return -1;

            int cmp = CompareIp(a.Ip, b.Ip);
            if (cmp != 0) return cmp;

            return a.DeviceIndex.CompareTo(b.DeviceIndex);
        });

        // 4. 写 CSV
        using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
        writer.WriteLine("服务器ID,服务器描述,网关ID,网关描述,IP组,IP,站别,产线,终端ID,终端名称,工序,组ID,启用状态");

        foreach (var r in records)
        {
            writer.Write(Escape(r.ServerId)); writer.Write(',');
            writer.Write(Escape(r.ServerDesc)); writer.Write(',');
            writer.Write(Escape(r.GatewayId)); writer.Write(',');
            writer.Write(Escape(r.GatewayDesc)); writer.Write(',');

            writer.Write(Escape(r.IpGroup)); writer.Write(',');
            writer.Write(Escape(r.Ip)); writer.Write(',');

            writer.Write(Escape(r.StageName)); writer.Write(',');
            writer.Write(Escape(r.PdlineName)); writer.Write(',');

            writer.Write(r.TerminalId > 0 ? r.TerminalId.ToString() : ""); writer.Write(',');

            writer.Write(Escape(r.TerminalName)); writer.Write(',');
            writer.Write(Escape(r.ProcessName)); writer.Write(',');
            writer.Write(r.GroupId > 0 ? r.GroupId.ToString() : ""); writer.Write(',');
            writer.WriteLine(Escape(r.Enabled));
        }
    }

    // ============ 导出用的行结构 ============

    private class ExportRow
    {
        public string ServerId { get; set; } = string.Empty;
        public string ServerDesc { get; set; } = string.Empty;
        public string GatewayId { get; set; } = string.Empty;
        public string GatewayDesc { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public int DeviceIndex { get; set; }

        /// <summary>IP 组（前三段）</summary>
        public string IpGroup => GetIpGroup(Ip);

        public string StageName { get; set; } = string.Empty;
        public string PdlineName { get; set; } = string.Empty;
        public int TerminalId { get; set; }
        public string TerminalName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string Enabled { get; set; } = string.Empty;
    }

    // ============ IP 排序 ============

    /// <summary>
    /// 按段数 + 每段数字比较。
    /// 3 段的 IP 排在 4 段前面。
    /// </summary>
    private static int CompareIp(string ip1, string ip2)
    {
        var p1 = ip1.Split('.');
        var p2 = ip2.Split('.');

        // 先按段数
        if (p1.Length != p2.Length)
            return p1.Length.CompareTo(p2.Length);

        // 再按每段数字
        for (int i = 0; i < p1.Length; i++)
        {
            bool ok1 = int.TryParse(p1[i], out var n1);
            bool ok2 = int.TryParse(p2[i], out var n2);

            if (ok1 && ok2)
            {
                if (n1 != n2) return n1.CompareTo(n2);
            }
            else
            {
                int cmp = string.Compare(p1[i], p2[i], StringComparison.Ordinal);
                if (cmp != 0) return cmp;
            }
        }
        return 0;
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        bool needQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n');
        if (!needQuote) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
    /// <summary>
    /// 取 IP 前三段作为 IP 组。
    /// "192.168.1.1" → "192.168.1"
    /// "192.168.1"   → "192.168.1"
    /// ""            → ""
    /// </summary>
    private static string GetIpGroup(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return string.Empty;

        var parts = ip.Split('.');
        if (parts.Length < 3) return ip;   // 段数不够，原样返回

        return $"{parts[0]}.{parts[1]}.{parts[2]}";
    }
}