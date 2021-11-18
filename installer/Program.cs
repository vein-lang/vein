using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;
using vein;
using static GitVersionInformation;

AnsiConsole.MarkupLine($"[grey]Vein installer[/] [red]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");
AnsiConsole.MarkupLine($"[grey]Copyright (C)[/] [cyan3]2021[/] [bold]Yuuki Wesp[/].\n\n");

ColorShim.Apply();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    Console.OutputEncoding = Encoding.Unicode;

var bin_folder = new DirectoryInfo("./bin");
var manifest_folder = new DirectoryInfo("./.vein");
var std_folder = new DirectoryInfo("./std");


if (!new[] { bin_folder, manifest_folder, std_folder }.All(x => x.Exists))
{
    AnsiConsole.MarkupLine($"[red]Installer package is corrupted.[/]");
    AnsiConsole.MarkupLine($"'[gray]{bin_folder.FullName}[/]' Exists: {bin_folder.Exists}");
    AnsiConsole.MarkupLine($"'[gray]{manifest_folder.FullName}[/]' Exists: {manifest_folder.Exists}");
    AnsiConsole.MarkupLine($"'[gray]{std_folder.FullName}[/]' Exists: {std_folder.Exists}");
    return -1;
}

var manifest_target_folder =
    new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vein"));

if (manifest_target_folder.Exists)
    manifest_target_folder.Delete(true);

CopyFilesRecursively(manifest_folder, manifest_target_folder);
AnsiConsole.MarkupLine($"Manifest is installed.");

static void chmod(FileInfo info)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = "/bin/bash",
            Arguments = $"-c \"{$"chmod +x {info.FullName}"}\""
        }).WaitForExit();
    }
    catch (Exception e)
    {
        AnsiConsole.MarkupLine($"[red]Failed[/] execute 'chmod +x {info.FullName}'");
        throw;
    }
}

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    foreach (var file in bin_folder.EnumerateFiles())
    {
        if (file.Name.Contains("ishtar"))
            chmod(file);
        if (file.Name.Contains("dch"))
            chmod(file);
        if (file.Name.Contains("veinc"))
            chmod(file);
        if (file.Name.Contains("veinlsp"))
            chmod(file);
    }


AnsiConsole.MarkupLine($"[green]Success[/] install Vein Lang [grey]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");

return 0;





static DirectoryInfo CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
{
    var newDirectoryInfo = target.CreateSubdirectory(source.Name);
    foreach (var fileInfo in source.GetFiles())
        fileInfo.CopyTo(Path.Combine(newDirectoryInfo.FullName, fileInfo.Name));

    foreach (var childDirectoryInfo in source.GetDirectories())
        CopyFilesRecursively(childDirectoryInfo, newDirectoryInfo);

    return newDirectoryInfo;
}
