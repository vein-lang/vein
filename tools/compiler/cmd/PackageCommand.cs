namespace vein.cmd;

using Spectre.Console.Cli;

[ExcludeFromCodeCoverage]
public class PackageCommand : Command<CompileSettings>
{
    public override int Execute(CommandContext context, CompileSettings settings)
    {
        settings.GeneratePackageOutput = true;
        return new CompileCommand().Execute(context, settings);
    }
}
