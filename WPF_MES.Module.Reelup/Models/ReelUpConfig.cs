// Models/ReelUpConfig.cs
namespace WPF_MES.Module.ReelUp.Models;

public class ReelUpConfig
{
    // ===== 数据库 =====
    // 默认沿用 jdbc.properties
    public string ConnectionString { get; set; } = "User Id=wls;Password=123456;Data Source=192.168.0.16:1521/SMT;Pooling=true;Min Pool Size=1;Max Pool Size=20;";
    public string SmtSchema { get; set; } = "SMTUSER";
    public string DefaultBuilding { get; set; } = "B302";

    // ===== 楼层与树 =====
    public int TreeNodeQty { get; set; } = 50;

    // ===== 通讯 =====
    public int WlsPort { get; set; } = 4660;
    public int MagicBeanQty { get; set; } = 255;
    public int CstQty { get; set; } = 50;
    public int StatusQty { get; set; } = 100;

    // ===== 行为 =====
    public int GunTimeoutMs { get; set; } = 300000; // 原 Java 是 5 分钟 (300000ms)
    public int ReconnectMinDelayMs { get; set; } = 1000;
    public int ReconnectMaxDelayMs { get; set; } = 30000;
    public bool MinimizeToTrayOnClose { get; set; } = true;

    // ===== 权限 =====
    public List<string> DeveloperUserNos { get; set; } = new() { "Varlike" };

    // ===== 品牌 =====
    public string AppTitle { get; set; } = "ReelUp 上料与投入产出";
    public string PoweredBy { get; set; } = "Powered by Varlike";
}