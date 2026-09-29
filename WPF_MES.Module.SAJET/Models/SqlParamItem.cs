using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Module.SAJET.Models;

/// <summary>
/// SQL 参数项
/// </summary>
public partial class SqlParamItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;

    [ObservableProperty] private string _value = string.Empty;

    public SqlParamItem() { }

    public SqlParamItem(string name, string value = "")
    {
        Name = name;
        Value = value;
    }
}