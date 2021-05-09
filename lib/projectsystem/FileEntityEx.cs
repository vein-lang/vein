namespace wave.project
{
    using System.IO;

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

        public static string ReadToEnd(this FileInfo info) 
            => File.ReadAllText(info.FullName);
    }
}