namespace vein.project.shards;

using System;
using System.Collections.Generic;
using System.IO;

public abstract class PackageBase : IDisposable
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

    protected abstract void Dispose(bool disposing);
}
