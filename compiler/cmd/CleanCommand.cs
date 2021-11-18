namespace vein.cmd;

using System.IO;
using System.Linq;
using MoreLinq;
using project;
using Spectre.Console.Cli;
using vein.extensions;

public class CleanCommand : Command
{
    public override int Execute(CommandContext context)
    {
        new DirectoryInfo(Directory.GetCurrentDirectory())
            .EnumerateFiles("*.vproj")
            .Count(out var len)
            .Select(VeinProject.LoadFrom)
            .Pipe(x => x.CacheDir.Delete(true))
            .Consume();

        Log.Info($"[green]Success[/] cleaned [orange]{len}[/] projects.");

        return 0;
    }
}
