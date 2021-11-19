namespace ishtar;


using System;
using System.Threading;

[ExcludeFromCodeCoverage]
public class DefaultWatchDog : IWatchDog
{
    private static readonly object guarder = new();
    void IWatchDog.FastFail(WNE type, string msg, CallFrame frame)
    {
        lock (guarder)
        {
            var result = new NativeException {code = type, msg = msg, frame = frame};
            Interlocked.Exchange(ref VM.CurrentException, result);
        }
    }

    void IWatchDog.ValidateLastError()
    {
        lock (guarder)
        {
            if (VM.CurrentException is not null)
            {
                CallFrame.FillStackTrace(VM.CurrentException.frame);
                Console.ForegroundColor = ConsoleColor.Red;
                var err = $"native exception was thrown.\n\t" +
                          $"[{VM.CurrentException.code}]\n\t" +
                          $"'{VM.CurrentException.msg}'";
                if (VM.CurrentException.frame.exception is not null)
                    err += $"\n{VM.CurrentException.frame.exception.stack_trace}";
                VM.println(err);
                Console.ForegroundColor = ConsoleColor.White;
                VM.halt();
            }
        }
    }
}
