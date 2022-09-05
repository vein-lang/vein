namespace vein.compilation;

using System.Collections.Generic;

public class CompilationLog
{
    public Queue<string> Info { get; } = new();
    public Queue<string> Warn { get; } = new();
    public Queue<string> Error { get; } = new();
}
