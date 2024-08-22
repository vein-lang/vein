namespace vein.cmd;

using MoreLinq;
using vein.extensions;


[ExcludeFromCodeCoverage]
public class CleanCommand : Command
{
    public override int Execute(CommandContext context)
    {
        new DirectoryInfo(Directory.GetCurrentDirectory())
            .EnumerateFiles("*.vproj", SearchOption.AllDirectories)
            .Select(VeinProject.LoadFrom)
            .Where(x => x.CacheDir.Exists)
            .Count(out var len)
            .Pipe(x => x.CacheDir.Delete(true))
            .Consume();

        Log.Info($"[green]Success[/] cleaned [orange]{len}[/] projects.");

        return 0;
    }
}
