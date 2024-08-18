namespace vein.cmd;

using compiler.shared;
using project;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Xml.Linq;

[ExcludeFromCodeCoverage]
public class PackageCommand(WorkloadDb db) : AsyncCommandWithProject<CompileSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CompileSettings settings, VeinProject project)
    {
        using var tag = ScopeMetric.Begin("project.pack")
            .WithProject(project);
        var tool = await db.TakeTool(PackageKey.VeinCompilerPackage, "veinc", false);
        if (tool is null)
        {
            Log.Error($"'[orange]{PackageKey.VeinCompilerPackage}[/]' package not found, it may need to be installed by '[gray]rune workload install vein.compiler[/]'?");
            return -1;
        }

        return await new VeinCompilerProxy(tool, context.Arguments.Concat(["--gen-shard"])).ExecuteAsync();
    }
}
