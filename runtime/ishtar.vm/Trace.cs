namespace ishtar;

using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class Trace
{
    private static Process dch_proc;
    private static FileInfo dch_file
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new("dch.exe");
            return new("dth");
        }
    }


    private static bool useDCH;
    private static bool useConsole;
    private static bool useFile;

    public static void init()
    {
        useDCH = Environment.GetEnvironmentVariable("--sys::log::use-dch=1") is not null;
        useConsole = Environment.GetEnvironmentVariable("--sys::log::use-console=1") is not null;
        useFile = Environment.GetEnvironmentVariable("--sys::log::use-file=1") is not null; // TODO
        
        if (useDCH)
        {
            if (!dch_file.Exists)
            {
                useConsole = true;
                useDCH = false;
                return;
            }
            dch_proc = Process.Start(new ProcessStartInfo(dch_file.FullName)
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false
            });
        }
    }
    public static void println(string s)
    {
        if (useDCH)
        {
            if (dch_proc is null) return;
            dch_proc.StandardInput.Write($"{s}\n");
        }

        if (useConsole)
        {
            Console.WriteLine(s);
        }
    }
}
