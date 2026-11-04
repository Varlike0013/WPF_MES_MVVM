using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF_MES.Module.ReelUp.Models;
using WPF_MES.Module.ReelUp.Services;
using WPF_MES.Module.ReelUp.Views;
using Application = System.Windows.Application;
using TreeNode = WPF_MES.Module.ReelUp.Models.TreeNode;

namespace WPF_MES.Module.ReelUp.ViewModels;

public partial class ReelUpMainViewModel : ObservableObject, IDisposable
{
    private readonly ReelUpCommunicationManager _commManager;
    private readonly ReelUpDao _reelUpDao;
    private readonly InputOutputDao _ioDao;

    [ObservableProperty] private string _userStatusText = string.Empty;
    [ObservableProperty] private bool _isDeveloper;
    [ObservableProperty] private string _deviceStatusText = "系统就绪，请手动连接设备";

    // 楼层下拉框
    public ObservableCollection<string> AvailableFloors { get; } = new(
        Models.Floors.All.Select(f => f.Code).OrderBy(c => c)
    );
    [ObservableProperty] private string _selectedFloor = "B302F";
    [ObservableProperty] private string _cfgConnString = string.Empty;
    [ObservableProperty] private int _cfgGunTimeout = 300000;
    [ObservableProperty] private string _cfgDefaultBuilding = "B4";

    // 树与数据源
    public ObservableCollection<TreeNode> NavigationTree { get; } = new();
    public ObservableCollection<ReceiveBean> ReelUpLogs { get; } = new();
    public ObservableCollection<ReceiveBean> IoLogs { get; } = new();
    public ObservableCollection<IDInfo> ReelUpConfigs { get; } = new();
    public ObservableCollection<IDInfo> IoConfigs { get; } = new();

    public ReelUpMainViewModel()
    {
        _reelUpDao = new ReelUpDao();
        _ioDao = new InputOutputDao();

        var wlsCore = new WlsCore(_reelUpDao);
        var timeoutMonitor = new GunTimeoutMonitor(_reelUpDao, ReelUpConfigStore.Current);

        _commManager = new ReelUpCommunicationManager(_reelUpDao, _ioDao, wlsCore, timeoutMonitor, ReelUpConfigStore.Current);

        // 订阅事件
        _commManager.OnDataReceived += OnDataReceived;
        _commManager.OnConnectionStateChanged += OnConnectionStateChanged;

        InitUserInfo();
        LoadTreeAndConfigs();

        var cfg = ReelUpConfigStore.Current;
        CfgConnString = cfg.ConnectionString;
        CfgGunTimeout = cfg.GunTimeoutMs;
        CfgDefaultBuilding = cfg.DefaultBuilding;

        LoadTreeAndConfigs();
    }

    private void InitUserInfo()
    {
        var session = UserSession.Instance;
        IsDeveloper = session.IsDeveloper;
        string role = IsDeveloper ? "Developer" : "Operator";
        UserStatusText = $"Login User: {session.UserName}\t\t Role: {role}";
    }

    // 当选中楼层改变时触发
    partial void OnSelectedFloorChanged(string value)
    {
        LoadTreeAndConfigs();
    }

    private void LoadTreeAndConfigs()   
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var treeDataList = _reelUpDao.GetIpInfo(SelectedFloor);

            var allConfigs = _reelUpDao.GetReelupConfigInfo();
            var floorInfo = Floors.Find(SelectedFloor);

            if (floorInfo != null)
            {
                string prefixOld = floorInfo.OldPattern.Replace("%", "");
                string prefixNew = floorInfo.NewPattern.Replace("%", "");
                allConfigs = allConfigs.Where(x =>
                    x.Line.StartsWith(prefixOld, StringComparison.OrdinalIgnoreCase) ||
                    x.Line.StartsWith(prefixNew, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ReelUpConfigs.Clear();
            foreach (var item in allConfigs) ReelUpConfigs.Add(item);

            // IO 配置不按楼层过滤，直接全量显示
            var ioList = _ioDao.GetIoConfigInfo();
            IoConfigs.Clear();
            foreach (var item in ioList) IoConfigs.Add(item);

            BuildTree(treeDataList);
        });
    }

    private void BuildTree(List<IDInfo> list)
    {
        NavigationTree.Clear();

        var lineGroups = list.GroupBy(x => x.Line).OrderBy(x => x.Key);

        foreach (var lg in lineGroups)
        {
            var lineNode = new TreeNode { Name = lg.Key, IsExpanded = true };

            foreach (var terminal in lg)
            {
                lineNode.Children.Add(new TreeNode
                {
                    Name = $"[{terminal.Id}] {terminal.Ip} ({terminal.GunType})",
                    Tag = terminal,
                    State = DeviceNodeState.NotStarted
                });
            }
            NavigationTree.Add(lineNode);
        }
    }

    // 接收到底层 TCP 状态变化
    private void OnConnectionStateChanged(string ip, bool isConnected)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var lineNode in NavigationTree)
            {
                var targetNode = lineNode.Children.FirstOrDefault(n => (n.Tag as IDInfo)?.Ip == ip);
                if (targetNode != null)
                {
                    targetNode.State = isConnected ? DeviceNodeState.Connected : DeviceNodeState.Disconnected;
                    break;
                }
            }
        });
    }

    private void OnDataReceived(ReceiveBean bean)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // 简单的分流逻辑
            if (bean.StrData.Length > 15 || bean.Feedback.Contains("SN"))
            {
                IoLogs.Insert(0, bean);
                if (IoLogs.Count > 100) IoLogs.RemoveAt(100);
            }
            else
            {
                ReelUpLogs.Insert(0, bean);
                if (ReelUpLogs.Count > 100) ReelUpLogs.RemoveAt(100);
            }
            DeviceStatusText = $"最近通讯: {DateTime.Now:HH:mm:ss}";
        });
    }

    // 双击节点开关
    [RelayCommand]
    private void ToggleNodeConnection(TreeNode node)
    {
        if (node.Tag is not IDInfo info) return;

        if (node.State == DeviceNodeState.NotStarted)
        {
            // 红灯 -> 启动（变灰灯，等待后台连上）
            node.State = DeviceNodeState.Disconnected;
            _commManager.StartOutTask(info.Ip, info.GunType, SelectedFloor);
        }
        else
        {
            // 绿灯或灰灯 -> 停止（变红灯）
            _commManager.StopOutTask(info.Ip);
            node.State = DeviceNodeState.NotStarted;
        }
    }

    // 一键全连
    [RelayCommand]
    private void ConnectAll()
    {
        foreach (var lineNode in NavigationTree)
        {
            foreach (var terminalNode in lineNode.Children)
            {
                if (terminalNode.State == DeviceNodeState.NotStarted)
                {
                    ToggleNodeConnection(terminalNode);
                }
            }
        }
    }

    // 一键全断
    [RelayCommand]
    private void DisconnectAll()
    {
        _commManager.StopAllCommunication();

        foreach (var lineNode in NavigationTree)
        {
            foreach (var terminalNode in lineNode.Children)
            {
                terminalNode.State = DeviceNodeState.NotStarted;
            }
        }
        DeviceStatusText = "全部断开";
    }

    public void Dispose()
    {
        _commManager?.Dispose();
    }

    [RelayCommand]
    private void AddTerminal()
    {
        var win = new AddTerminalWindow();
        if (Application.Current.MainWindow != null)
        {
            win.Owner = Application.Current.MainWindow;
        }

        if (win.ShowDialog() == true)
        {
            LoadTreeAndConfigs();
        }
    }
}