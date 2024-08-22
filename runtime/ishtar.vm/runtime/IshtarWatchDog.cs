namespace ishtar;

using System.Threading;
using runtime.gc;

[ExcludeFromCodeCoverage]
public readonly unsafe struct IshtarWatchDog(VirtualMachine* vm)
{
    private static readonly object guarder = new();

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
            vm->halt();
        }
    }
}
