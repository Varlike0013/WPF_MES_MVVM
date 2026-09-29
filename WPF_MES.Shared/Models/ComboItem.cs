namespace WPF_MES.Contracts.Models;

public class ComboItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public override string ToString() => Name;
}