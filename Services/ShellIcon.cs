using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Vanara.PInvoke;

namespace ColumnFinder.Services;

/// <summary>
/// Windows 셸에서 파일/폴더 아이콘을 추출해서 BitmapSource로 반환.
/// 결과는 확장자(파일) 또는 "DIR" 키로 캐시.
/// </summary>
public static class ShellIcon
{
    private static readonly ConcurrentDictionary<string, BitmapSource> _cache = new();

    public static BitmapSource? Get(string path, bool isDirectory)
    {
        var key = isDirectory ? "DIR" : System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (_cache.TryGetValue(key, out var cached)) return cached;

        var attrs = isDirectory
            ? FileAttributes.Directory
            : FileAttributes.Normal;

        var flags = Shell32.SHGFI.SHGFI_ICON
                  | Shell32.SHGFI.SHGFI_SMALLICON
                  | Shell32.SHGFI.SHGFI_USEFILEATTRIBUTES;

        var shfi = new Shell32.SHFILEINFO();
        var hr = Shell32.SHGetFileInfo(path, attrs, ref shfi, Marshal.SizeOf(shfi), flags);
        if (hr == IntPtr.Zero || shfi.hIcon.IsNull) return null;

        try
        {
            var src = Imaging.CreateBitmapSourceFromHIcon(
                (IntPtr)shfi.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            src.Freeze();
            _cache[key] = src;
            return src;
        }
        finally
        {
            User32.DestroyIcon((IntPtr)shfi.hIcon);
        }
    }
}
