using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using WPF_MES.Shared;
using WPF_MES.Shared.Models;
using WPF_MES.Shared.Network;
using WPF_MES.Shared.Services;

namespace WPF_MES_MVVM.ViewModels;

public partial class ShareInfoViewModel : ObservableObject
{
    public ObservableCollection<ShareNode> RootNodes { get; } = new();

    [ObservableProperty] private string _shareRoot = ShareFileService.DefaultShareRoot;
    [ObservableProperty] private string _fileName = ShareFileService.DefaultFileName;
    [ObservableProperty] private string _connectionStatus = "未连接";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private ShareNode? _selectedNode;

    public string FullPath => Path.Combine(ShareRoot.TrimEnd('\\'), FileName);

    public ShareInfoViewModel()
    {
        UpdateStatus();
    }

    partial void OnShareRootChanged(string value) => UpdateStatus();
    partial void OnFileNameChanged(string value) => UpdateStatus();

    private void UpdateStatus()
    {
        try
        {
            ConnectionStatus = Directory.Exists(ShareRoot) ? "已连接" : "未连接";
        }
        catch { ConnectionStatus = "未连接"; }
    }

    // ============ 连接 / 断开 ============

    [RelayCommand]
    private void Connect()
    {
        var dlg = new Views.NetworkLoginWindow(ShareRoot)
        {
            Owner = Application.Current.MainWindow
        };
        if (dlg.ShowDialog() != true) return;

        var (ok, msg) = NetworkShareConnector.Connect(ShareRoot, dlg.UserName, dlg.Password);
        Logger.Info($"[SHARE] Connect: {ok} - {msg}");
        if (!ok)
        {
            MessageBox.Show($"连接失败：{msg}", "网络共享",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        UpdateStatus();
        LoadDocument();   // 连接成功后自动加载
    }

    [RelayCommand]
    private void Disconnect()
    {
        NetworkShareConnector.Disconnect(ShareRoot);
        UpdateStatus();
    }

    // ============ 加载 / 保存 ============

    [RelayCommand]
    private void LoadDocument()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var doc = ShareFileService.Load(FullPath);

            RootNodes.Clear();
            foreach (var n in doc.RootNodes) RootNodes.Add(n);

            SelectedNode = RootNodes.FirstOrDefault();
            Logger.Info($"[SHARE] Loaded {RootNodes.Count} root nodes from {FullPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[SHARE] Load failed: {ex.Message}");
            MessageBox.Show($"加载失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SaveDocument()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var doc = new ShareTreeDocument();
            foreach (var n in RootNodes) doc.RootNodes.Add(n);

            ShareFileService.Save(FullPath, doc);

            MessageBox.Show("已保存到共享路径。", "保存成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Logger.Info($"[SHARE] Saved to {FullPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[SHARE] Save failed: {ex.Message}");
            MessageBox.Show($"保存失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    // ============ 树操作 ============

    [RelayCommand]
    private void AddRootNode()
    {
        var node = new ShareNode { Title = "新根节点", Author = CurrentUser.UserName };
        RootNodes.Add(node);
        SelectedNode = node;
    }

    [RelayCommand]
    private void AddChildNode()
    {
        if (SelectedNode == null)
        {
            MessageBox.Show("请先选中一个节点。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var child = new ShareNode { Title = "新子节点", Author = CurrentUser.UserName };
        SelectedNode.Children.Add(child);
        SelectedNode.IsExpanded = true;
        SelectedNode = child;
    }

    [RelayCommand]
    private void DeleteNode()
    {
        if (SelectedNode == null) return;

        var confirm = MessageBox.Show(
            $"确定要删除节点 \"{SelectedNode.Title}\" 及其全部子节点吗？",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        if (RootNodes.Remove(SelectedNode)) { SelectedNode = null; return; }

        // 递归找父级
        if (FindAndRemove(RootNodes, SelectedNode))
            SelectedNode = null;
    }

    private static bool FindAndRemove(IEnumerable<ShareNode> nodes, ShareNode target)
    {
        foreach (var n in nodes)
        {
            if (n.Children.Remove(target)) return true;
            if (FindAndRemove(n.Children, target)) return true;
        }
        return false;
    }

    // ============ 图片 ============

    [RelayCommand]
    private void PickImage()
    {
        Logger.Info($"[SHARE] PickImage called. SelectedNode={(SelectedNode == null ? "null" : SelectedNode.Title)}");

        if (SelectedNode == null)
        {
            MessageBox.Show("请先在左侧信息树中选择一个节点。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new OpenFileDialog
        {
            Title = "选择图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var bytes = File.ReadAllBytes(dlg.FileName);
            SelectedNode.ImageBase64 = Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"读取图片失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ClearImage()
    {
        if (SelectedNode == null) return;
        SelectedNode.ImageBase64 = string.Empty;
    }
}