using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_MES.Shared.Models;

/// <summary>
/// dataMap 的一行
/// </summary>
public partial class DataMapItem : ObservableObject
{
    public string Key { get; set; } = string.Empty;

    [ObservableProperty] private string _value = string.Empty;

    public DataMapItem() { }

    public DataMapItem(string key, string value)
    {
        Key = key;
        Value = value;
    }
}