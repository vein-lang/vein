using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Pastel;
using System.Drawing;
using System.Globalization;
using Newtonsoft.Json;
using wave;
using static wave._term;
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


if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
{
    WriteLine("Platform is not supported.");
    return -1;
}
var watch = Stopwatch.StartNew();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    OutputEncoding = Encoding.Unicode;
JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
{
    NullValueHandling = NullValueHandling.Ignore,
    Formatting = Formatting.Indented,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    Culture = CultureInfo.InvariantCulture
};


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

var ver = FileVersionInfo.GetVersionInfo(typeof(_term).Assembly.Location).ProductVersion;
WriteLine($"Wave compiler {ver}".Pastel(Color.Gray));
WriteLine($"Copyright (C) 2020 Yuuki Wesp.\n\n".Pastel(Color.Gray));

rootCommand.Handler = CommandHandler.Create<DirectoryInfo, FileInfo>((sourcePath, output) =>
{
    if (sourcePath is null)
        return Fail("'--source-dir' is null.");
    if (output is null)
        return Fail("'--out-file' is null.");
    return Pipeline.StartAsync(sourcePath, output);
});

var result = rootCommand.InvokeAsync(args).Result;

watch.Stop();

WriteLine($"{":sparkles:".Emoji()} Done in {watch.Elapsed.TotalSeconds:00.000}s.");

return result;