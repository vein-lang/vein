namespace ishtar;

using System.Threading;

[ExcludeFromCodeCoverage]
public unsafe class DefaultWatchDog(VirtualMachine vm) : IWatchDog
{
    private static readonly object guarder = new();

    void IWatchDog.FastFail(WNE type, string msg, CallFrame* frame)
    {
        lock (guarder)
        {
            var result = new NativeException {code = type, msg = msg, frame = frame};
            Interlocked.Exchange(ref vm.CurrentException, result);
        }
    }
    void IWatchDog.ValidateLastError()
    {
        lock (guarder)
        {
            if (vm.CurrentException is null)
                return;

            CallFrame.FillStackTrace(vm.CurrentException.frame);
            Console.ForegroundColor = ConsoleColor.Red;
            var err = $"native exception was thrown.\n\t" +
                      $"[{vm.CurrentException.code}]\n\t" +
                      $"'{vm.CurrentException.msg}'";
            if (vm.CurrentException is not null && vm.CurrentException.frame is not null && !vm.CurrentException.frame->exception.IsDefault())
                err += $"\n{vm.CurrentException.frame->exception.GetStackTrace()}";
            vm.println(err);
            Console.ForegroundColor = ConsoleColor.White;
            vm.halt();
        }
    }
}
