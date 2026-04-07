using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ColumnFinder.Services;

namespace ColumnFinder.Views;

public partial class MillerColumn : UserControl
{
    public event Action<string>? FolderSelected;
    public event Action<string>? FileActivated;
    public event Action<string>? SelectionChanged; // 단일 선택 경로 (파일/폴더)
    public event Action? Changed; // 파일 작업 후 갱신 알림

    public string CurrentPath { get; private set; } = "";
    public string Filter { get; set; } = "";
    public bool ShowHidden { get; set; } = false;
    public SortMode Sort { get; set; } = SortMode.ModifiedDesc;
    private List<Row> _all = new();

    public enum SortMode { ModifiedDesc, ModifiedAsc, NameAsc, NameDesc, SizeDesc, SizeAsc, TypeAsc }

    public MillerColumn()
    {
        InitializeComponent();
    }

    public void Load(string path)
    {
        CurrentPath = path;
        Reload();
    }

    public void Reload()
    {
        try
        {
            var dir = new DirectoryInfo(CurrentPath);
            var infos = dir.EnumerateFileSystemInfos()
                .Where(i => ShowHidden || (i.Attributes & FileAttributes.Hidden) == 0);
            infos = Sort switch
            {
                SortMode.ModifiedDesc => infos.OrderByDescending(i => i.LastWriteTime),
                SortMode.ModifiedAsc => infos.OrderBy(i => i.LastWriteTime),
                SortMode.NameAsc => infos.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase),
                SortMode.NameDesc => infos.OrderByDescending(i => i.Name, StringComparer.OrdinalIgnoreCase),
                SortMode.SizeDesc => infos.OrderByDescending(i => i is FileInfo fi ? fi.Length : -1),
                SortMode.SizeAsc => infos.OrderBy(i => i is FileInfo fi ? fi.Length : -1),
                SortMode.TypeAsc => infos.OrderBy(i => i is DirectoryInfo ? "" : Path.GetExtension(i.Name), StringComparer.OrdinalIgnoreCase)
                                         .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase),
                _ => infos,
            };
            _all = infos.Select(i => new Row
            {
                Name = i.Name,
                FullPath = i.FullName,
                IsDirectory = i is DirectoryInfo,
                Icon = ShellIcon.Get(i.FullName, i is DirectoryInfo),
            }).ToList();
            ApplyFilter();
        }
        catch
        {
            _all = new();
            List.ItemsSource = null;
        }
    }

    public void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(Filter))
            List.ItemsSource = _all;
        else
            List.ItemsSource = _all
                .Where(r => r.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    public string[] SelectedPaths =>
        List.SelectedItems.Cast<Row>().Select(r => r.FullPath).ToArray();

    private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (List.SelectedItems.Count != 1) return;
        if (List.SelectedItem is not Row sel) return;
        SelectionChanged?.Invoke(sel.FullPath);
        if (sel.IsDirectory)
            FolderSelected?.Invoke(sel.FullPath);
    }

    private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (List.SelectedItem is not Row sel) return;
        if (!sel.IsDirectory)
        {
            FileActivated?.Invoke(sel.FullPath);
            try { Process.Start(new ProcessStartInfo(sel.FullPath) { UseShellExecute = true }); }
            catch { }
        }
    }

    private void List_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete) { Delete(); e.Handled = true; }
        else if (e.Key == Key.F2) { Rename(); e.Handled = true; }
        else if (e.Key == Key.Enter)
        {
            if (List.SelectedItem is Row r)
            {
                if (r.IsDirectory) FolderSelected?.Invoke(r.FullPath);
                else { try { Process.Start(new ProcessStartInfo(r.FullPath) { UseShellExecute = true }); } catch { } }
            }
            e.Handled = true;
        }
        else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.C) { FileOps.CopyToClipboard(SelectedPaths, false); e.Handled = true; }
            else if (e.Key == Key.X) { FileOps.CopyToClipboard(SelectedPaths, true); e.Handled = true; }
            else if (e.Key == Key.V) { FileOps.Paste(CurrentPath); Changed?.Invoke(); e.Handled = true; }
        }
    }

    public void Delete()
    {
        var sel = SelectedPaths;
        if (sel.Length == 0) return;
        FileOps.DeleteToRecycleBin(sel);
        Changed?.Invoke();
    }

    public void Rename()
    {
        if (List.SelectedItem is not Row r) return;
        var dlg = new RenameDialog(r.Name) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.NewName) && dlg.NewName != r.Name)
        {
            FileOps.Rename(r.FullPath, dlg.NewName);
            Changed?.Invoke();
        }
    }

    // ---------- 드래그 앤 드롭 ----------
    private Point _dragStart;
    private bool _dragging;

    private void List_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _dragging = false;
    }

    private void List_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging || e.LeftButton != MouseButtonState.Pressed) return;
        var diff = _dragStart - e.GetPosition(null);
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var paths = SelectedPaths;
        if (paths.Length == 0) return;

        _dragging = true;
        var data = new DataObject(DataFormats.FileDrop, paths);
        DragDrop.DoDragDrop(List, data, DragDropEffects.Copy | DragDropEffects.Move);
        _dragging = false;
    }

    private void List_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? ((e.KeyStates & DragDropKeyStates.ControlKey) != 0 ? DragDropEffects.Copy : DragDropEffects.Move)
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void List_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        bool move = (e.KeyStates & DragDropKeyStates.ControlKey) == 0;
        FileOps.DropFiles(paths, CurrentPath, move);
        Changed?.Invoke();
    }

    // 자체 ContextMenu 사용 (XAML 정의). 별도 핸들러 불필요.
    private void List_MouseRightButtonUp(object sender, MouseButtonEventArgs e) { }

    private void Ctx_OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        var sel = SelectedPaths;
        try
        {
            if (sel.Length > 0)
                Process.Start("explorer.exe", $"/select,\"{sel[0]}\"");
            else
                Process.Start("explorer.exe", $"\"{CurrentPath}\"");
        }
        catch { }
    }

    private void Ctx_Open_Click(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is Row r)
        {
            if (r.IsDirectory) FolderSelected?.Invoke(r.FullPath);
            else { try { Process.Start(new ProcessStartInfo(r.FullPath) { UseShellExecute = true }); } catch { } }
        }
    }
    private void Ctx_Cut_Click(object sender, RoutedEventArgs e) => FileOps.CopyToClipboard(SelectedPaths, true);
    private void Ctx_Copy_Click(object sender, RoutedEventArgs e) => FileOps.CopyToClipboard(SelectedPaths, false);
    private void Ctx_Paste_Click(object sender, RoutedEventArgs e) { FileOps.Paste(CurrentPath); Changed?.Invoke(); }
    private void Ctx_Rename_Click(object sender, RoutedEventArgs e) => Rename();
    private void Ctx_Delete_Click(object sender, RoutedEventArgs e) => Delete();

    public int ItemCount => (List.ItemsSource as System.Collections.IList)?.Count ?? 0;

    public void FocusList() { List.Focus(); if (List.Items.Count > 0 && List.SelectedIndex < 0) List.SelectedIndex = 0; }

    private class Row
    {
        public string Name { get; init; } = "";
        public string FullPath { get; init; } = "";
        public bool IsDirectory { get; init; }
        public BitmapSource? Icon { get; init; }
    }
}
