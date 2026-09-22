using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Drawing;

namespace WPF_MES_MVVM.ViewModels
{
    public partial class MenuNodeViewModel : ObservableObject
    {
        [ObservableProperty] private string _title;
        [ObservableProperty] private bool _isExpanded = true;
        [ObservableProperty] private bool _isGroupVisible;
        [ObservableProperty] private bool _isLeafVisible;
        [ObservableProperty] private bool _isVisible = true;

        /// <summary>分组图标：展开 ▼ / 折叠 ▶ / 叶子显示为空</summary>
        [ObservableProperty] private string _icon = string.Empty;

        public ObservableCollection<MenuNodeViewModel> Children { get; } = new();
        public IRelayCommand? OpenCommand { get; set; }
        public IRelayCommand ToggleCommand { get; }

        public MenuNodeViewModel(string title, bool isGroup)
        {
            _title = title;
            IsGroupVisible = isGroup;
            IsLeafVisible = !isGroup;
            ToggleCommand = new RelayCommand(() => IsExpanded = !IsExpanded);

            // 初始图标
            if (isGroup) UpdateIcon();
        }

        /// <summary>
        /// 当展开状态变化时更新图标
        /// </summary>
        partial void OnIsExpandedChanged(bool value)
        {
            if (IsGroupVisible) UpdateIcon();
        }

        private void UpdateIcon()
        {
            Icon = IsExpanded ? "▼" : "▶";
        }
    }
}