namespace vein.compilation;

using System.Collections.Generic;
using ishtar;

public class CompilationLog
{
    public Queue<string> Info { get; } = new();
    public Queue<string> Warn { get; } = new();
    public Queue<string> Error { get; } = new();
}


public class CompilationState
{
    public Queue<CompilationEventData> warnings { get; } = new();
    public Queue<CompilationEventData> errors { get; } = new();
    public Queue<CompilationEventData> infos { get; } = new();
}
