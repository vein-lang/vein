using Spectre.Console;
using static GitVersionInformation;

AnsiConsole.MarkupLine($"[grey]Ishtar DCH[/] [red]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");
AnsiConsole.MarkupLine($"[grey]Copyright (C)[/] [cyan3]2021[/] [bold]Yuuki Wesp[/].\n\n");

static string CMD(string key) => $"\a\a\t\t{key}\n";

while (true)
{
    var key = Console.ReadLine();

    if (key.Contains(CMD("EXIT")))
        break;

    AnsiConsole.MarkupLine(key);
}

