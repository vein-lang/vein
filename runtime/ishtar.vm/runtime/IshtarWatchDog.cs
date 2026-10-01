namespace ishtar;

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using runtime.gc;

[ExcludeFromCodeCoverage]
public readonly unsafe struct IshtarWatchDog(VirtualMachine* vm)
{
    private static readonly object guarder = new();

    /// <summary>
    /// DEBUG/test-only switch. When enabled, a fatal VM fault is re-thrown as a managed
    /// <see cref="IshtarFaultException"/> from <see cref="ValidateLastError"/> instead of terminating
    /// the process via <c>Environment.Exit</c> (see <see cref="VirtualMachine.halt"/>).
    /// <para/>
    /// Test runners such as NUnit run the VM in-process, swallow its stdout, and report only
    /// "test host process crashed" when the VM calls <c>Environment.Exit</c> — hiding the real reason.
    /// Turning this on lets the fault surface as an ordinary test failure carrying the full message.
    /// The interception is compiled in only for DEBUG builds; the flag is a no-op in Release.
    /// </summary>
    public static bool CaptureFaultsAsExceptions;

    public void FastFail(WNE type, string msg, CallFrame* frame)
    {
        lock (guarder)
        {
            var result = IshtarGC.AllocateImmortal<IshtarMasterFault>(frame);
            *result = new (type, StringStorage.Intern(msg, frame), frame);

            if (vm->currentFault is null)
            {
                vm->currentFault = result;
                return;
            }

            Interlocked.Exchange(ref Unsafe.AsRef<nint>(vm->currentFault), (nint)result);
        }
    }
    public void ValidateLastError()
    {
        lock (guarder)
        {
            if (vm->currentFault is null)
                return;
            var exception = vm->currentFault;

            CallFrame.FillStackTrace(exception->frame);
            var err = $"\u001b[31mnative exception was thrown.\n\t" +
                      $"[\u001b[33m{exception->code}\u001b[31m]\n\t" +
                      $"'{StringStorage.GetStringUnsafe(exception->msg)}'";
            if (exception is not null && exception->frame is not null && !exception->frame->exception.IsDefault())
                err += $"\n{exception->frame->exception.GetStackTrace()}\u001b[0m";
            vm->trace.console_std_write_line(err);
            Console.ResetColor();

#if DEBUG
            if (CaptureFaultsAsExceptions)
            {
                var code = exception->code;
                var detail = StringStorage.GetStringUnsafe(exception->msg);
                if (exception->frame is not null && !exception->frame->exception.IsDefault())
                    detail += $"\n{exception->frame->exception.GetStackTrace()}";
                vm->currentFault = null; // clear so the surfaced exception is not re-reported on re-entry
                throw new IshtarFaultException(code, detail);
            }
#endif
            vm->halt();
        }
    }
}

/// <summary>
/// Managed representation of a fatal VM fault, surfaced (DEBUG-only) by
/// <see cref="IshtarWatchDog.CaptureFaultsAsExceptions"/> so test runners can observe the cause
/// instead of seeing the host process exit.
/// </summary>
public sealed class IshtarFaultException(WNE code, string detail)
    : Exception($"[{code}] {detail}")
{
    public WNE Code { get; } = code;
}
