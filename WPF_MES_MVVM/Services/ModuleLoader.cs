using System.IO;
using System.Reflection;
using WPF_MES.Contracts;

namespace WPF_MES_MVVM.Services;

/// <summary>
/// 扫描 Modules 目录，反射加载所有实现 IModule 的 DLL
/// </summary>
public class ModuleLoader
{
    private readonly string _moduleDir;

    public ModuleLoader()
    {
        _moduleDir = Path.Combine(AppContext.BaseDirectory, "Modules");
    }

    /// <summary>
    /// 加载所有模块。失败时通过 out 参数返回错误信息
    /// </summary>
    public List<IModule> LoadAllModules(out List<string> errors)
    {
        errors = new List<string>();
        var modules = new List<IModule>();

        if (!Directory.Exists(_moduleDir))
        {
            errors.Add($"Modules 目录不存在：{_moduleDir}");
            return modules;
        }

        var dlls = Directory.GetFiles(_moduleDir, "WPF_MES.Module.*.dll");
        if (dlls.Length == 0)
        {
            errors.Add($"Modules 目录下没有任何模块 DLL：{_moduleDir}");
            return modules;
        }

        foreach (var dll in dlls)
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);

                var types = asm.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t)
                                && !t.IsInterface
                                && !t.IsAbstract);

                foreach (var t in types)
                {
                    if (Activator.CreateInstance(t) is IModule m)
                    {
                        modules.Add(m);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var msg = string.Join("; ",
                    ex.LoaderExceptions.Where(e => e != null).Select(e => e!.Message));
                errors.Add($"{Path.GetFileName(dll)} 加载失败：{msg}");
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(dll)} 加载失败：{ex.Message}");
            }
        }

        return modules;
    }
}