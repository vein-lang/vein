namespace vein.cmd;

using project;
using Spectre.Console;
using Spectre.Console.Cli;

[ExcludeFromCodeCoverage]
public class ListInstalledWorkloadCommand : AsyncCommandWithProgress<ListInstalledWorkloadCommand.ListWorkloadCommandSettings>
{
    [ExcludeFromCodeCoverage]
    public class ListWorkloadCommandSettings : CommandSettings;


    public static readonly ShardStorage Storage = new();

    public override async Task<int> ExecuteAsync(ProgressContext context, ListWorkloadCommandSettings settings)
    {
        var manifests = await Storage.GetInstalledWorkloads();

        if (manifests.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]No[/] one installed workloads");
            return 0;
        }

        var groups = manifests.GroupBy(x => x.Name);
        foreach (var a in groups)
        {
            AnsiConsole.MarkupLine($"Workload [blue]'{a.Key}'[/]");
            foreach (var manifest in a)
            {
                AnsiConsole.MarkupLine($"- [yellow]{manifest.Name}[/]@[aqua]{manifest.Version}[/]");

                foreach (var (key, pkg) in manifest.Packages)
                {
                    AnsiConsole.MarkupLine($"-- [yellow]{key.key}[/] as [red]{pkg.Kind.key}[/]");

                    foreach (var @base in pkg.Definition)
                    {
                        if (@base is WorkloadPackageTool tool)
                        {
                            AnsiConsole.MarkupLine($"--- [blue]exported[/] global command '[magenta1]{(tool.OverrideName ?? Path.GetFileNameWithoutExtension(tool.ExecPath))}[/]'");
                        }
                    }
                }
                
            }
        }

        return 0;
    }
}
