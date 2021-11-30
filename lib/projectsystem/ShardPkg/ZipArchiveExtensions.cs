namespace vein.project.shards;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

public static class ZipArchiveExtensions
{
    public static ZipArchiveEntry LookupEntry(this ZipArchive zipArchive, string path)
    {
        var entry = zipArchive.Entries.FirstOrDefault(zipEntry => UnescapePath(zipEntry.FullName) == path);
        if (entry == null)
            throw new FileNotFoundException(path);

        return entry;
    }

    public static IEnumerable<string> GetFiles(this ZipArchive zipArchive)
        => zipArchive.Entries.Select(e => UnescapePath(e.FullName));

    private static string UnescapePath(string path)
    {
        if (path != null && path.IndexOf('%', StringComparison.Ordinal) > -1)
            return Uri.UnescapeDataString(path);
        return path;
    }

    public static Stream OpenFile(this ZipArchive zipArchive, string path)
    {
        var entry = LookupEntry(zipArchive, path);
        return entry.Open();
    }
}
