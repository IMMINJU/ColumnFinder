using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ColumnFinder.Views;

public partial class Breadcrumb : UserControl
{
    public event Action<string>? Navigated;

    public Breadcrumb()
    {
        InitializeComponent();
    }

    private void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 빈 영역(브레드크럼 버튼/TextBox가 아닌 곳) 클릭 시 편집모드
        var src = e.OriginalSource as DependencyObject;
        while (src != null)
        {
            if (src is Button || src is TextBox) return;
            src = System.Windows.Media.VisualTreeHelper.GetParent(src);
        }
        BeginEdit();
    }

    public void SetPath(string path)
    {
        var crumbs = new List<Crumb>();
        var parts = new List<(string name, string full)>();
        var di = new DirectoryInfo(path);
        while (di != null)
        {
            parts.Add((di.Name.TrimEnd('\\'), di.FullName));
            di = di.Parent;
        }
        parts.Reverse();
        for (int i = 0; i < parts.Count; i++)
        {
            crumbs.Add(new Crumb
            {
                Name = parts[i].name,
                Path = parts[i].full,
                SeparatorVisibility = i == parts.Count - 1 ? Visibility.Collapsed : Visibility.Visible,
            });
        }
        Items.ItemsSource = crumbs;
        Editor.Text = path;
    }

    private void Crumb_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string p) Navigated?.Invoke(p);
    }

    public void BeginEdit()
    {
        Items.Visibility = Visibility.Collapsed;
        Editor.Visibility = Visibility.Visible;
        Editor.Focus();
        Editor.SelectAll();
    }

    private void EndEdit(bool commit)
    {
        Editor.Visibility = Visibility.Collapsed;
        Items.Visibility = Visibility.Visible;
        if (commit && Directory.Exists(Editor.Text))
            Navigated?.Invoke(Editor.Text);
    }

    private void Editor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { EndEdit(true); e.Handled = true; }
        else if (e.Key == Key.Escape) { EndEdit(false); e.Handled = true; }
    }

    private void Editor_LostFocus(object sender, RoutedEventArgs e) => EndEdit(false);

    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        BeginEdit();
        base.OnMouseDoubleClick(e);
    }

    private class Crumb
    {
        public string Name { get; init; } = "";
        public string Path { get; init; } = "";
        public Visibility SeparatorVisibility { get; init; }
    }
}
