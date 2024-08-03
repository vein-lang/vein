namespace vein.project;

using System.IO;
using static System.String;

public class SymlinkCollector(DirectoryInfo baseFolder)
{
    public const string CmdFileTemplate =
        """
        @ECHO off
        GOTO start
        :find_dp0
        SET dp0=%~dp0
        EXIT /b
        :start
        SETLOCAL
        CALL :find_dp0
        "{0}"   %*
        """;

    public const string PwsFileTemplate =
        """
        #!/usr/bin/env pwsh
        & "{0}"   $args
        exit $LASTEXITCODE
        """;

    public const string ShellTemplate =
        """
        #!/bin/sh
        exec "{0}"   "$@"
        """;


    public DirectoryInfo SymlinkFolder = baseFolder.SubDirectory("bin");
    public void GenerateSymlink(string name, FileInfo @for)
    {
        var file = SymlinkFolder.Ensure().File($"{name}.symlink.v");
        if (file.Exists) DeleteSymlink(name);

        SymlinkFolder.File($"{name}.symlink.v").WriteAllText($"'{name}' = '{@for.FullName}'");
        SymlinkFolder.File($"{name}.cmd").WriteAllText(Format(CmdFileTemplate, @for.FullName));
        SymlinkFolder.File($"{name}.ps1").WriteAllText(Format(PwsFileTemplate, @for.FullName));
        SymlinkFolder.File($"{name}.sh").WriteAllText(Format(ShellTemplate, @for.FullName));
    }

    public void DeleteSymlink(string name)
    {
        SymlinkFolder.File($"{name}.symlink.v").Delete();
        SymlinkFolder.File($"{name}.cmd").Delete();
        SymlinkFolder.File($"{name}.ps1").Delete();
        SymlinkFolder.File($"{name}.sh").Delete();
    }

    public string ToExec(string binary)
    {
        var platform = PlatformKey.GetCurrentPlatform();

        if (platform.key.StartsWith("win") && !binary.EndsWith(".exe"))
            return $"{binary}.exe";
        return binary;
    }
}
