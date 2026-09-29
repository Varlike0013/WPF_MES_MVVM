namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 行为组下的一个 Job 信息
/// </summary>
public class GroupJobInfo
{
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string JobDesc { get; set; } = string.Empty;
    public string GroupSeq { get; set; } = string.Empty;
    public string SeqElse { get; set; } = string.Empty;
    public string SeqOther { get; set; } = string.Empty;
    public string ValueKind { get; set; } = string.Empty;
    public string TypeId { get; set; } = string.Empty;
    public string TypeNameE { get; set; } = string.Empty;
    public string ProcCallName { get; set; } = string.Empty;
}

/// <summary>
/// Job 下的一个存储过程
/// </summary>
public class JobDetailInfo
{
    public string JobSeq { get; set; } = string.Empty;
    public string SprocName { get; set; } = string.Empty;
}