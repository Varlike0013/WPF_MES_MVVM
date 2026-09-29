using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Oracle.ManagedDataAccess.Client;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Views.Dialogs;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class QueryFormViewModel : ObservableObject
{
    // ============ 左侧：查询列表 ============

    private List<SavedQuery> _allQueries = new();

    public ObservableCollection<SavedQuery> FilteredQueries { get; } = new();

    [ObservableProperty] private SavedQuery? _selectedQuery;

    [ObservableProperty] private string _filterText = string.Empty;

    /// <summary>过滤文本变化 → 实时过滤</summary>
    partial void OnFilterTextChanged(string value) => ApplyFilter(value);

    /// <summary>选中项变化 → 显示描述 + SQL + 清空参数</summary>
    partial void OnSelectedQueryChanged(SavedQuery? value)
    {
        if (value == null)
        {
            EditDescription = string.Empty;
            EditSql = string.Empty;
            ParamItems.Clear();
            HasParams = false;
            return;
        }

        EditDescription = value.Description;
        EditSql = value.Sql;

        // 切换选中时清空参数
        ParamItems.Clear();
        HasParams = false;
    }

    // ============ 左侧：编辑区 ============

    [ObservableProperty] private string _editDescription = string.Empty;
    [ObservableProperty] private string _editSql = string.Empty;

    // ============ 中间：参数区 ============

    public ObservableCollection<SqlParamItem> ParamItems { get; } = new();

    [ObservableProperty] private bool _hasParams;

    // ============ 右侧：结果 ============

    [ObservableProperty] private DataView? _resultData;

    [ObservableProperty] private string _statusMessage = string.Empty;

    // ============ 类型下拉 ============

    public List<string> QueryTypes { get; } = new()
    {
        "查询", "修改", "删除", "增加", "其他",
    };

    // ============ 构造 ============

    public QueryFormViewModel()
    {
        LoadQueries();
    }

    private void LoadQueries()
    {
        try
        {
            _allQueries = SqlQueryService.LoadAll();
            ApplyFilter(FilterText);
        }
        catch (Exception ex)
        {
            Logger.Error($"[SQLQUERY] LoadQueries failed: {ex.Message}");
            MessageBox.Show("加载查询配置失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter(string filter)
    {
        FilteredQueries.Clear();

        filter = filter?.Trim() ?? string.Empty;

        foreach (var q in _allQueries)
        {
            if (filter.Length == 0 ||
                q.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                FilteredQueries.Add(q);
            }
        }

        // 选中项如果被过滤掉了，清空选中
        if (SelectedQuery != null && !FilteredQueries.Contains(SelectedQuery))
            SelectedQuery = null;
    }

    // ============ 命令：新增 ============

    [RelayCommand]
    private void Add()
    {
        // 直接打开编辑对话框，包含：名称 + 类型 + 描述 + SQL
        var addDialog = new SqlEditDialog("", "查询", "", "")
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (addDialog.ShowDialog() != true) return;

        try
        {
            // 检查名称是否已存在
            var existing = SqlQueryService.LoadAll();
            if (existing.Any(q => q.Name == addDialog.QueryName))
            {
                MessageBox.Show($"名称 [{addDialog.QueryName}] 已存在，请更换。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SqlQueryService.Add(new SavedQuery
            {
                Name = addDialog.QueryName,
                Type = addDialog.QueryType,
                TypeId = addDialog.TypeId,
                Description = addDialog.Description,
                Sql = addDialog.SqlText,
            });

            LoadQueries();

            MessageBox.Show("SQL 语句已保存。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("保存失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：修改 ============

    [RelayCommand]
    private void Update()
    {
        if (SelectedQuery == null)
        {
            MessageBox.Show("请先选择一条 SQL 语句。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string sql = EditSql.Trim();
        string desc = EditDescription.Trim();

        if (sql.Length == 0)
        {
            MessageBox.Show("SQL 语句不能为空。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (desc.Length == 0)
        {
            MessageBox.Show("描述不能为空。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            SqlQueryService.Update(SelectedQuery.Name, sql, desc);

            // 同步到内存对象
            SelectedQuery.Sql = sql;
            SelectedQuery.Description = desc;

            MessageBox.Show("SQL 语句已更新。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("更新失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：删除 ============

    [RelayCommand]
    private void Delete()
    {
        if (SelectedQuery == null)
        {
            MessageBox.Show("请先选择一条 SQL 语句。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"确定要删除 SQL 语句 \"{SelectedQuery.Name}\" 吗？",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            string name = SelectedQuery.Name;
            SqlQueryService.Delete(name);

            LoadQueries();

            // 清空选中
            SelectedQuery = null;

            MessageBox.Show($"已删除 SQL 语句 \"{name}\"", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("删除失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：构建参数 ============

    [RelayCommand]
    private void Build()
    {
        string sql = EditSql.Trim();
        if (sql.Length == 0)
        {
            MessageBox.Show("SQL 语句为空，请先加载或输入。", "警告",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ParamItems.Clear();

        // 正则解析 :name
        var re = new Regex(@":([a-zA-Z_][a-zA-Z0-9_]*)");
        var seen = new HashSet<string>();

        foreach (Match m in re.Matches(sql))
        {
            string param = m.Groups[1].Value;
            if (seen.Add(param))
                ParamItems.Add(new SqlParamItem(param));
        }

        if (ParamItems.Count == 0)
        {
            HasParams = false;
            MessageBox.Show("此 SQL 无需参数，可直接执行。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        HasParams = true;
        StatusMessage = $"已解析 {ParamItems.Count} 个参数";
    }

    // ============ 命令：执行 ============

    [RelayCommand]
    private void Execute()
    {
        if (SelectedQuery == null)
        {
            MessageBox.Show("请先选择一条 SQL 语句。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string sql = SelectedQuery.Sql.Trim();
        if (sql.EndsWith(";")) sql = sql.Substring(0, sql.Length - 1);

        if (sql.Length == 0)
        {
            MessageBox.Show("SQL 语句为空。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 收集参数
        var ps = new Dictionary<string, object>();
        foreach (var p in ParamItems)
        {
            ps[p.Name] = string.IsNullOrEmpty(p.Value) ? DBNull.Value : p.Value;
        }

        try
        {
            int typeId = SelectedQuery.TypeId;

            if (typeId == 0 || typeId == 4)
            {
                // 查询：显示结果
                var dt = OracleHelper.QueryDataTable(sql, ps);
                ResultData = dt.DefaultView;
                StatusMessage = $"查询完成，共 {dt.Rows.Count} 行";

                if (dt.Rows.Count == 0)
                    MessageBox.Show("查询结果为空。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show($"查询完成，共 {dt.Rows.Count} 行。", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // 修改/删除/增加：执行并显示影响行数
                int affected = OracleHelper.ExecuteNonQuery(sql, ps);
                StatusMessage = $"操作完成，影响 {affected} 行";
                MessageBox.Show($"操作成功，影响 {affected} 行。", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[SQLQUERY] Execute failed: {ex.Message}");
            MessageBox.Show("执行失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 命令：导出 ============

    [RelayCommand]
    private void Export()
    {
        if (ResultData == null || ResultData.Table == null ||
            ResultData.Table.Rows.Count == 0)
        {
            MessageBox.Show("没有数据可导出。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "导出 CSV",
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"Query_{DateTime.Now:yyyyMMddHHmmss}.csv",
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var dt = ResultData.Table;

            string[] headers = dt.Columns
                .Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .ToArray();

            var rows = dt.Rows.Cast<DataRow>().ToList();

            CsvExporter.Export(dialog.FileName, headers, rows,
                r => headers.Select(h => r[h]?.ToString() ?? "").ToArray());

            Logger.Info($"[SQLQUERY] Export OK: {dialog.FileName}");

            var result = MessageBox.Show(
                $"导出成功！\n\n文件：{dialog.FileName}\n\n是否打开文件？",
                "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}