namespace vein;

public static class DirectoryExtensions
{
    public static DirectoryInfo Combine(this DirectoryInfo a, DirectoryInfo b) => new(Path.Combine(a.FullName, b.ToString()));
    public static FileInfo Combine(this DirectoryInfo a, FileInfo b) => new(Path.Combine(a.FullName, b.ToString()));
    public static FileInfo ToFile(this string a) => new(a);
    public static async Task<FileInfo> ToFileAsync(this Task<string> a) => new(await a);

    public static async Task<DirectoryInfo> ToDirectoryAsync(this Task<string> a) => new(await a);
    public static DirectoryInfo ToDirectory(this string a) => new(a);
}
