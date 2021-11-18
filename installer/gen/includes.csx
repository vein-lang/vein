#r "nuget: Spectre.Console, 0.42.1-preview.0.18"
#r "nuget: Flurl.Http, 3.2.0"
#r "nuget: Newtonsoft.Json, 13.0.1"


using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using Flurl;
using Flurl.Http;
using Spectre.Console;


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    Console.OutputEncoding = Encoding.Unicode;


AnsiConsole.MarkupLine($"[grey]Vein blob generator[/]");
AnsiConsole.MarkupLine($"[grey]Copyright (C)[/] [cyan3]2021[/] [bold]Yuuki Wesp[/].\n\n");