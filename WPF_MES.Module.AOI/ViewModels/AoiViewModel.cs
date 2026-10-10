using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Shapes;
using WPF_MES.Module.AOI.Models;
using WPF_MES.Module.AOI.Services;
using WPF_MES.Shared;
using IOPath = System.IO.Path;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.AOI.ViewModels;

public partial class AoiViewModel : ObservableObject
{
    // ============ 配置持久化 ============

    private readonly JsonConfig<AoiConfig> _config = new("aoi.json");

    // ============ 界面属性 ============

    [ObservableProperty] private string _dirPath = string.Empty;
    [ObservableProperty] private string _backupDir = string.Empty;
    [ObservableProperty] private string _line = string.Empty;
    [ObservableProperty] private string _terminal = string.Empty;
    [ObservableProperty] private string _type = "TXT_BGA";
    [ObservableProperty] private string _apiCheck = "SJ_CHK_SVI_VAR";
    [ObservableProperty] private string _apiTravel = "SJ_SVI_TRANSFER_VAR";
    [ObservableProperty] private string _lineIdText = string.Empty;
    [ObservableProperty] private string _terminalIdText = string.Empty;

    [ObservableProperty] private bool _isWatching;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "就绪";

    public ObservableCollection<AoiResultItem> Results { get; } = new();

    // 下拉选项
    public string[] TypeOptions { get; } = { "TXT_BGA", "TXT_NBGA" };
    public string[] ApiCheckOptions { get; } = { "SJ_CHK_SVI_VAR" };
    public string[] ApiTravelOptions { get; } = { "SJ_SVI_TRANSFER_VAR" };

    // 线别 / 终端（从 SAJET 拿，这里用静态列表占位）
    public ObservableCollection<string> LineOptions { get; } = new();
    public ObservableCollection<string> TerminalOptions { get; } = new();

    private readonly AoiFileWatcher _watcher = new();

    // ============ 构造 ============

    public AoiViewModel()
    {
        LoadConfig();
        _watcher.Changed += () => Application.Current?.Dispatcher.Invoke(OnWatcherChanged);
    }

    // ============ 配置加载/保存 ============

    private void LoadConfig()
    {
        var c = _config.Data;
        DirPath = c.DirPath;
        BackupDir = c.BackupDir;
        ApiCheck = string.IsNullOrEmpty(c.ApiCheck) ? "SJ_CHK_SVI_VAR" : c.ApiCheck;
        ApiTravel = string.IsNullOrEmpty(c.ApiTravel) ? "SJ_SVI_TRANSFER_VAR" : c.ApiTravel;
        Type = string.IsNullOrEmpty(c.Type) ? "TXT_BGA" : c.Type;
        Line = c.Line;
        LineIdText = c.LineId.ToString();
        Terminal = c.Terminal;
        TerminalIdText = c.TerminalId.ToString();
    }

    [RelayCommand]
    private void SaveConfig()
    {
        var c = _config.Data;
        c.DirPath = DirPath;
        c.BackupDir = BackupDir;
        c.ApiCheck = ApiCheck;
        c.ApiTravel = ApiTravel;
        c.Type = Type;
        c.Line = Line;
        c.LineId = int.TryParse(LineIdText, out var lid) ? lid : 0;
        c.Terminal = Terminal;
        c.TerminalId = int.TryParse(TerminalIdText, out var tid) ? tid : 0;

        _config.Save();
        StatusText = "配置已保存";
        MessageBox.Show("配置信息已保存", "保存成功",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ============ 目录选择 ============

    [RelayCommand]
    private void SelectDir()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "选择源文件夹",
            InitialDirectory = Directory.Exists(DirPath) ? DirPath : string.Empty,
        };
        if (dlg.ShowDialog() == true) DirPath = dlg.FolderName;
    }

