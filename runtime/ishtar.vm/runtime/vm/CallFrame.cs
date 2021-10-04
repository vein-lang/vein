namespace ishtar
{
    using System.Text;

    public unsafe class CallFrame
    {
        public CallFrame parent;
        public RuntimeIshtarMethod method;
        public stackval* returnValue;
        public IshtarObject* _this_;
        public stackval* args;
        public stackval* stack;
        public OpCodeValue last_ip;
        public int level;

        public CallFrameException exception;


        public static void FillStackTrace(CallFrame frame)
        {
            var str = new StringBuilder();

            str.AppendLine($"\tat {frame.method.Owner.FullName.NameWithNS}.{frame.method.Name}");

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
