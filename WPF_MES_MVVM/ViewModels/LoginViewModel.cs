using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using WPF_MES.Contracts;
using WPF_MES.Shared;

namespace WPF_MES_MVVM.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly List<IModule> _modules;
    private readonly JsonConfig<Models.LoginSettings> _settings;

    public event Action<IModule, LoginResult>? LoginSucceeded;

    public ObservableCollection<IModule> Modules { get; }
    public ObservableCollection<string> UserNoHistory { get; } = new();

    [ObservableProperty] private IModule? _selectedModule;
    [ObservableProperty] private string _userNo = string.Empty;

    public LoginViewModel(List<IModule> modules)
    {
        _modules = modules;
        Modules = new ObservableCollection<IModule>(modules);

        _settings = new JsonConfig<Models.LoginSettings>("login.json");
        LoadSettings();
    }

    // ============ 设置读写 ============

    private void LoadSettings()
    {
        var data = _settings.Data;

        // 加载历史用户名
        UserNoHistory.Clear();
        foreach (var name in data.UserNoHistory)
        {
            if (!string.IsNullOrWhiteSpace(name))
                UserNoHistory.Add(name);
        }

        // 默认选中最近一个
        if (UserNoHistory.Count > 0)
            UserNo = UserNoHistory[0];

        // 恢复上次模块
        IModule? lastModule = null;
        if (!string.IsNullOrEmpty(data.LastModuleKey))
            lastModule = Modules.FirstOrDefault(m => m.ModuleKey == data.LastModuleKey);

        SelectedModule = lastModule ?? Modules.FirstOrDefault();
    }

    /// <summary>
    /// 登录成功后保存：
    /// 1. 记录模块
    /// 2. 用户名去重后插到最前
    /// 3. 超出上限时从末尾（最远的记录）删除
    /// </summary>
    private void SaveSettings(string moduleKey, string userNo)
    {
        if (string.IsNullOrWhiteSpace(userNo)) return;

        _settings.Data.LastModuleKey = moduleKey;

        var list = _settings.Data.UserNoHistory;

        // 去重（忽略大小写），移除已存在的同名项
        list.RemoveAll(x => string.Equals(x, userNo, StringComparison.OrdinalIgnoreCase));

        // 插到最前，作为"最近使用"
        list.Insert(0, userNo);

        // 超出上限 → 从末尾删除最远的记录
        int max = _settings.Data.MaxHistoryCount > 0 ? _settings.Data.MaxHistoryCount : 10;
        if (list.Count > max)
        {
            int removeCount = list.Count - max;
            list.RemoveRange(max, removeCount);   // 从索引 max 开始，删到末尾
        }

        try
        {
            _settings.Save();
        }
        catch
        {
            // JsonConfig 内部已记录日志
        }

        // 同步到 UI 集合
        RefreshHistoryFromData();
    }

    private void RefreshHistoryFromData()
    {
        UserNoHistory.Clear();
        foreach (var name in _settings.Data.UserNoHistory)
            UserNoHistory.Add(name);
    }

    // ============ 登录 ============

    public void DoLogin(string? password)
    {
        if (SelectedModule == null)
        {
            MessageBox.Show("请选择要登录的系统。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(UserNo))
        {
            MessageBox.Show("请输入用户名。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            MessageBox.Show("请输入密码。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var auth = SelectedModule.CreateAuthService();
            var result = auth.Login(UserNo.Trim(), password);

            if (result.Success)
            {
                CurrentUser.UserNo = result.UserNo;
                CurrentUser.UserName = result.UserName;
                CurrentUser.ModuleKey = SelectedModule.ModuleKey;
                SaveSettings(SelectedModule.ModuleKey, UserNo.Trim());
                LoginSucceeded?.Invoke(SelectedModule, result);
            }
            else
            {
                MessageBox.Show(
                    string.IsNullOrEmpty(result.Message) ? "用户名或密码错误。" : result.Message,
                    "登录失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("登录失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}