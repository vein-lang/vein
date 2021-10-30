namespace vein.fs;

using System.IO;
using project;

public enum ArtifactKind
{
    NONE,
    BINARY,
    DEBUG_SYMBOLS,
    IL,
    RESOURCES
}
public abstract class VeinArtifact
{
    public FileInfo Path { get; protected set; }
    public ArtifactKind Kind { get; protected set; }
    public VeinProject Project { get; protected set; }
}

public class DebugSymbolArtifact : VeinArtifact
{
    public DebugSymbolArtifact(FileInfo path, VeinProject project)
    {
        base.Kind = ArtifactKind.DEBUG_SYMBOLS;
        base.Path = path;
        base.Project = project;
    }
}
public class BinaryArtifact : VeinArtifact
{
    public BinaryArtifact(FileInfo path, VeinProject project)
    {
        base.Kind = ArtifactKind.BINARY;
        base.Path = path;
        base.Project = project;
    }
}
public class ILArtifact : VeinArtifact
{
    public ILArtifact(FileInfo path, VeinProject project)
    {
        base.Kind = ArtifactKind.IL;
        base.Path = path;
        base.Project = project;
    }
}
public class ResourceArtifact : VeinArtifact
{
    public ResourceArtifact(FileInfo path, VeinProject project)
    {
        base.Kind = ArtifactKind.RESOURCES;
        base.Path = path;
        base.Project = project;
    }
}
