namespace mana.project
{
    using System.IO;
    using System.Linq;

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

        public static FileInfo SingleFileByPattern(this DirectoryInfo info, string pattern)
            => info.GetFiles(pattern, SearchOption.TopDirectoryOnly).Single();

        public static string ReadToEnd(this FileInfo info) 
            => File.ReadAllText(info.FullName);
        public static byte[] ReadAllBytes(this FileInfo info) 
            => File.ReadAllBytes(info.FullName);
    }
}