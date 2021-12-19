namespace vein.cmd;

using Spectre.Console.Cli;
using System.ComponentModel;

[ExcludeFromCodeCoverage]
public class SetConfigCommandSettings : CommandSettings
{
    [Description("A key.")]
    [CommandArgument(0, "[KEY]")]
    public string Key { get; set; }

    [Description("A value.")]
    [CommandArgument(1, "[VALUE]")]
    public string Value { get; set; }
}

[ExcludeFromCodeCoverage]
public class SetConfigCommand : Command<SetConfigCommandSettings>
{
    public override int Execute(CommandContext context, SetConfigCommandSettings settings)
    {
        SecurityStorage.AddKey(settings.Key, settings.Value);
        Log.Info($"[green]Success[/] save [orange]'{settings.Key}'[/] into [orange]global[/] configuration.");
        return 0;
    }
}

[ExcludeFromCodeCoverage]
public class GetConfigCommandSettings : CommandSettings
{
    [Description("A key.")]
    [CommandArgument(0, "[KEY]")]
    public string Key { get; set; }
}

[ExcludeFromCodeCoverage]
public class GetConfigCommand : Command<GetConfigCommandSettings>
{
    public override int Execute(CommandContext context, GetConfigCommandSettings settings)
    {
        if (!SecurityStorage.HasKey(settings.Key))
        {
            Log.Info($"Key [orange]'{settings.Key}'[/] not found in [orange]global[/] configuration.");
            return -1;
        }

        var value = SecurityStorage.GetByKey<string>(settings.Key);
        Log.Info($"[green]{settings.Key}[/]: [orange]'{value}'[/]");
        return 0;
    }
}


[ExcludeFromCodeCoverage]
public class ListConfigCommand : Command
{
    public override int Execute(CommandContext context)
    {
        foreach (string key in SecurityStorage.GetAllKeys())
            Log.Info($"[green]{key}[/]");
        return 0;
    }
}