    [RelayCommand]
    private void SelectBackupDir()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "选择备份文件夹",
            InitialDirectory = Directory.Exists(BackupDir) ? BackupDir : string.Empty,
        };
        if (dlg.ShowDialog() == true) BackupDir = dlg.FolderName;
    }

    // ============ 批量执行 ============

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (!ValidatePaths()) return;
        if (!ValidateType()) return;

        IsBusy = true;
        StatusText = "正在处理...";

        int ok = 0, fail = 0;
        try
        {
            await Task.Run(() =>
            {
                var files = new DirectoryInfo(DirPath)
                    .GetFiles(GetSearchFilter())
                    .ToList();

                if (files.Count == 0) return;

                foreach (var fi in files)
                {
                    ProcessOneFile(fi.FullName, fi.Name, ref ok, ref fail);
                }
            });

            StatusText = $"处理完成：成功 {ok}，失败 {fail}";
            MessageBox.Show(
                $"处理完成\n成功: {ok} 个\n失败: {fail} 个\n已全部备份",
                "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"[AOI] Execute failed: {ex.Message}");
            MessageBox.Show($"处理失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ============ 监控 ============

    [RelayCommand]
    private void StartWatch()
    {
        if (IsWatching)
        {
            MessageBox.Show("监控已在运行中", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!ValidatePaths()) return;

        _watcher.Start(DirPath);
        IsWatching = true;
        StatusText = $"监控中：{DirPath}";

        // 立即处理一次已存在的文件
        OnWatcherChanged();
    }

    [RelayCommand]
    private void StopWatch()
    {
        _watcher.Stop();
        IsWatching = false;
        StatusText = "监控已停止";
    }

    // ============ 内部逻辑 ============

    private bool ValidatePaths()
    {
        if (string.IsNullOrWhiteSpace(DirPath) || !Directory.Exists(DirPath))
        {
            MessageBox.Show("源文件夹无效，请先选择", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (string.IsNullOrWhiteSpace(BackupDir) || !Directory.Exists(BackupDir))
        {
            MessageBox.Show("备份文件夹无效，请先选择", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private bool ValidateType()
    {
        if (Type is not ("TXT_BGA" or "TXT_NBGA"))
        {
            MessageBox.Show($"未知的处理类型：{Type}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private string GetSearchFilter() => Type switch
    {
        "TXT_BGA" => "*.txt",
        "TXT_NBGA" => "*.txt",
        _ => "*.txt",
    };

    private void OnWatcherChanged()
    {
        // 遍历目录，处理就绪文件
        if (!Directory.Exists(DirPath)) return;

        int ok = 0, fail = 0;
        var files = new DirectoryInfo(DirPath).GetFiles(GetSearchFilter());
        foreach (var fi in files)
        {
            // 稳定性检查：300ms 内大小不变才处理
            long size1 = fi.Length;
            if (size1 == 0) continue;
            System.Threading.Thread.Sleep(300);
            long size2 = new FileInfo(fi.FullName).Length;
            if (size1 != size2) continue;

            ProcessOneFile(fi.FullName, fi.Name, ref ok, ref fail);
        }

        if (ok + fail > 0)
            StatusText = $"已处理：成功 {ok}，失败 {fail}";
    }

    private void ProcessOneFile(string filePath, string fileName, ref int ok, ref int fail)
    {
        AoiRecord? record = null;
        string error = string.Empty;
        bool processOk = false;

        try
        {
            if (Type == "TXT_BGA")
                processOk = AoiFileProcessor.ProcessTxtBga(filePath, out record, out error);
            else if (Type == "TXT_NBGA")
                processOk = AoiFileProcessor.ProcessTxtNbga(filePath, out record, out error);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            processOk = false;
        }

        if (processOk && record != null)
        {
            var uploader = new AoiUploadService(ApiCheck, ApiTravel,
                int.TryParse(TerminalIdText, out var tid) ? tid : 0);

            if (uploader.Upload(record, out string uploadErr))
            {
                error = record.PassStatus == "RPASS" ? "OK" : "REPAIR";
            }
            else
            {
                error = $"上传失败：{uploadErr}";
                processOk = false;
            }
        }

        // 加到 UI 表格（回主线程）
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Results.Add(new AoiResultItem
            {
                LineName = record?.LineName ?? "-",
                SerialNumber = record?.SerialNumber ?? fileName,
                OutputTime = record?.OutputTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "-",
                Result = error,
                AbnormalCount = record?.AbnormalCount ?? 0,
                StatusKind = processOk
                    ? (error == "OK" ? "OK" : "REPAIR")
                    : "ERROR",
            });
        });

        // 移动文件到备份
        if (MoveToBackup(filePath, fileName, out string backupErr))
        {
            if (processOk) ok++; else fail++;
        }
        else
        {
            Logger.Warn($"[AOI] Backup failed: {backupErr}");
        }
    }

    private bool MoveToBackup(string filePath, string fileName, out string error)
    {
        error = string.Empty;
        try
        {
            string destPath = IOPath.Combine(BackupDir, fileName);

            if (File.Exists(destPath))
            {
                string baseName = IOPath.GetFileNameWithoutExtension(fileName);
                string ext = IOPath.GetExtension(fileName);
                string ts = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                destPath = IOPath.Combine(BackupDir, $"{baseName}_{ts}{ext}");
            }

            try
            {
                File.Move(filePath, destPath);
            }
            catch
            {
                File.Copy(filePath, destPath, overwrite: true);
                File.Delete(filePath);
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}