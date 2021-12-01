namespace vein
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


        public static FileInfo File(this DirectoryInfo info, string name)
        {
            if (info.Exists) foreach (var file in info.GetFiles())
                {
                    if (file.Name.Equals(name))
                        return file;
                }

            return new FileInfo(Path.Combine(info.FullName, name));
        }

        public static string ReadToEnd(this FileInfo info)
            => System.IO.File.ReadAllText(info.FullName);
        public static byte[] ReadAllBytes(this FileInfo info)
            => System.IO.File.ReadAllBytes(info.FullName);
        public static string[] ReadAllLines(this FileInfo info)
            => System.IO.File.ReadAllLines(info.FullName);
    }
}
