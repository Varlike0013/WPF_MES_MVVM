namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// ERP 物料信息
/// </summary>
public class ErpMaterialItem
{
    /// <summary>工单或料号 (WORK_ORDER)</summary>
    public string WorkOrder { get; set; } = string.Empty;

    /// <summary>ECS料号 (ECS_PN)</summary>
    public string EcsPn { get; set; } = string.Empty;

    /// <summary>类型 (ECS_PN_DESC)</summary>
    public string EcsPnDesc { get; set; } = string.Empty;

    /// <summary>料件号 (KPARTS_NO)</summary>
    public string KpartsNo { get; set; } = string.Empty;

    /// <summary>创建时间 (CREATE_TIME)</summary>
    public string CreateTime { get; set; } = string.Empty;
}