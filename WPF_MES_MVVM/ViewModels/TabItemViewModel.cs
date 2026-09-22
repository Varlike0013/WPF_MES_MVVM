using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WPF_MES_MVVM.ViewModels;

/// <summary>
/// 页签 ViewModel
/// </summary>
public partial class TabItemViewModel : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private bool _isActive;

    /// <summary>点关闭按钮时触发</summary>
    public event Action? CloseRequested;

    /// <summary>点页签本身时触发</summary>
    public event Action? SelectRequested;

    public IRelayCommand CloseCommand { get; }
    public IRelayCommand SelectCommand { get; }

    public TabItemViewModel(string title)
    {
        _title = title;
        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());
        SelectCommand = new RelayCommand(() => SelectRequested?.Invoke());
    }
}