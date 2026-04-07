using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualBasic.FileIO;

namespace ColumnFinder.Services;

/// <summary>
/// 파일 작업 래퍼. 삭제는 휴지통으로, 복사/이동은 셸 다이얼로그를 통해 진행.
/// </summary>
public static class FileOps
{
    public static void CopyToClipboard(string[] paths, bool cut)
    {
        var data = new DataObject();
        var col = new StringCollection();
        col.AddRange(paths);
        data.SetFileDropList(col);

        // Preferred DropEffect: 2=move(cut), 5=copy
        var stream = new MemoryStream(BitConverter.GetBytes(cut ? 2 : 5));
        data.SetData("Preferred DropEffect", stream);
        Clipboard.SetDataObject(data, true);
    }

    public static void Paste(string targetDir)
    {
        if (!Clipboard.ContainsFileDropList()) return;
        var files = Clipboard.GetFileDropList().Cast<string>().ToArray();
        bool cut = false;
        if (Clipboard.GetDataObject()?.GetData("Preferred DropEffect") is MemoryStream ms)
        {
            var b = new byte[4]; ms.Read(b, 0, 4);
            cut = BitConverter.ToInt32(b, 0) == 2;
        }
        foreach (var src in files)
        {
            var dst = Path.Combine(targetDir, Path.GetFileName(src));
            try
            {
                if (Directory.Exists(src))
                {
                    if (cut) FileSystem.MoveDirectory(src, dst, UIOption.AllDialogs, UICancelOption.DoNothing);
                    else FileSystem.CopyDirectory(src, dst, UIOption.AllDialogs, UICancelOption.DoNothing);
                }
                else if (File.Exists(src))
                {
                    if (cut) FileSystem.MoveFile(src, dst, UIOption.AllDialogs, UICancelOption.DoNothing);
                    else FileSystem.CopyFile(src, dst, UIOption.AllDialogs, UICancelOption.DoNothing);
                }
            }
            catch { }
        }
    }

    public static void DeleteToRecycleBin(string[] paths)
    {
        foreach (var p in paths)
        {
            try
            {
                if (Directory.Exists(p))
                    FileSystem.DeleteDirectory(p, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
                else if (File.Exists(p))
                    FileSystem.DeleteFile(p, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
            }
            catch { }
        }
    }

    public static void Rename(string path, string newName)
    {
        try
        {
            if (Directory.Exists(path))
                FileSystem.RenameDirectory(path, newName);
            else if (File.Exists(path))
                FileSystem.RenameFile(path, newName);
        }
        catch { }
    }

    public static void DropFiles(string[] sources, string targetDir, bool move)
    {
        foreach (var src in sources)
        {
            var dst = Path.Combine(targetDir, Path.GetFileName(src));
            if (string.Equals(src, dst, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                if (Directory.Exists(src))
                {
                    if (move) FileSystem.MoveDirectory(src, dst, UIOption.AllDialogs, UICancelOption.DoNothing);
                    else FileSystem.CopyDirectory(src, dst, UIOption.AllDialogs, UICancelOption.DoNothing);
                }
                else if (File.Exists(src))
                {
                    if (move) FileSystem.MoveFile(src, dst, UIOption.AllDialogs, UICancelOption.DoNothing);
                    else FileSystem.CopyFile(src, dst, UIOption.AllDialogs, UICancelOption.DoNothing);
                }
            }
            catch { }
        }
    }

    public static string CreateNewFolder(string parentDir)
    {
        var baseName = "새 폴더";
        var name = baseName;
        int i = 2;
        while (Directory.Exists(Path.Combine(parentDir, name)))
            name = $"{baseName} ({i++})";
        var full = Path.Combine(parentDir, name);
        Directory.CreateDirectory(full);
        return full;
    }
}
