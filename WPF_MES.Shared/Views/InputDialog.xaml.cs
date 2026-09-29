using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WPF_MES.Shared.Models;

namespace WPF_MES.Shared.Views;

public partial class InputDialog : Window
{
    // ============ 属性 ============

    /// <summary>窗口标题</summary>
    public string DialogTitle { get; set; } = "输入";

    /// <summary>整体提示（可为空）</summary>
    public string DialogPrompt { get; set; } = string.Empty;

    /// <summary>输入字段集合</summary>
    public ObservableCollection<InputField> Fields { get; }

    /// <summary>提示是否可见</summary>
    public Visibility PromptVisibility =>
        string.IsNullOrEmpty(DialogPrompt) ? Visibility.Collapsed : Visibility.Visible;

    // ============ 构造（多字段） ============

    /// <summary>
    /// 多字段输入对话框
    /// </summary>
    /// <param name="title">窗口标题</param>
    /// <param name="dialogPrompt">整体提示（可选）</param>
    /// <param name="fields">字段列表</param>
    public InputDialog(string title, string dialogPrompt, IEnumerable<InputField> fields)
    {
        InitializeComponent();

        DialogTitle = title;
        DialogPrompt = dialogPrompt;
        Fields = new ObservableCollection<InputField>(fields);

        DataContext = this;

        Loaded += OnLoaded;
    }

    /// <summary>
    /// 多字段输入对话框（无整体提示）
    /// </summary>
    public InputDialog(string title, IEnumerable<InputField> fields)
        : this(title, string.Empty, fields) { }

    /// <summary>
    /// 多字段输入对话框（params 形式）
    /// </summary>
    public InputDialog(string title, params InputField[] fields)
        : this(title, string.Empty, fields) { }

    // ============ 构造（单字段便捷） ============

    /// <summary>
    /// 单字段输入对话框
    /// </summary>
    /// <param name="title">窗口标题</param>
    /// <param name="label">输入框标签</param>
    /// <param name="defaultValue">默认值</param>
    public InputDialog(string title, string label, string defaultValue = "")
        : this(title, string.Empty,
              new[] { new InputField("value", label, defaultValue) })
    { }

    // ============ 加载后聚焦 ============

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 找到第一个输入框并聚焦
        var textBox = FindFirstTextBox(this);
        if (textBox != null)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    /// <summary>
    /// 在可视树里找第一个 TextBox
    /// </summary>
    private static System.Windows.Controls.TextBox? FindFirstTextBox(DependencyObject parent)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is System.Windows.Controls.TextBox tb) return tb;

            var found = FindFirstTextBox(child);
            if (found != null) return found;
        }
        return null;
    }

    // ============ 取值 ============

    /// <summary>
    /// 获取所有字段的值（按 Key）
    /// </summary>
    public Dictionary<string, string> GetValues()
    {
        return Fields.ToDictionary(f => f.Key, f => f.Value);
    }

    /// <summary>
    /// 按 Key 获取单个值。不存在返回空字符串。
    /// </summary>
    public string GetValue(string key)
    {
        return Fields.FirstOrDefault(f => f.Key == key)?.Value ?? string.Empty;
    }

    /// <summary>
    /// 按顺序获取所有值
    /// </summary>
    public List<string> GetValueList()
    {
        return Fields.Select(f => f.Value).ToList();
    }

    /// <summary>
    /// 判断所有字段是否都非空
    /// </summary>
    public bool AllNonEmpty()
    {
        return Fields.All(f => !string.IsNullOrWhiteSpace(f.Value));
    }

    // ============ 按钮 ============

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}