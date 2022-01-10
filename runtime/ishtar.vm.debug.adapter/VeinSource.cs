namespace ishtar.debugger;

using System.Text;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Utilities;
using static System.FormattableString;

internal class VeinSource
{
    public VeinSource(string name, string path, int sourceReference)
    {
        this.Name = name;
        this.Path = path;
        this.SourceReference = sourceReference;
    }

    public string Name { get; }
    public string Path { get; }
    public int SourceReference { get; }

    internal Source GetProtocolSource() =>
        new Source() { Name = this.Name, Path = this.Path, SourceReference = this.SourceReference.ZeroToNull() };

    internal static VeinSource Create(StringBuilder output, SampleSourceManager sampleScriptManager, string name, string path, int sourceReference)
    {
        if (sourceReference > 0 && !string.IsNullOrWhiteSpace(path))
            output.AppendLine(Invariant($"Source {name} should not have both a path and a source reference!"));
        return new VeinSource(name, path, sourceReference);
    }
}
