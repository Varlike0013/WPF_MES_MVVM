using System.Windows;
using WPF_MES_MVVM.Services;
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
    /// 登录 → Shell 循环。注销后回到登录，退出则结束。
    /// </summary>
    private void RunLoginLoop(List<IModule> modules)
    {
        while (true)
        {
            // ============ 1. 登录 ============
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

            Logger.Info($"[LOGIN OK] Module={selectedModule.ModuleKey}, " +
                        $"UserNo={loginResult.UserNo}");

            // ============ 2. Shell ============
            bool isLogout = false;

            var shellVm = new ShellViewModel(selectedModule, loginResult);
            var shell = new ShellWindow { DataContext = shellVm };

            shellVm.LogoutRequested += () =>
            {
                isLogout = true;
                Logger.Info("[LOGOUT] User requested logout.");
                shell.Close();
            };

            shell.ShowDialog();

            // ============ 3. 判断 ============
            if (!isLogout)
            {
                // 用户点"退出"或关闭窗口
                Logger.Info("[EXIT] Shell closed, exiting.");
                Shutdown();
                return;
            }

            // 否则是注销 → 循环回登录
            Logger.Info("[LOGOUT] Back to login.");
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("[EXIT] Application exiting.");
        Logger.Shutdown();
        base.OnExit(e);
    }
}