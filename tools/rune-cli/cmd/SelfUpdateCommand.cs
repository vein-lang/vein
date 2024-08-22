namespace vein.cmd;

public class SelfUpdateCommandSettings : CommandSettings
{
    [Description("A flag indicates that current instance has been moved to the temp folder and deletion/update is now allowed")]
    [CommandOption("--isolated", IsHidden = true)]
    public bool IsolateSuccess { get; set; }
}


public class SelfUpdateCommand : Command<SelfUpdateCommandSettings>
{
    public override int Execute(CommandContext context, SelfUpdateCommandSettings settings) => 0;
}
