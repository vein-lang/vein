namespace ishtar.debugger;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;


using Thread = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.Thread;

internal class IshtarThread
{
    private Stack<IshtarStackFrame> frames;

    internal IshtarThread(int id, string name)
    {
        this.Id = id;
        this.Name = name;

        this.frames = new Stack<IshtarStackFrame>();
    }

    internal int Id { get; private set; }
    internal string Name { get; private set; }

    internal IReadOnlyCollection<IshtarStackFrame> StackFrames
        => this.frames;

    internal void PushStackFrame(IshtarStackFrame frame)
        => this.frames.Push(frame);

    internal IshtarStackFrame PopStackFrame()
        => this.frames.Pop();

    internal IshtarStackFrame GetTopStackFrame()
    {
        if (this.frames.Any())
        {
            return this.frames.Peek();
        }

        return null;
    }

    internal void Invalidate()
    {
        foreach (IshtarStackFrame stackFrame in this.frames)
        {
            stackFrame.Invalidate();
        }
    }

    #region Protocol Implementation

    internal Thread GetProtocolThread() =>
        new Thread(
            id: this.Id,
            name: this.Name);

    internal StackTraceResponse HandleStackTraceRequest(StackTraceArguments arguments)
    {
        IEnumerable<IshtarStackFrame> enumFrames = this.frames;

        if (arguments.StartFrame.HasValue)
        {
            enumFrames = enumFrames.Skip(arguments.StartFrame.Value);
        }

        if (arguments.Levels.HasValue)
        {
            enumFrames = enumFrames.Take(arguments.Levels.Value);
        }

        List<StackFrame> stackFrames = enumFrames.Select(f => f.GetProtocolObject(arguments.Format)).ToList();

        return new StackTraceResponse(
            stackFrames: stackFrames,
            totalFrames: this.frames.Count);
    }

    #endregion
}
