namespace ishtar
{
    using System.Runtime.CompilerServices;
    using System.Text;

    public static class CallFrameEx
    {
        public static IshtarGC GetGC(this CallFrame frame) => frame.vm.GC;
    }

    public unsafe class CallFrame(VM vm)
    {
        public CallFrame parent;
        public RuntimeIshtarMethod method;
        public stackval* returnValue;
        public stackval* args;
        public stackval* stack;
        public OpCodeValue last_ip;
        public int level;
        public VM vm = vm;

        public CallFrameException exception;


        public void assert(bool conditional, [CallerArgumentExpression("conditional")] string msg = default)
        {
            if (conditional)
                return;
            vm.FastFail(WNE.STATE_CORRUPT, $"Static assert failed: '{msg}'", this);
        }
        public void assert(bool conditional, WNE type, [CallerArgumentExpression("conditional")] string msg = default)
        {
            if (conditional)
                return;
            vm.FastFail(type, $"Static assert failed: '{msg}'", this);
        }


        public void ThrowException(RuntimeIshtarClass @class) =>
            this.exception = new CallFrameException()
            {
                value = vm.GC.AllocObject(@class)
            };

        public void ThrowException(RuntimeIshtarClass @class, string message)
        {
            this.exception = new CallFrameException()
            {
                value = vm.GC.AllocObject(@class)
            };

            if (@class.FindField("message") is null)
                throw new InvalidOperationException($"Class '{@class.FullName}' is not contained 'message' field.");

            this.exception.value->vtable[@class.Field["message"].vtable_offset]
                = vm.GC.ToIshtarObject(message);
        }


        public static void FillStackTrace(CallFrame frame)
        {
            var str = new StringBuilder();

            if (frame is null)
            {
                Console.WriteLine($"<<DETECTED NULL FRAME>>");
                return;
            }

            if (frame.method.Owner is not null)
                str.AppendLine($"\tat {frame.method.Owner.FullName.NameWithNS}.{frame.method.Name}");
            else
                str.AppendLine($"\tat <sys>.{frame.method.Name}");

            var r = frame.parent;

            while (r != null)
            {
                str.AppendLine($"\tat {r.method.Owner.FullName.NameWithNS}.{r.method.Name}");
                r = r.parent;
            }

            frame.exception ??= new CallFrameException();
            frame.exception.stack_trace = str.ToString();
        }
    }
}
