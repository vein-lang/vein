namespace ishtar
{
    using System.Runtime.CompilerServices;
    using System.Text;

    public unsafe class CallFrame
    {
        public CallFrame parent;
        public RuntimeIshtarMethod method;
        public stackval* returnValue;
        public stackval* args;
        public stackval* stack;
        public OpCodeValue last_ip;
        public int level;

        public CallFrameException exception;


        public void assert(bool conditional, [CallerArgumentExpression("conditional")] string msg = default)
        {
            if (conditional)
                return;
            VM.FastFail(WNE.STATE_CORRUPT, $"Static assert failed: '{msg}'", this);
            VM.ValidateLastError();
        }
        public void assert(bool conditional, WNE type, [CallerArgumentExpression("conditional")] string msg = default)
        {
            if (conditional)
                return;
            VM.FastFail(type, $"Static assert failed: '{msg}'", this);
            VM.ValidateLastError();
        }


        public static void FillStackTrace(CallFrame frame)
        {
            var str = new StringBuilder();

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
