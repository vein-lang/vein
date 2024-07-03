namespace ishtar
{
    using System.Runtime.CompilerServices;
    using System.Text;
    using runtime.gc;

    public static class CallFrameEx
    {
        public static IshtarGC GetGC(this CallFrame frame) => frame.vm.GC;
    }

    public unsafe struct CallFrame : IDisposable
    {
        public CallFrame(RuntimeIshtarMethod* m, CallFrame* parent, CallFrame* self)
        {
            this.Self = self;
            this.method = m;
            this.parent = parent;
            this.level = 0;

            if (parent is not null)
                this.level = parent->level + 1;
        }

        public readonly CallFrame* Self ;
        public readonly CallFrame* parent;
        public readonly RuntimeIshtarMethod* method;
        public readonly int level;
        public SmartPointer<stackval> returnValue;
        public stackval* args;
        public OpCodeValue last_ip;
        public VirtualMachine vm => method->Owner->Owner->vm;

        public CallFrameException exception;

        public CallFrame* CreateChild(RuntimeIshtarMethod* m) => Create(m, Self);

        public static CallFrame* Create(RuntimeIshtarMethod* method, CallFrame* parent)
        {
            var frame = IshtarGC.AllocateImmortal<CallFrame>(parent);

            *frame = new CallFrame(method, parent, frame);

            return frame;
        }

        private static void Free(CallFrame* frame) => IshtarGC.FreeImmortal(frame);


        public void assert(bool conditional, [CallerArgumentExpression("conditional")] string msg = default)
        {
            if (conditional)
                return;
            vm.FastFail(WNE.STATE_CORRUPT, $"Static assert failed: '{msg}'", Self);
        }
        public void assert(bool conditional, WNE type, [CallerArgumentExpression("conditional")] string msg = default)
        {
            if (conditional)
                return;
            vm.FastFail(type, $"Static assert failed: '{msg}'", Self);
        }


        public void ThrowException(RuntimeIshtarClass* @class) =>
            this.exception = new CallFrameException()
            {
                value = vm.GC.AllocObject(@class, Self)
            };

        public void ThrowException(RuntimeIshtarClass* @class, string message)
        {
            this.exception = new CallFrameException()
            {
                value = vm.GC.AllocObject(@class, Self)
            };

            if (@class->FindField("message") is null)
                throw new InvalidOperationException($"Class '{@class->FullName->NameWithNS}' is not contained 'message' field.");

            this.exception.value->vtable[@class->Field["message"]->vtable_offset]
                = vm.GC.ToIshtarObject(message, Self);
        }


        public static void FillStackTrace(CallFrame* frame)
        {
            var str = new StringBuilder();

            if (frame is null)
            {
                throw new Exception($"<<DETECTED NULL FRAME>>");
                return;
            }

            if (frame->method != null && !frame->method->IsDisposed() && frame->method->Owner is not null && frame->method->Owner->FullName is not null)
                str.AppendLine($"\tat {frame->method->Owner->FullName->NameWithNS}.{frame->method->Name}");
            else if (frame->method is not null && !frame->method->IsDisposed())
                str.AppendLine($"\tat <sys>.{frame->method->Name}");
            else
                str.AppendLine($"\tat <sys>.ukn+0");


            var r = frame->parent;
            var index = 0;
            while (r != null)
            {
                if (r->method is not null && !r->method->IsDisposed() && r->method->Owner is not null && r->method->Owner->FullName is not null)
                    str.AppendLine($"\tat {r->method->Owner->FullName->NameWithNS}.{r->method->Name}");
                else if (r->method is not null && !r->method->IsDisposed())
                    str.AppendLine($"\tat sys.{r->method->Name}");
                else
                    str.AppendLine($"\tat <sys>.ukn+{++index}");

                r = r->parent;
            }
            if (frame->exception.stack_trace is null)
                frame->exception.stack_trace = StringStorage.Intern(str.ToString(), frame);
        }

        public void Dispose() => Free(Self);
    }
}
