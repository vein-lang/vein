namespace vein.project.shards;

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

public static class StreamExtensions
{
    private const long MAX_MMAP_SIZE = 10 * 1024 * 1024;

    public static async Task CopyToAsync(this Stream stream, Stream destination, CancellationToken token)
        => await stream.CopyToAsync(destination, 8192, token);

    public static string CopyToFile(this Stream inputStream, string fileFullPath)
    {
        if (Path.GetFileName(fileFullPath).Length == 0)
        {
            Directory.CreateDirectory(fileFullPath);
            return fileFullPath;
        }

        var directory = Path.GetDirectoryName(fileFullPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(fileFullPath))
            return fileFullPath;

        long? size = null;
        try
        {
            size = inputStream.Length;
        }
        catch (NotSupportedException) { }
        using (var outputStream = ExtractionFileIO.CreateFile(fileFullPath))
        {
            if (size is > 0 and <= MAX_MMAP_SIZE)
            {
                outputStream.Dispose();
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fileFullPath, FileMode.Open, mapName: null, (long)size))
                using (MemoryMappedViewStream mmstream = mmf.CreateViewStream())
                    inputStream.CopyTo(mmstream);
            }
            else
                inputStream.CopyTo(outputStream);
        }
        return fileFullPath;
    }
}
