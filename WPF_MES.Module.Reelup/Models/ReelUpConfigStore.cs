using System.IO;
using System.Text.Json;
using WPF_MES.Module.ReelUp.Models;

namespace WPF_MES.Module.ReelUp.Services;

public static class ReelUpConfigStore
{
    private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reelup.json");
    private static ReelUpConfig? _current;

    public static ReelUpConfig Current
    {
        get
        {
            if (_current == null)
            {
                if (File.Exists(_configPath))
                {
                    try
                    {
                        var json = File.ReadAllText(_configPath);
                        _current = JsonSerializer.Deserialize<ReelUpConfig>(json) ?? new ReelUpConfig();
                    }
                    catch { _current = new ReelUpConfig(); }
                }
                else _current = new ReelUpConfig();
            }
            return _current;
        }
    }

    public static void Save()
    {
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}