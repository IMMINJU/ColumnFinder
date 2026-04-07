using System.IO;

namespace ColumnFinder.Models;

public class FileEntry
{
    public string Name { get; init; } = "";
    public string FullPath { get; init; } = "";
    public bool IsDirectory { get; init; }

    public static FileEntry From(FileSystemInfo info) => new()
    {
        Name = info.Name,
        FullPath = info.FullName,
        IsDirectory = info is DirectoryInfo,
    };
}
