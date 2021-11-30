namespace vein.project.shards;

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class ExtractionFileIO
{
    private static int _unixPermissions = Convert.ToInt32("766", 8);
    private static Lazy<Func<string, FileStream>> _createFileMethod =
        new Lazy<Func<string, FileStream>>(CreateFileMethodSelector);


    internal static FileStream CreateFile(string path) => _createFileMethod.Value(path);


    private static Func<string, FileStream> CreateFileMethodSelector()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return File.Create;
        else
            return _CreateFile;
    }

    private static FileStream _CreateFile(string path)
    {
        // .NET APIs don't expose UNIX file permissions, so P/Invoke the POSIX create file API with our
        // preferred permissions, and wrap the file handle/descriptor in a SafeFileHandle.
        int fd;
        try
        {
            fd = PosixCreate(path, _unixPermissions);
        }
        catch (Exception exception)
        {
            throw new Exception($"Error trying to create file {path}: {exception.Message}", exception);
        }

        if (fd == -1)
        {
            using (File.Create(path)) { }
            File.Delete(path);
            throw new InvalidOperationException("libc creat failed, but File.Create did not");
        }

        var sfh = new SafeFileHandle((IntPtr)fd, ownsHandle: true);

        try
        {
            return new FileStream(sfh, FileAccess.ReadWrite);
        }
        catch
        {
            sfh.Dispose();
            throw;
        }
    }


    [DllImport("libc", EntryPoint = "creat")]
    private static extern int PosixCreate([MarshalAs(UnmanagedType.LPStr)] string pathname, int mode);
}
