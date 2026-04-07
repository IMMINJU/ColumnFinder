using System.IO;
using System.Windows;

namespace ColumnFinder.Views;

public partial class RenameDialog : Window
{
    public string NewName => NameBox.Text.Trim();

    public RenameDialog(string current)
    {
        InitializeComponent();
        NameBox.Text = current;
        Loaded += (_, _) =>
        {
            NameBox.Focus();
            // 확장자 제외하고 선택
            var stem = Path.GetFileNameWithoutExtension(current);
            NameBox.Select(0, stem.Length);
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
