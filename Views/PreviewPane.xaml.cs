using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColumnFinder.Services;

namespace ColumnFinder.Views;

public partial class PreviewPane : UserControl
{
    private static readonly string[] ImageExts = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico" };
    private static readonly string[] TextExts = {
        ".txt", ".md", ".log", ".json", ".xml", ".yml", ".yaml", ".csv",
        ".cs", ".js", ".ts", ".py", ".java", ".kt", ".go", ".rs", ".rb",
        ".html", ".css", ".scss", ".vue", ".tsx", ".jsx",
        ".sh", ".bat", ".ps1", ".ini", ".toml", ".conf",
    };

    public PreviewPane()
    {
        InitializeComponent();
        Clear();
    }

    public void Clear()
    {
        Thumb.Source = null;
        BigIcon.Source = null;
        BigIcon.Visibility = Visibility.Collapsed;
        TextScroll.Visibility = Visibility.Collapsed;
        TextPreview.Text = "";
        NameText.Text = "";
        MetaPanel.Children.Clear();
    }

    public void Show(string path)
    {
        Clear();
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (Directory.Exists(path))
            {
                var di = new DirectoryInfo(path);
                NameText.Text = di.Name;
                BigIcon.Source = ShellIcon.Get(path, true);
                BigIcon.Visibility = Visibility.Visible;
                int folders = 0, files = 0;
                try { folders = di.EnumerateDirectories().Count(); files = di.EnumerateFiles().Count(); } catch { }
                AddMeta("종류", "폴더");
                AddMeta("항목", $"{folders}개 폴더, {files}개 파일");
                AddMeta("수정일", di.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                return;
            }

            if (!File.Exists(path)) return;
            var fi = new FileInfo(path);
            NameText.Text = fi.Name;

            var ext = fi.Extension.ToLowerInvariant();
            if (ImageExts.Contains(ext))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.DecodePixelWidth = 800;
                bmp.EndInit();
                bmp.Freeze();
                Thumb.Source = bmp;
                AddMeta("종류", "이미지");
                AddMeta("크기", $"{bmp.PixelWidth} × {bmp.PixelHeight}");
            }
            else if (TextExts.Contains(ext) && fi.Length < 512 * 1024)
            {
                using var fs = File.OpenRead(path);
                using var sr = new StreamReader(fs);
                var buf = new char[8192];
                int read = sr.Read(buf, 0, buf.Length);
                TextPreview.Text = new string(buf, 0, read);
                TextScroll.Visibility = Visibility.Visible;
                AddMeta("종류", "텍스트");
            }
            else
            {
                BigIcon.Source = ShellIcon.Get(path, false);
                BigIcon.Visibility = Visibility.Visible;
                AddMeta("종류", string.IsNullOrEmpty(ext) ? "파일" : ext.TrimStart('.').ToUpper() + " 파일");
            }

            AddMeta("용량", FormatSize(fi.Length));
            AddMeta("수정일", fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
        }
        catch (Exception ex)
        {
            NameText.Text = "미리보기 실패";
            AddMeta("오류", ex.Message);
        }
    }

    private void AddMeta(string label, string value)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        sp.Children.Add(new TextBlock
        {
            Text = label,
            Width = 56,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            FontSize = 11,
        });
        sp.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
        });
        MetaPanel.Children.Add(sp);
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int u = 0;
        while (size >= 1024 && u < units.Length - 1) { size /= 1024; u++; }
        return $"{size:0.#} {units[u]}";
    }
}
