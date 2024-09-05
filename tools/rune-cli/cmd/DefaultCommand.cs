namespace vein.cmd;

public class DefaultCommandSettings : CommandSettings
{
    [Description("Show app version")]
    [CommandOption("--version|-v")]
    public bool ShowVersion { get; set; }
}

public class DefaultCommand : Command<DefaultCommandSettings>
{
    public override int Execute(CommandContext context, DefaultCommandSettings settings)
    {
        return 0;
    }
}
