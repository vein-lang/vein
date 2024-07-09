namespace ishtar;

using System.Text;
using runtime;

internal struct IshtarTrace()
{
    private bool useConsole = Environment.GetCommandLineArgs().Contains("--sys::log::use-console=1");
    private bool useFile = Environment.GetCommandLineArgs().Contains("--sys::log::use-file=1");

    [Conditional("DEBUG")]
    public void Setup() => IshtarSharedDebugData.Setup();

    [Conditional("DEBUG")]
    public void println(string s) => IshtarSharedDebugData.TraceOutPush(s);


    [Conditional("DEBUG")]
    public void debug_stdout_write(string s) => IshtarSharedDebugData.StdOutPush(s);

    [Conditional("DEBUG")]
    public unsafe void signal_state(OpCodeValue ip, CallFrame current, TimeSpan cycleDelay, stackval currentStack)
        => IshtarSharedDebugData.SetState(new IshtarState($"{ip}", current.method->Name, cycleDelay, $"{currentStack.type}", current.level));

    public void console_std_write(string s)
    {
#if DEBUG
        debug_stdout_write(s);
#else
        Console.WriteLine(s);
#endif
    }
}
