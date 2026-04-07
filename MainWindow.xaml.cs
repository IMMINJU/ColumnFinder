using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ColumnFinder.Services;
using ColumnFinder.Views;

namespace ColumnFinder;

public partial class MainWindow : Window
{
    private const string StartPath = @"C:\Users\minju";
    private readonly Stack<string> _back = new();
    private readonly Stack<string> _forward = new();
    private string _current = "";
    private bool _showHidden = false;
    private MillerColumn.SortMode _sort = MillerColumn.SortMode.ModifiedDesc;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BuildSidebar();
            AddressBar.Navigated += p => Navigate(p);
            Navigate(StartPath, recordHistory: false);
        };
    }

    // ---------- 사이드바 ----------
    private void BuildSidebar()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var entries = new List<SidebarEntry>
        {
            new("홈", home),
            new("바탕 화면", Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
            new("다운로드", Path.Combine(home, "Downloads")),
        };

        foreach (var e in entries)
            e.Icon = ShellIcon.Get(e.Path, true);

        Sidebar.ItemsSource = entries;
    }

    private void Sidebar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Sidebar.SelectedItem is SidebarEntry s && Directory.Exists(s.Path))
            Navigate(s.Path);
    }

    // ---------- 네비 ----------
    private void Navigate(string path, bool recordHistory = true)
    {
        if (!Directory.Exists(path)) return;
        if (recordHistory && _current != "")
        {
            _back.Push(_current);
            _forward.Clear();
        }
        _current = path;
        ColumnsHost.Children.Clear();
        ColumnsHost.ColumnDefinitions.Clear();
        AddressBar.SetPath(path);
        PushColumn(path);
        UpdateStatus();
    }

    private void PushColumn(string path)
    {
        var col = new MillerColumn { ShowHidden = _showHidden, Sort = _sort };
        col.Load(path);
        col.FolderSelected += OnFolderSelected;
        col.SelectionChanged += OnItemSelected;
        col.Changed += () =>
        {
            foreach (var c in ColumnsHost.Children.OfType<MillerColumn>()) c.Reload();
            UpdateStatus();
        };
        int idx = ColumnsHost.ColumnDefinitions.Count;
        // 기존 컬럼 사이에 splitter가 들어가면 idx를 짝수로 (0,2,4,...)
        ColumnsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 120 });
        Grid.SetColumn(col, idx);
        ColumnsHost.Children.Add(col);

        // 컬럼 사이 스플리터
        if (idx > 0)
        {
            var splitter = new GridSplitter
            {
                Width = 4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = System.Windows.Media.Brushes.Transparent,
                ResizeBehavior = GridResizeBehavior.PreviousAndCurrent,
            };
            Grid.SetColumn(splitter, idx);
            ColumnsHost.Children.Add(splitter);
        }

        UpdateStatus(col);
    }

    private MillerColumn? ActiveColumn =>
        ColumnsHost.Children.OfType<MillerColumn>().LastOrDefault();

    private void OnItemSelected(string path)
    {
        // 폴더면 OnFolderSelected가 따로 처리하므로 무시
        if (Directory.Exists(path)) { RemovePreviewColumn(); return; }
        ShowPreviewColumn(path);
    }

    private void RemovePreviewColumn()
    {
        var preview = ColumnsHost.Children.OfType<PreviewPane>().LastOrDefault();
        if (preview is null) return;
        int col = Grid.GetColumn(preview);
        ColumnsHost.Children.Remove(preview);
        // 마지막 컬럼 정의도 제거 (preview용으로 추가했던 것)
        if (ColumnsHost.ColumnDefinitions.Count > col)
            ColumnsHost.ColumnDefinitions.RemoveAt(ColumnsHost.ColumnDefinitions.Count - 1);
    }

    private void ShowPreviewColumn(string filePath)
    {
        var existing = ColumnsHost.Children.OfType<PreviewPane>().LastOrDefault();
        if (existing != null)
        {
            existing.Show(filePath);
            return;
        }
        var pane = new PreviewPane();
        pane.Show(filePath);
        int idx = ColumnsHost.ColumnDefinitions.Count;
        ColumnsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star), MinWidth = 200 });
        Grid.SetColumn(pane, idx);
        ColumnsHost.Children.Add(pane);
    }

    private void OnFolderSelected(string path)
    {
        RemovePreviewColumn();
        var parent = Path.GetDirectoryName(path);
        var existing = ColumnsHost.Children.OfType<MillerColumn>()
            .FirstOrDefault(c => string.Equals(c.CurrentPath, parent, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            int idx = Grid.GetColumn(existing);
            var toRemove = ColumnsHost.Children.OfType<UIElement>()
                .Where(c => Grid.GetColumn(c) > idx).ToList();
            foreach (var c in toRemove) ColumnsHost.Children.Remove(c);
            while (ColumnsHost.ColumnDefinitions.Count > idx + 1)
                ColumnsHost.ColumnDefinitions.RemoveAt(ColumnsHost.ColumnDefinitions.Count - 1);
        }
        PushColumn(path);
        AddressBar.SetPath(path);
        _current = path;
    }

    private void UpdateStatus(MillerColumn? col = null)
    {
        col ??= ColumnsHost.Children.OfType<MillerColumn>().LastOrDefault();
        StatusBar.Text = col is null ? "0개 항목" : $"{col.ItemCount}개 항목";
    }

    // ---------- 툴바/네비 핸들러 ----------
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_back.Count == 0) return;
        _forward.Push(_current);
        var prev = _back.Pop();
        Navigate(prev, recordHistory: false);
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (_forward.Count == 0) return;
        _back.Push(_current);
        var next = _forward.Pop();
        Navigate(next, recordHistory: false);
    }

    private void Up_Click(object sender, RoutedEventArgs e)
    {
        var parent = Path.GetDirectoryName(_current);
        if (!string.IsNullOrEmpty(parent)) Navigate(parent);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Navigate(_current, recordHistory: false);

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ActiveColumn is null) return;
        ActiveColumn.Filter = SearchBox.Text;
        ActiveColumn.ApplyFilter();
        UpdateStatus();
    }

    private void NewFolder_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveColumn is null) return;
        FileOps.CreateNewFolder(ActiveColumn.CurrentPath);
        ActiveColumn.Reload();
        UpdateStatus();
    }
    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveColumn is { } c) FileOps.CopyToClipboard(c.SelectedPaths, true);
    }
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveColumn is { } c) FileOps.CopyToClipboard(c.SelectedPaths, false);
    }
    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveColumn is null) return;
        FileOps.Paste(ActiveColumn.CurrentPath);
        ActiveColumn.Reload();
        UpdateStatus();
    }
    private void Rename_Click(object sender, RoutedEventArgs e) => ActiveColumn?.Rename();
    private void Delete_Click(object sender, RoutedEventArgs e) => ActiveColumn?.Delete();

    private void Sort_Click(object sender, RoutedEventArgs e)
    {
        SortPopup.PlacementTarget = (UIElement)sender;
        SortPopup.IsOpen = true;
    }

    private void SortItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string tag &&
            Enum.TryParse<MillerColumn.SortMode>(tag, out var mode))
        {
            _sort = mode;
            foreach (var c in ColumnsHost.Children.OfType<MillerColumn>())
            {
                c.Sort = mode;
                c.Reload();
            }
            SortPopup.IsOpen = false;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var ctrl = e.KeyboardDevice.Modifiers == ModifierKeys.Control;
        if (ctrl && e.Key == Key.L) { AddressBar.BeginEdit(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.F) { SearchBox.Focus(); SearchBox.SelectAll(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.H) { ToggleHidden(); e.Handled = true; return; }

        // 컬럼 간 ←→ 이동 (검색박스/주소창 포커스 중엔 무시)
        if (!(Keyboard.FocusedElement is TextBox))
        {
            if (e.Key == Key.Left) { MoveColumn(-1); e.Handled = true; return; }
            if (e.Key == Key.Right) { MoveColumn(+1); e.Handled = true; return; }
        }
        base.OnPreviewKeyDown(e);
    }

    private void MoveColumn(int dir)
    {
        var cols = ColumnsHost.Children.OfType<MillerColumn>().OrderBy(Grid.GetColumn).ToList();
        if (cols.Count == 0) return;
        var focused = cols.FirstOrDefault(c => c.IsKeyboardFocusWithin) ?? cols.Last();
        int idx = cols.IndexOf(focused) + dir;
        if (idx < 0 || idx >= cols.Count) return;
        cols[idx].FocusList();
    }

    private void ToggleHidden()
    {
        _showHidden = !_showHidden;
        foreach (var c in ColumnsHost.Children.OfType<MillerColumn>())
        {
            c.ShowHidden = _showHidden;
            c.Reload();
        }
        UpdateStatus();
    }

    private class SidebarEntry
    {
        public string Name { get; }
        public string Path { get; }
        public BitmapSource? Icon { get; set; }
        public SidebarEntry(string name, string path) { Name = name; Path = path; }
    }
}
