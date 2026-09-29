using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// 存储过程参数项
/// </summary>
public partial class ProcParamItem : ObservableObject
{
    /// <summary>参数名（大写）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>参数值（用户输入或输出结果）</summary>
    [ObservableProperty] private string _value = string.Empty;
}