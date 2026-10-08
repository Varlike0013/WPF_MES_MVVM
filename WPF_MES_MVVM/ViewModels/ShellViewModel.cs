using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using WPF_MES.Contracts;
using WPF_MES.Shared;
using WPF_MES_MVVM.Services;

namespace WPF_MES_MVVM.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IModule _module;
    private readonly LoginResult _loginResult;

    // 页面缓存：key = 标题，value = 已创建的用户控件
    private readonly Dictionary<string, System.Windows.Controls.UserControl> _pageCache = new();

    // 当前打开的菜单项（用于判断是否已收藏）
    private ModuleMenuNode? _currentMenuItem;

    // 已收藏的菜单项
    private readonly List<ModuleMenuNode> _favoriteItems = new();

    private const string FavoriteGroupTitle = "⭐ 我的收藏";
    private readonly JsonConfig<Models.FavoritesData> _favConfig;

    // ============ 菜单 & 页签 ============

    public ObservableCollection<MenuNodeViewModel> MenuNodes { get; } = new();
    public ObservableCollection<TabItemViewModel> Tabs { get; } = new();

    // ============ 顶部 ============

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _userDisplay = string.Empty;
    [ObservableProperty] private string _timeDisplay = string.Empty;

    // ============ 内容区 ============

    [ObservableProperty] private string _welcomeText = "欢迎使用 MES 系统";
    [ObservableProperty] private System.Windows.Controls.UserControl? _currentPage;
    [ObservableProperty] private bool _isWelcomeVisible = true;

    // ============ 搜索 ============

    [ObservableProperty] private string _searchText = string.Empty;

    // ============ 收藏状态（给 ☆/★ 图标用） ============

    [ObservableProperty] private bool _isCurrentFavorite;

    /// <summary>注销事件</summary>
    public event Action? LogoutRequested;

    public ShellViewModel(IModule module, LoginResult loginResult)
    {
        _module = module;
        _loginResult = loginResult;

        Title = module.DisplayName;
        UserDisplay = $"{loginResult.UserName} ({loginResult.UserNo})";
        WelcomeText = $"欢迎使用 {module.DisplayName}";

        // 加载收藏配置
        _favConfig = new JsonConfig<Models.FavoritesData>("favorites.json");
        LoadFavoritesFromConfig();

        BuildMenu();
        StartClock();
    }

    #region 菜单构建

    private void BuildMenu()
    {
        MenuNodes.Clear();

        // 1. 先建收藏分组（如果有收藏）
        RebuildFavorites();

        // 2. 模块菜单
        var nodes = _module.GetMenuItems().ToList();
        // 主程序统一追加全局项  ← 关键
        GlobalMenuProvider.AttachTo(nodes);
        foreach (var node in nodes)
        {
            var vm = CreateMenuNode(node);
            if (vm != null)
                MenuNodes.Add(vm);
        }
    }

    /// <summary>
    /// 重建收藏分组。放在 MenuNodes 最前面。
    /// </summary>
    private void RebuildFavorites()
    {
        // 移除已有的收藏分组
        var old = MenuNodes.FirstOrDefault(n => n.Title == FavoriteGroupTitle);
        if (old != null)
            MenuNodes.Remove(old);

        if (_favoriteItems.Count == 0) return;

        var favGroup = new MenuNodeViewModel(FavoriteGroupTitle, true);
        foreach (var item in _favoriteItems)
        {
            var captured = item;
            var nodeVm = new MenuNodeViewModel(item.Title, false)
            {
                OpenCommand = new RelayCommand(() => OpenPage(captured))
            };
            favGroup.Children.Add(nodeVm);
        }

        MenuNodes.Insert(0, favGroup);
    }

    private MenuNodeViewModel? CreateMenuNode(ModuleMenuNode node)
    {
        bool isGroup = node.Children.Count > 0;
        var vm = new MenuNodeViewModel(node.Title, isGroup);

        if (!isGroup)
        {
            var captured = node;
            vm.OpenCommand = new RelayCommand(() => OpenPage(captured));
        }
        else
        {
            foreach (var child in node.Children)
            {
                var childVm = CreateMenuNode(child);
                if (childVm != null)
                    vm.Children.Add(childVm);
            }
        }

        return vm;
    }

    #endregion

    #region 我的收藏
    /// <summary>
    /// 从配置文件加载收藏
    /// </summary>
    private void LoadFavoritesFromConfig()
    {
        _favoriteItems.Clear();

        foreach (var title in _favConfig.Data.Titles)
        {
            var node = FindMenuNode(title);
            if (node != null)
                _favoriteItems.Add(node);
        }
    }

    /// <summary>
    /// 把当前收藏保存到配置文件
    /// </summary>
    private void SaveFavoritesToConfig()
    {
        _favConfig.Data.Titles = _favoriteItems.Select(f => f.Title).ToList();
        _favConfig.Save();   // 内部会记日志、抛异常
    }
    #endregion

    #region 页面切换

    private void OpenPage(ModuleMenuNode item)
    {
        if (item.ViewFactory == null)
        {
            MessageBox.Show($"功能 [{item.Title}] 尚未实现。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 记录当前项，更新收藏状态
        _currentMenuItem = item;
        IsCurrentFavorite = _favoriteItems.Any(f => f.Title == item.Title);

        // 已缓存 → 切换
        if (_pageCache.TryGetValue(item.Title, out var cached))
        {
            ShowPage(item.Title, cached);
            return;
        }

        // 新建
        try
        {
            var page = item.ViewFactory();
            _pageCache[item.Title] = page;
            AddTab(item.Title);
            ShowPage(item.Title, page);
        }
        catch (Exception ex)
        {
            Logger.Error($"[SHELL] Open page failed: {item.Title}, {ex.Message}");
            MessageBox.Show($"打开页面 [{item.Title}] 失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowPage(string title, System.Windows.Controls.UserControl page)
    {
        IsWelcomeVisible = false;
        CurrentPage = page;

        foreach (var tab in Tabs)
            tab.IsActive = tab.Title == title;
    }

    private void AddTab(string title)
    {
        if (Tabs.Any(t => t.Title == title)) return;

        var tab = new TabItemViewModel(title);
        tab.CloseRequested += () => ClosePage(title);
        tab.SelectRequested += () => ActivateTab(title);
        Tabs.Add(tab);
    }

    private void ActivateTab(string title)
    {
        if (!_pageCache.TryGetValue(title, out var page)) return;
        ShowPage(title, page);

        // 同步当前菜单项和收藏状态
        var item = FindMenuNode(title);
        if (item != null)
        {
            _currentMenuItem = item;
            IsCurrentFavorite = _favoriteItems.Any(f => f.Title == item.Title);
        }
    }

    private void ClosePage(string title)
    {
        if (_pageCache.TryGetValue(title, out var page))
            _pageCache.Remove(title);

        var tab = Tabs.FirstOrDefault(t => t.Title == title);
        if (tab != null) Tabs.Remove(tab);

        if (CurrentPage == page)
        {
            var next = Tabs.FirstOrDefault();
            if (next != null)
            {
                ActivateTab(next.Title);
            }
            else
            {
                // 没有页签了，回到欢迎页
                CurrentPage = null;
                IsWelcomeVisible = true;
                WelcomeText = $"欢迎使用 {_module.DisplayName}";
                _currentMenuItem = null;
                IsCurrentFavorite = false;
            }
        }
    }

    private ModuleMenuNode? FindMenuNode(string title)
    {
        foreach (var node in _module.GetMenuItems())
        {
            var found = FindMenuNodeRecursive(node, title);
            if (found != null) return found;
        }
        return null;
    }

    private ModuleMenuNode? FindMenuNodeRecursive(ModuleMenuNode node, string title)
    {
        if (node.Title == title && node.ViewFactory != null) return node;
        foreach (var child in node.Children)
        {
            var found = FindMenuNodeRecursive(child, title);
            if (found != null) return found;
        }
        return null;
    }

    #endregion

    #region 命令：首页 / 收藏 / 展开折叠 / 注销

    /// <summary>回到首页（关闭所有页签）</summary>
    [RelayCommand]
    private void GoHome()
    {
        Tabs.Clear();
        _pageCache.Clear();
        CurrentPage = null;
        IsWelcomeVisible = true;
        WelcomeText = $"欢迎使用 {_module.DisplayName}";
        _currentMenuItem = null;
        IsCurrentFavorite = false;
    }

    /// <summary>收藏 / 取消收藏当前页面</summary>
    [RelayCommand]
    private void ToggleFavorite()
    {
        if (_currentMenuItem == null)
        {
            MessageBox.Show("请先打开一个页面。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var existing = _favoriteItems.FirstOrDefault(f => f.Title == _currentMenuItem.Title);
        if (existing != null)
        {
            _favoriteItems.Remove(existing);
            IsCurrentFavorite = false;
        }
        else
        {
            _favoriteItems.Add(_currentMenuItem);
            IsCurrentFavorite = true;
        }

        RebuildFavorites();
        SaveFavoritesToConfig();   // ← 新增
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var node in MenuNodes)
            CollapseNode(node);
    }

    [RelayCommand]
    private void ExpandAll()
    {
        foreach (var node in MenuNodes)
            ExpandNode(node);
    }

    private void CollapseNode(MenuNodeViewModel node)
    {
        if (node.Children.Count > 0)
        {
            node.IsExpanded = false;
            foreach (var child in node.Children)
                CollapseNode(child);
        }
    }

    private void ExpandNode(MenuNodeViewModel node)
    {
        if (node.Children.Count > 0)
        {
            node.IsExpanded = true;
            foreach (var child in node.Children)
                ExpandNode(child);
        }
    }

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke();

    #endregion

    #region 搜索过滤

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter(value);
    }

    private void ApplyFilter(string keyword)
    {
        keyword = keyword?.Trim() ?? string.Empty;
        foreach (var node in MenuNodes)
            FilterNode(node, keyword, false);
    }

    private bool FilterNode(MenuNodeViewModel node, string keyword, bool parentMatch)
    {
        if (keyword.Length == 0)
        {
            node.IsVisible = true;
            foreach (var child in node.Children)
                FilterNode(child, keyword, false);
            return true;
        }

        bool selfMatch = parentMatch ||
            node.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase);

        bool anyChildVisible = false;
        foreach (var child in node.Children)
        {
            if (FilterNode(child, keyword, selfMatch))
                anyChildVisible = true;
        }

        bool visible = selfMatch || anyChildVisible;
        node.IsVisible = visible;

        if (visible && node.Children.Count > 0)
            node.IsExpanded = true;

        return visible;
    }

    #endregion

    #region 时钟

    private void StartClock()
    {
        UpdateTime();
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (_, _) => UpdateTime();
        timer.Start();
    }

    private void UpdateTime()
    {
        TimeDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    #endregion
}