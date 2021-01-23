namespace wave.langserver
{
    using System;

    public class FileContent
    {
        private readonly Uri _path;

        public FileContent(Uri path)
        {
            _path = path;
        }


        public static implicit operator FileContent(string path) => new FileContent(new Uri(path));
        public static implicit operator FileContent(Uri path) => new FileContent(path);
    }
}