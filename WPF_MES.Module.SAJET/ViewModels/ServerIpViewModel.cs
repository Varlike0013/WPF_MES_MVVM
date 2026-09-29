using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Module.SAJET.Services;
using WPF_MES.Shared;

namespace WPF_MES.Module.SAJET.ViewModels;

public partial class ServerIpViewModel : ObservableObject
{
    // ============ 树 ============

    public ObservableCollection<TreeNodeViewModel> TreeNodes { get; } = new();

    // ============ 表格 ============

    public ObservableCollection<TerminalLinkItem> TerminalItems { get; } = new();

    // ============ 状态 ============

    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] private bool _isExporting;

    // ============ 构造 ============

    public ServerIpViewModel()
    {
        LoadTree();
    }

    private void LoadTree()
    {
        try
        {
            TreeNodes.Clear();
            foreach (var n in ServerIpService.LoadServerTree())
                TreeNodes.Add(n);

            StatusMessage = $"已加载 {TreeNodes.Count} 个服务器";
        }
        catch (Exception ex)
        {
            Logger.Error($"[SERVER-IP] Load tree failed: {ex.Message}");
            MessageBox.Show("加载服务器树失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============ 节点选中 ============

    public void OnNodeSelected(TreeNodeViewModel node)
    {
        if (node == null) return;

        switch (node.Type)
        {
            case TreeNodeType.Ip:
                // 点 IP 节点 → 加载该 IP 的终端（替换）
                LoadTerminalLinks(node.ServerId, node.GatewayId, node.DeviceIndex);
                break;

            case TreeNodeType.Server:
            case TreeNodeType.Gateway:
                // 服务器/网关节点：选中不做表格加载
                break;
        }
    }

    // ============ 节点展开 ============

    public void OnNodeExpanded(TreeNodeViewModel node)
    {
        if (node == null) return;

        // 只处理网关节点
        if (node.Type != TreeNodeType.Gateway) return;

        HandleGatewayExpanded(node);
    }

    private void HandleGatewayExpanded(TreeNodeViewModel gw)
    {
        try
        {
            // 1. 加载 IP 子节点（只加载一次）
            if (!gw.ChildrenLoaded)
            {
                LoadGatewayChildren(gw);
                gw.ChildrenLoaded = true;
            }

            // 2. 加载该网关下所有设备的终端到表格（替换）
            LoadAllTerminalsForGateway(gw);
        }
        catch (Exception ex)
        {
            Logger.Error($"[SERVER-IP] Load gateway failed: {ex.Message}");
            MessageBox.Show("加载网关数据失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 加载网关的 IP 子节点
    /// </summary>
    private void LoadGatewayChildren(TreeNodeViewModel gw)
    {
        var ips = ServerIpService.GetGatewayIps(
            gw.ServerId, gw.GatewayId, gw.DriverId);

        gw.Children.Clear();

        if (ips.Count == 0)
        {
            gw.Children.Add(new TreeNodeViewModel
            {
                Type = TreeNodeType.Ip,
                Title = "(无可用IP)",
                ServerId = gw.ServerId,
                GatewayId = gw.GatewayId,
            });
            return;
        }

        for (int i = 0; i < ips.Count; i++)
        {
            int idx = i + 1;
            string ip = ips[i];
            string termName = ServerIpService.GetTerminalNameForIp(
                gw.ServerId, gw.GatewayId, idx);

            string title = string.IsNullOrEmpty(termName)
                ? $"IP{idx}: {ip}"
                : $"IP{idx}: {ip} ({termName})";

            gw.Children.Add(new TreeNodeViewModel
            {
                Type = TreeNodeType.Ip,
                Title = title,
                ServerId = gw.ServerId,
                GatewayId = gw.GatewayId,
                Ip = ip,
                DeviceIndex = idx,
            });
        }
    }

    /// <summary>
    /// 加载该网关下所有设备的终端到表格（清空后加载）
    /// </summary>
    private void LoadAllTerminalsForGateway(TreeNodeViewModel gw)
    {
        TerminalItems.Clear();

        int deviceCount = gw.ConnectNumber;
        if (deviceCount <= 0) deviceCount = gw.Children.Count;

        for (int i = 1; i <= deviceCount; i++)
        {
            var list = ServerIpService.QueryTerminalLinks(
                gw.ServerId, gw.GatewayId, i);

            foreach (var item in list)
                TerminalItems.Add(item);
        }

        StatusMessage = $"网关 {gw.GatewayDesc}：{TerminalItems.Count} 条终端";
    }

    // ============ IP 节点点击：只显示该 IP 的终端 ============

    private void LoadTerminalLinks(string serverId, string gatewayId, int deviceId)
    {
        try
        {
            var list = ServerIpService.QueryTerminalLinks(serverId, gatewayId, deviceId);

            TerminalItems.Clear();
            foreach (var item in list)
                TerminalItems.Add(item);

            StatusMessage = $"设备 {deviceId}：{list.Count} 条终端";
        }
        catch (Exception ex)
        {
            Logger.Error($"[SERVER-IP] Load terminal links failed: {ex.Message}");
            MessageBox.Show("加载终端信息失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            TerminalItems.Clear();
        }
    }

    // ============ 命令：清空 ============

    [RelayCommand]
    private void Clear()
    {
        TerminalItems.Clear();
        StatusMessage = string.Empty;
    }

    // ============ 命令：导出 ============

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (IsExporting)
        {
            MessageBox.Show("正在导出，请等待完成。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 选文件
        var dialog = new SaveFileDialog
        {
            Title = "导出 CSV",
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            DefaultExt = "csv",
            FileName = "TGS_SERVER_IPS.csv",
            InitialDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop),
        };

        if (dialog.ShowDialog() != true) return;

        string filePath = dialog.FileName;

        IsExporting = true;
        StatusMessage = "导出中，请稍候...";

        try
        {
            await Task.Run(() =>
            {
                ServerIpService.ExportGatewayData(filePath,
                    (current, total) =>
                    {
                        // 后台线程 → Dispatcher 更新 UI
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = $"导出中... ({current}/{total})";
                        });
                    });
            });

            Logger.Info($"[SERVER-IP] Export OK: {filePath}");

            StatusMessage = "导出完成";

            var result = MessageBox.Show(
                $"导出成功！\n\n文件：{filePath}\n\n是否打开文件？",
                "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[SERVER-IP] Export failed: {ex.Message}");
            MessageBox.Show("导出失败：" + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "导出失败";
        }
        finally
        {
            IsExporting = false;
        }
    }
}