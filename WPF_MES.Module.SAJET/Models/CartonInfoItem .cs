using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 箱号信息行
/// </summary>
public partial class CartonInfoItem : ObservableObject
{
    // ============ 基础字段 ============

    /// <summary>工单 (G_WO_CARTON.WORK_ORDER)</summary>
    public string WorkOrder { get; set; } = string.Empty;

    /// <summary>更新人员 (SYS_EMP.EMP_NAME)</summary>
    public string UpdateEmp { get; set; } = string.Empty;

    /// <summary>更新时间</summary>
    public string UpdateTime { get; set; } = string.Empty;

    /// <summary>数量</summary>
    public int Qty { get; set; }

    /// <summary>包装工单 (G_PACK_CARTON.WORK_ORDER)</summary>
    public string PackWorkOrder { get; set; } = string.Empty;

    /// <summary>料号</summary>
    public string PartNo { get; set; } = string.Empty;

    /// <summary>箱号</summary>
    public string CartonNo { get; set; } = string.Empty;

    /// <summary>关闭标志原始值：N/Y/K</summary>
    public string CloseFlag { get; set; } = string.Empty;

    /// <summary>状态描述：打开/关闭/缴库</summary>
    public string StatusDesc { get; set; } = string.Empty;

    /// <summary>终端</summary>
    public string TerminalName { get; set; } = string.Empty;

    /// <summary>创建人员</summary>
    public string CreateEmp { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public string CreateTime { get; set; } = string.Empty;
}