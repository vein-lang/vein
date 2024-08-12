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
            Console.ForegroundColor = ConsoleColor.Red;
            var err = $"native exception was thrown.\n\t" +
                      $"[{exception->code}]\n\t" +
                      $"'{StringStorage.GetStringUnsafe(exception->msg)}'";
            if (exception is not null && exception->frame is not null && !exception->frame->exception.IsDefault())
                err += $"\n{exception->frame->exception.GetStackTrace()}";
            vm->println(err);
            Console.ForegroundColor = ConsoleColor.White;
            vm->halt();
        }
    }
}
