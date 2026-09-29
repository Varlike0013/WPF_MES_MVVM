using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class CheckEmpViewModel : ObservableObject
{
    // ============ 查询条件 ============

    public List<QueryTypeItem> QueryTypes { get; } = new()
    {
        new(0, "工号"),
        new(1, "名称"),
    };

    [ObservableProperty] private QueryTypeItem? _selectedQueryType;
    [ObservableProperty] private string _inputText = string.Empty;

    // ============ 员工列表 ============

    public ObservableCollection<EmpInfoItem> EmpItems { get; } = new();

    [ObservableProperty] private EmpInfoItem? _selectedEmp;

    // ============ 角色列表 ============

    public ObservableCollection<RoleItem> RoleItems { get; } = new();

    /// <summary>原角色 ID 集合（用于保存时比对差异）</summary>
    private HashSet<int> _originalRoleIds = new();

    // ============ 复制权限 ============

    [ObservableProperty] private string _copyFromEmpNo = string.Empty;

    // ============ 信息编辑 ============

    [ObservableProperty] private string _editEmpNo = string.Empty;
    [ObservableProperty] private string _editEmpName = string.Empty;
    [ObservableProperty] private string _editEmail = string.Empty;
    [ObservableProperty] private string _editDesc = string.Empty;

    public ObservableCollection<DeptOption> DeptOptions { get; } = new();
    [ObservableProperty] private DeptOption? _selectedDept;

    [ObservableProperty] private bool _isQuit;
    [ObservableProperty] private DateTime? _quitDate;

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 构造 ============

    public CheckEmpViewModel()
    {
        SelectedQueryType = QueryTypes[0];

        LoadRoles();
        LoadDepts();
    }

    // ============ 加载 ============

    private void LoadRoles()
    {
        try
        {
            RoleItems.Clear();
            foreach (var r in CheckEmpService.GetAllRoles())
                RoleItems.Add(r);
        }
        catch (Exception ex)
        {
            Logger.Error($"[EMP] Load roles failed: {ex.Message}");
            MessageBox.Show("加载角色失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadDepts()
    {
        try
        {
            DeptOptions.Clear();
            foreach (var d in CheckEmpService.GetAllDepts())
                DeptOptions.Add(d);
        }
        catch (Exception ex)
        {
            Logger.Error($"[EMP] Load depts failed: {ex.Message}");
            MessageBox.Show("加载部门失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 选中员工变化 ============

    partial void OnSelectedEmpChanged(EmpInfoItem? value)
    {
        if (value == null)
        {
            ClearRoleChecks();
            ClearEditInfo();
            return;
        }

        LoadEmployeeRoles(value.EmpNo);
        LoadEmployeeInfo(value.EmpNo);

        StatusMessage = $"当前员工：{value.EmpName} ({value.EmpNo})";
    }

    /// <summary>
    /// 加载该员工的角色勾选状态
    /// </summary>
    private void LoadEmployeeRoles(string empNo)
    {
        try
        {
            _originalRoleIds = CheckEmpService.GetEmployeeRoleIds(empNo);

            foreach (var role in RoleItems)
                role.IsChecked = _originalRoleIds.Contains(role.RoleId);
        }
        catch (Exception ex)
        {
            Logger.Error($"[EMP] Load roles failed: {ex.Message}");
            MessageBox.Show("加载角色失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 加载该员工的完整信息到编辑区
    /// </summary>
    private void LoadEmployeeInfo(string empNo)
    {
        try
        {
            var detail = CheckEmpService.GetEmployeeDetail(empNo);
            if (detail == null) return;

            EditEmpNo = detail.Value.EmpNo;
            EditEmpName = detail.Value.EmpName;
            EditEmail = detail.Value.Email;
            EditDesc = detail.Value.Desc;

            // 部门
            SelectedDept = DeptOptions.FirstOrDefault(d => d.DeptId == detail.Value.DeptId);

            // 离职
            bool isQuit = detail.Value.Enabled == "N";
            IsQuit = isQuit;
            QuitDate = detail.Value.QuitDate;
        }
        catch (Exception ex)
        {
            Logger.Error($"[EMP] Load info failed: {ex.Message}");
        }
    }

    // ============ 查询员工 ============

    [RelayCommand]
    private void Search()
    {
        string input = InputText.Trim();
        if (input.Length == 0)
        {
            EmpItems.Clear();
            return;
        }

        try
        {
            int type = SelectedQueryType?.Type ?? 0;
            var list = CheckEmpService.QueryEmployees(type, input);

            EmpItems.Clear();
            foreach (var item in list)
                EmpItems.Add(item);

            StatusMessage = $"共查询到 {list.Count} 位员工";
        }
        catch (Exception ex)
        {
            Logger.Error($"[EMP] Search failed: {ex.Message}");
            MessageBox.Show("查询失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 保存角色 ============

    [RelayCommand]
    private void SaveRoles()
    {
        if (SelectedEmp == null)
        {
            MessageBox.Show("请先在员工列表中选择一位员工。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 收集新勾选的角色
        var newRoleIds = new HashSet<int>(
            RoleItems.Where(r => r.IsChecked).Select(r => r.RoleId));

        // 判断是否有变化
        bool same = newRoleIds.SetEquals(_originalRoleIds);
        if (same)
        {
            MessageBox.Show("权限没有变化。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 确认
        var confirm = MessageBox.Show(
            $"确定要保存员工「{SelectedEmp.EmpName}」的角色权限吗？",
            "确认保存",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = CheckEmpService.SaveEmployeeRoles(
                SelectedEmp.EmpNo, _originalRoleIds, newRoleIds);

            Logger.Info($"[EMP] Save roles OK: {SelectedEmp.EmpNo}, affected={affected}");

            _originalRoleIds = newRoleIds;

            MessageBox.Show("角色权限已保存。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"[EMP] Save roles failed: {ex.Message}");
            MessageBox.Show("保存失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 清空勾选 ============

    [RelayCommand]
    private void ClearRoles()
    {
        foreach (var role in RoleItems)
            role.IsChecked = false;
    }

    // ============ 全选 ============

    [RelayCommand]
    private void CheckAllRoles()
    {
        foreach (var role in RoleItems)
            role.IsChecked = true;
    }

    // ============ 复制权限 ============

    [RelayCommand]
    private void CopyRoles()
    {
        if (SelectedEmp == null)
        {
            MessageBox.Show("请先在员工列表中选择一位员工。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string sourceEmpNo = CopyFromEmpNo.Trim();
        if (sourceEmpNo.Length == 0)
        {
            MessageBox.Show("请输入要复制权限的员工工号。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (sourceEmpNo == SelectedEmp.EmpNo)
        {
            MessageBox.Show("不能从自己复制权限。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!CheckEmpService.ExistsEmployee(sourceEmpNo))
        {
            MessageBox.Show($"未找到员工：{sourceEmpNo}", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var sourceRoleIds = CheckEmpService.GetEmployeeRoleIds(sourceEmpNo);

            foreach (var role in RoleItems)
                role.IsChecked = sourceRoleIds.Contains(role.RoleId);

            StatusMessage = $"已复制 {sourceEmpNo} 的权限（未保存）";
            CopyFromEmpNo = string.Empty;
        }
        catch (Exception ex)
        {
            Logger.Error($"[EMP] Copy roles failed: {ex.Message}");
            MessageBox.Show("复制权限失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 保存员工信息 ============

    [RelayCommand]
    private void SaveInfo()
    {
        if (string.IsNullOrWhiteSpace(EditEmpNo))
        {
            MessageBox.Show("请输入工号。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(EditEmpName))
        {
            MessageBox.Show("请输入姓名。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IsQuit && !QuitDate.HasValue)
        {
            MessageBox.Show("已标记离职，请选择离职日期。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool exists = CheckEmpService.ExistsEmployee(EditEmpNo);

        string action = exists ? "修改" : "新增";

        var confirm = MessageBox.Show(
            $"确定要{action}员工「{EditEmpName}」的信息吗？",
            $"确认{action}",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            int affected = CheckEmpService.SaveEmployeeInfo(
                EditEmpNo,
                EditEmpName,
                EditEmail,
                SelectedDept?.DeptId ?? 0,
                EditDesc,
                IsQuit,
                QuitDate);

            Logger.Info($"[EMP] Save info OK: {EditEmpNo}, affected={affected}");

            MessageBox.Show($"{action}成功。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新员工列表
            if (!string.IsNullOrEmpty(InputText))
                Search();
        }
        catch (Exception ex)
        {
            Logger.Error($"[EMP] Save info failed: {ex.Message}");
            MessageBox.Show($"{action}失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 内部：清理 ============

    private void ClearRoleChecks()
    {
        foreach (var role in RoleItems)
            role.IsChecked = false;

        _originalRoleIds.Clear();
    }

    private void ClearEditInfo()
    {
        EditEmpNo = string.Empty;
        EditEmpName = string.Empty;
        EditEmail = string.Empty;
        EditDesc = string.Empty;
        SelectedDept = null;
        IsQuit = false;
        QuitDate = null;
    }
}