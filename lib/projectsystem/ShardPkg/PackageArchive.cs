namespace vein.project.shards;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

public class PackageArchive : PackageBase
{
    private readonly ZipArchive _zipArchive;

    protected Stream ZipStream { get; set; }

    public long Size => ZipStream.Length;

    public override IEnumerable<string> GetFiles()
        => _zipArchive.GetFiles();

    public override string GetFile(string name)
        => _zipArchive.GetFiles().FirstOrDefault(x => x.Equals(name));

    public override IEnumerable<string> GetFiles(string folder)
        => GetFiles().Where(f => f.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));

    public override Stream GetStream(string path)
    {
        var stream = default(Stream);
        if (path is not null)
            stream = _zipArchive.OpenFile(path);

        return stream;
    }

    public void ExtractTo(DirectoryInfo dir)
        => _zipArchive.ExtractToDirectory(dir.FullName, true);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _zipArchive.Dispose();
            ZipStream.Dispose();
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _zipArchive.Dispose();
            await ZipStream.DisposeAsync();
        }
    }

    public PackageArchive(FileInfo info)
    {
        if (!info.Exists)
            throw new FileNotFoundException($"File '{info.FullName}' is not found.");
        ZipStream = info.OpenRead();
        _zipArchive = new ZipArchive(ZipStream);
    }

    public PackageArchive(Stream stream)
    {
        ZipStream = stream;
        _zipArchive = new ZipArchive(ZipStream);
    }
}
