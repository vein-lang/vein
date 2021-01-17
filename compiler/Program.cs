using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Pastel;
using System.Drawing;
using System.Threading.Tasks;
using wave;
using static System.Console;


if (Environment.GetEnvironmentVariable("WT_SESSION") == null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Environment.SetEnvironmentVariable($"RUNE_EMOJI_USE", "0");
    Environment.SetEnvironmentVariable($"RUNE_COLOR_USE", "0");
    Environment.SetEnvironmentVariable($"RUNE_NIER_USE", "0");
    Environment.SetEnvironmentVariable($"NO_COLOR", "true");
    ForegroundColor = ConsoleColor.Gray;
    WriteLine($"\t@\tno windows-terminal: coloring, emoji and nier has disabled.");
    ForegroundColor = ConsoleColor.White;
}

AppDomain.CurrentDomain.ProcessExit += (_, _) => { ConsoleExtensions.Disable(); };


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


var rootCommand = new RootCommand
{
    new Option<DirectoryInfo>(
        new []{"--source-dir", "-s"},
        "Source directory."),
    new Option<FileInfo>(
        new []{"--out-file", "-o"},
        "Filename for output.")
};

rootCommand.Description = "Wave Compiler";

var ver = FileVersionInfo.GetVersionInfo(typeof(Test).Assembly.Location).ProductVersion;
WriteLine($"Wave compiler {ver}".Pastel(Color.Gray));
WriteLine($"Copyright (C) 2020 Yuuki Wesp.\n\n".Pastel(Color.Gray));

rootCommand.Handler = CommandHandler.Create<DirectoryInfo, FileInfo>((sourcePath, output) =>
{
    if (!sourcePath.Exists)
    {
        return Task.FromResult(-1);
    }
});

return rootCommand.InvokeAsync(args).Result;