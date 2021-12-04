namespace vein.project.shards;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public abstract class PackageBase : IDisposable, IAsyncDisposable
{
    public abstract IEnumerable<string> GetFiles(string folder);
    public abstract IEnumerable<string> GetFiles();
    public abstract string GetFile(string name);
    public abstract Stream GetStream(string path);


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void Dispose(bool disposing);
    protected abstract ValueTask DisposeAsync(bool disposing);
}
