namespace vein;
#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class FileEntityEx
{
    public static DirectoryInfo SubDirectory(this DirectoryInfo current, string target)
        => new(Path.Combine(current.FullName, target));

    public static DirectoryInfo ThrowIfNotExist(this DirectoryInfo dir, string msg)
    {
        if (!dir.Exists)
            throw new DirectoryNotFoundException(msg);
        return dir;
    }

    public static DirectoryInfo Ensure(this DirectoryInfo dir)
    {
        if (dir.Exists)
            return dir;
        dir.Create();
        return dir;
    }

    public static FileInfo SingleFileByPattern(this DirectoryInfo info, string pattern)
        => info.GetFiles(pattern, SearchOption.TopDirectoryOnly).Single();


    public static FileInfo File(this DirectoryInfo info, string name)
    {
        if (info.Exists) foreach (var file in info.GetFiles())
        {
            if (file.Name.Equals(name))
                return file;
        }

        return new FileInfo(Path.Combine(info.FullName, name));
    }

    public static string SanitizeFileSystemName(this string name)
        => Path.GetInvalidFileNameChars().Concat([' ', ')', '(', '\\', '/', ',', '@']).Aggregate(name, (current, c) => current.Replace(c.ToString(), ""));

    public static void WriteAllText(this FileInfo info, string content)
        => System.IO.File.WriteAllText(info.FullName, content);
    public static void WriteAllBytes(this FileInfo info, byte[] content)
        => System.IO.File.WriteAllBytes(info.FullName, content);

    public static Task WriteAllTextAsync(this FileInfo info, string content, CancellationToken ct = default)
        => System.IO.File.WriteAllTextAsync(info.FullName, content, ct);
    public static Task WriteAllBytesAsync(this FileInfo info, byte[] content, CancellationToken ct = default)
        => System.IO.File.WriteAllBytesAsync(info.FullName, content, ct);

    public static string ReadToEnd(this FileInfo info)
        => System.IO.File.ReadAllText(info.FullName);
    public static byte[] ReadAllBytes(this FileInfo info)
        => System.IO.File.ReadAllBytes(info.FullName);
    public static string[] ReadAllLines(this FileInfo info)
        => System.IO.File.ReadAllLines(info.FullName);


    public static Task<string> ReadToEndAsync(this FileInfo info)
        => System.IO.File.ReadAllTextAsync(info.FullName);
    public static Task<byte[]> ReadAllBytesAsync(this FileInfo info)
        => System.IO.File.ReadAllBytesAsync(info.FullName);
    public static Task<string[]> ReadAllLinesAsync(this FileInfo info)
        => System.IO.File.ReadAllLinesAsync(info.FullName);
}
