namespace vein.cmd;


public class WorkloadGetToolCommandSettings : CommandSettings
{
    [CommandOption("--tool", IsHidden = true)]
    public string ToolName { get; set; }

    [CommandOption("--package", IsHidden = true)]
    public string PackageName { get; set; }
}

public class WorkloadGetTool(WorkloadDb db) : AsyncCommand<WorkloadGetToolCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, WorkloadGetToolCommandSettings settings)
    {
        var tool = await db.TakeTool(new PackageKey(settings.PackageName), settings.ToolName, false);

        if (tool is null)
        {
            await Console.Out.WriteLineAsync($"null");
            return 0;
        }
        await Console.Out.WriteLineAsync($"{tool.FullName}");
        return 0;
    }
}
