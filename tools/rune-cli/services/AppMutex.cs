namespace vein.services;

using System.Diagnostics;
using Spectre.Console;

public static class AppMutex
{
    private static FileInfo LockFile => SecurityStorage.RootFolder.File(".lock");
    public static async Task Begin()
    {
        if (LockFile.Exists)
        {
            var pid = int.Parse(await LockFile.ReadToEndAsync());
            if (Process.GetCurrentProcess().Id == pid)
                return;
            if (ProcessHasExist(pid))
            {
                AnsiConsole.Markup($"Lockfile [gray]{LockFile.FullName}[/] exist, currently rune cli already active, processId: [red]{pid}[/]");
                Environment.Exit(-1);
            }
        }
        await LockFile.WriteAllTextAsync(Process.GetCurrentProcess().Id.ToString());
    }

    public static async Task End()
    {
        if (LockFile.Exists)
            LockFile.Delete();
    }

    private static bool ProcessHasExist(int pid)
    {
        try
        {
            Process.GetProcessById(pid);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

}
