namespace ishtar
{
    using System.Runtime.InteropServices;
    using System.Text;
    using io;
    using runtime.gc;

    public static unsafe class CallFrameEx
    {
        public static IshtarGC* GetGC(this CallFrame frame) => frame.vm->gc;
    }

    [CTypeExport("call_frame_t")]
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

        public readonly CallFrame* Self;
        public readonly CallFrame* parent;
        public readonly RuntimeIshtarMethod* method;
        public readonly int level;
        public SmartPointer<stackval> returnValue;
        public stackval* args;
        public OpCodeValue last_ip;
        public VirtualMachine* vm => method->Owner->Owner->vm;

        /// <summary>
        /// For async methods: the IshtarJob that this method will resolve on RET.
        /// Null for synchronous methods.
        /// </summary>
        public IshtarAsyncJob* asyncJob;

        /// <summary>
        /// Non-null when this frame is being resumed after suspension.
        /// Contains the saved IP, stack snapshot, and locals to restore.
        /// </summary>
        public SuspendedFrame* resumeState;

        /// <summary>
        /// The result from the awaited Job that triggered resumption.
        /// Pushed onto the eval stack after state is restored.
        /// </summary>
        public stackval awaitResult;

        public CallFrameException exception;

        public byte* _unsafeStack;
        public int _unsafeStackUsed;
        private const int UnsafeStackSize = 65536;

public void* AllocUnsafeStack(int size)
{
    if (size <= 0)
    {
        vm->FastFail(WNE.STATE_CORRUPT, $"unsafe stackalloc invalid size {size}", Self);
        return null;
    }

    if (_unsafeStack == null)
    {
        _unsafeStack = (byte*)NativeMemory.Alloc(UnsafeStackSize);
        if (_unsafeStack == null)
        {
            vm->FastFail(WNE.OUT_OF_MEMORY, "unsafe stack allocation failed", Self);
            return null;
        }
    }

    // Ensure at least pointer-size alignment for safe typed loads/stores (e.g., on ARM64)
    // (sizeof(nint) is platform-dependent, so this cannot be a compile-time const)
    int alignment = sizeof(nint);
    var alignedUsed = (_unsafeStackUsed + (alignment - 1)) & ~(alignment - 1);

    // Use subtraction to avoid `_unsafeStackUsed + size` overflow
    if ((uint)size > UnsafeStackSize || (uint)alignedUsed > (uint)(UnsafeStackSize - size))
    {
        vm->FastFail(WNE.OVERFLOW, "unsafe stack overflow: per-frame limit exceeded", Self);
        return null;
    }

    var ptr = _unsafeStack + alignedUsed;
    _unsafeStackUsed = alignedUsed + size;
    return ptr;
}

        private void FreeUnsafeStack()
        {
            if (_unsafeStack != null)
                NativeMemory.Free(_unsafeStack);
            _unsafeStack = null;
            _unsafeStackUsed = 0;
        }

        public CallFrame* CreateChild(RuntimeIshtarMethod* m) => Create(m, Self);

        public static CallFrame* Create(RuntimeIshtarMethod* method, CallFrame* parent)
        {
            var frame = IshtarGC.AllocateImmortal<CallFrame>(parent);

            *frame = new CallFrame(method, parent, frame);

            return frame;
        }

        public static CallFrame* Create(RuntimeIshtarMethod* method)
        {
            var frame = IshtarGC.AllocateImmortal<CallFrame>(null);

            *frame = new CallFrame(method, null, frame);

            return frame;
        }

        private static void Free(CallFrame* frame) => IshtarGC.FreeImmortal(frame);


        public void assert(bool conditional, [CallerArgumentExpression("conditional")] string msg = default)
        {
            if (conditional)
                return;
            vm->FastFail(WNE.STATE_CORRUPT, $"Static assert failed: '{msg}'", Self);
        }
        public void assert(bool conditional, WNE type, [CallerArgumentExpression("conditional")] string msg = default)
        {
            if (conditional)
                return;
            vm->FastFail(type, $"Static assert failed: '{msg}'", Self);
        }


        public void ThrowException(RuntimeIshtarClass* @class) =>
            this.exception = new CallFrameException()
            {
                value = vm->gc->AllocObject(@class, Self)
            };

        public void ThrowException(RuntimeIshtarClass* @class, string message)
        {
            this.exception = new CallFrameException()
            {
                value = vm->gc->AllocObject(@class, Self)
            };

            if (@class->FindField("message") is null)
                throw new InvalidOperationException($"Class '{@class->FullName->NameWithNS}' is not contained 'message' field.");

            this.exception.value->vtable[@class->Field["message"]->vtable_offset]
                = vm->gc->ToIshtarObject(message, Self);
        }


        public static void FillStackTrace(CallFrame* frame)
        {
            var str = new StringBuilder();

            if (frame is null)
                throw new Exception($"<<DETECTED NULL FRAME>>");

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

        public void Dispose()
        {
            FreeUnsafeStack();
            Free(Self);
        }
    }
}
