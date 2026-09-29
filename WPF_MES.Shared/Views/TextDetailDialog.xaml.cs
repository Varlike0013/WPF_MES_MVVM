using System.Windows;

namespace WPF_MES.Shared.Views;

public partial class TextDetailDialog : Window
{
    public string DialogTitle { get; set; } = "详细内容";

    public TextDetailDialog(string title, string content)
    {
        InitializeComponent();

        DialogTitle = title;
        Title = title;
        txtContent.Text = content;

        DataContext = this;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}