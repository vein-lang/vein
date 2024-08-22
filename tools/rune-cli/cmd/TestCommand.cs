namespace vein.cmd;

[ExcludeFromCodeCoverage]
public class TestCommandSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Path to vproj file")]
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }
}

[ExcludeFromCodeCoverage]
public class TestCommand(WorkloadDb db) : AsyncCommandWithProject<TestCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext ctx, TestCommandSettings settings, VeinProject project) =>
        new RunCommand(db).ExecuteAsync(ctx, new RunSettings()
        {
            EntryPoint = "fixture"
        }, project);
}
