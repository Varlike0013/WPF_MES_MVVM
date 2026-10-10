using System.Windows;
using WPF_MES_MVVM.Services;
using System.Windows.Media.Imaging;
using WPF_MES.Shared;
using WPF_MES_MVVM.ViewModels;
using WPF_MES_MVVM.Views;
using WPF_MES.Contracts;

namespace WPF_MES_MVVM;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 1. 解析启动参数
        Logger.Init(e.Args);
        Logger.Info($"[START] Args={string.Join(" ", e.Args)}");
        Logger.Info($"[START] Console={Logger.ConsoleEnabled}");

        // 2. 加载模块
        var loader = new ModuleLoader();
        var modules = loader.LoadAllModules(out var errors);

        foreach (var err in errors)
            Logger.Error($"[MODULE ERROR] {err}");

        if (modules.Count == 0)
        {
            Logger.Error("[MODULE] No modules found.");
            MessageBox.Show(
                "没有找到任何可用模块。\n\n" +
                $"请确认 Modules 目录下存在模块 DLL。\n" +
                $"目录：{System.IO.Path.Combine(AppContext.BaseDirectory, "Modules")}",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        foreach (var m in modules)
            Logger.Info($"[MODULE LOADED] Key={m.ModuleKey}, Name={m.DisplayName}");

        foreach (var m in modules)
            Logger.Info($"[MODULE] Loaded: {m.ModuleKey} - {m.DisplayName}");

        // 3. 登录 → Shell 循环
        RunLoginLoop(modules);
    }
    /// <summary>
    /// 登录 → 主界面 循环。注销后回到登录，退出则结束。
    /// </summary>
    private void RunLoginLoop(List<IModule> modules)
    {
        while (true)
        {
            // ============================================================
            // 1. 登录
            // ============================================================
            var loginVm = new LoginViewModel(modules);
            var login = new LoginWindow { DataContext = loginVm };

            IModule? selectedModule = null;
            LoginResult? loginResult = null;

            loginVm.LoginSucceeded += (module, result) =>
            {
                selectedModule = module;
                loginResult = result;
                login.DialogResult = true;
            };

            bool? dialogResult = login.ShowDialog();

            // 用户取消或关闭登录窗口
            if (dialogResult != true || selectedModule == null || loginResult == null)
            {
                Logger.Info("[LOGIN] Cancelled, exiting.");
                Shutdown();
                return;
            }

            Logger.Info($"[LOGIN OK] Module={selectedModule.ModuleKey}, UserNo={loginResult.UserNo}");

            // ============================================================
            // 2. 主界面分流
            // ============================================================
            bool needRelogin = false;

            if (selectedModule is IStandaloneWindowModule standalone)
            {
                // ---- 独立窗口模块（AOI 等）----
                needRelogin = RunStandaloneWindow(standalone, loginResult);
            }
            else
            {
                // ---- 菜单式模块（SAJET 等）----
                needRelogin = RunShellWindow(selectedModule, loginResult);
            }

            // ============================================================
            // 3. 循环判断
            // ============================================================
            if (!needRelogin)
            {
                Logger.Info("[EXIT] Program exit.");
                Shutdown();
                return;
            }

            Logger.Info("[LOGOUT] Back to login.");
        }
    }

    /// <summary>
    /// 打开独立窗口模块。
    /// 返回 true → 回到登录；返回 false → 退出程序。
    /// 独立窗口默认关闭即退出，不支持注销。
    /// </summary>
    private bool RunStandaloneWindow(IStandaloneWindowModule standalone, LoginResult loginResult)
    {
        Logger.Info($"[OPEN] Standalone window for {standalone.GetType().Name}");
        var win = standalone.CreateMainWindow(loginResult.UserName);
        win.ShowDialog();

        // 目前独立窗口不支持注销：关闭即退出
        // 若将来要支持，可在 Window 里暴露事件，参照 Shell 的 LogoutRequested 模式
        Logger.Info("[EXIT] Standalone window closed.");
        return false;
    }

    /// <summary>
    /// 打开 Shell 窗口。
    /// 返回 true → 用户点了注销，回到登录；返回 false → 用户关闭窗口，退出程序。
    /// </summary>
    private bool RunShellWindow(IModule module, LoginResult loginResult)
    {
        bool isLogout = false;

        var shellVm = new ShellViewModel(module, loginResult);
        var shell = new ShellWindow { DataContext = shellVm };

        shellVm.LogoutRequested += () =>
        {
            isLogout = true;
            Logger.Info("[LOGOUT] User requested logout.");
            shell.Close();
        };

        shell.ShowDialog();

        return isLogout;
    }
    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("[EXIT] Application exiting.");
        Logger.Shutdown();
        base.OnExit(e);
    }
}