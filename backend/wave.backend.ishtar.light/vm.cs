namespace ishtar
{
    using System;
    using System.Runtime.InteropServices;
    using wave.runtime;
    using static OpCodeValue;
    using static wave.runtime.WaveTypeCode;

    public delegate void A_OperationDelegate<T>(ref T t1, ref T t2);

    public static unsafe partial class VM
    {
        public static NativeException VMException { get; set; }

        public static void FastFail(WNE type, string msg, CallFrame frame = null) 
            => VMException = new NativeException { code = type, msg = msg, frame = frame };


        public static void ValidateLastError()
        {
            if (VMException is not null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                println($"native exception was thrown.\n\t" +
                        $"[{VMException.code}]\n\t" +
                        $"'{VMException.msg}'");
                Console.ForegroundColor = ConsoleColor.White;
                shutdown();
            }
        }

        public static void println(string str)
        {
            return;
            Console.WriteLine(str);
        }

        public static void shutdown(int exitCode = -1) => Environment.Exit(exitCode);

        public static unsafe void exec_method_native(CallFrame frame)
        {
            var caller = (delegate*<CallFrame, IshtarObject**, IshtarObject*>) 
                frame.method.PIInfo.Addr;
            var args_len = frame.method.ArgLength;
            var args = (IshtarObject**)Marshal.AllocHGlobal(sizeof(IshtarObject*) * args_len);

            if (args == null)
            {
                FastFail(WNE.OUT_OF_MEMORY, "Cannot apply boxing memory.");
                ValidateLastError();
                return;
            }
            
            for (var i = 0; i != args_len; i++)
                args[i] = IshtarGC.WrapValue(frame, &frame.args[i]);

            var result = caller(frame, args);

            Marshal.FreeHGlobal((nint)args);

            if (frame.method.ReturnType.TypeCode == TYPE_VOID)
                return;
            frame.returnValue = IshtarGC.AllocValue();
            frame.returnValue->type = frame.method.ReturnType.TypeCode;
            frame.returnValue->data.p = (nint)result;
        }

        public static unsafe void exec_method(CallFrame invocation)
        {
            var _module = invocation.method.Owner.Owner;
            var mh = invocation.method.Header;
            var args = invocation.args;

            var locals = default(stackval*);
            
            
            var ip = mh.code;
            fixed (stackval* p = GC.AllocateArray<stackval>(mh.max_stack, true))
                invocation.stack = p;
            fixed (stackval* p = new stackval[0])
                locals = p;
            var stack = invocation.stack;
            var sp = stack;
            var start = (ip + 1) - 1;
            var end = mh.code + mh.code_size;

            void jump_now() => ip = start + mh.labels_map[mh.labels[(int)*ip]].pos - 1;

            while (true)
            {
                println($"@@.{((OpCodeValue)(ushort)(*ip))} 0x{(nint)ip:X}");
                ValidateLastError();

                if (invocation.exception is not null && invocation.level == 0)
                    return;
                if (ip == end)
                {
                    FastFail(WNE.END_EXECUTE_MEMORY, "unexpected end of executable memory.");
                    continue;
                }

                switch ((OpCodeValue)(ushort)*ip)
                {
                    case NOP:
                        ++ip;
                        break;
                    case ADD:
                        ++ip;
                        --sp;
                        A_OP(sp, 0, ip);
                        break;
                    case SUB:
                        ++ip;
                        --sp;
                        A_OP(sp, 1, ip);
                        break;
                    case MUL:
                        ++ip;
                        --sp;
                        A_OP(sp, 2, ip);
                        break;
                    case DIV:
                        ++ip;
                        --sp;
                        
                        A_OP(sp, 3, ip);
                        break;
                    case DUP:
                        * sp = sp[-1];
                        ++sp;
                        ++ip;
                        break;
                    case LDARG_0:
                    case LDARG_1:
                    case LDARG_2:
                    case LDARG_3:
                    case LDARG_4:
                        *sp = args[(*ip) - (short)LDARG_0];

                        #if DEBUG_IL
                        printf("load from args -> %s %d\n", VAL_NAMES[sp->type], sp->data.i);
                        #endif

                        ++sp;
                        ++ip;
                        break;
                    case LDC_I4_0:
                    case LDC_I4_1:
                    case LDC_I4_2:
                    case LDC_I4_3:
                    case LDC_I4_5:
                        sp->type = TYPE_I4;
                        sp->data.i = (int)(*ip) - (int)LDC_I4_0;
                        ++ip;
                        ++sp;
                        break;

                    case LDC_I8_0:
                    case LDC_I8_1:
                    case LDC_I8_2:
                    case LDC_I8_3:
                    case LDC_I8_5:
                        sp->type = TYPE_I8;
                        sp->data.l = (*ip) - (long)LDC_I8_0;
                        ++sp;
                        ++ip;
                        break;
                    case LDC_I4_S:
                        ++ip;
                        sp->type = TYPE_I4;
                        sp->data.i = (int)(*ip);
                        ++ip;
                        ++sp;
                        break;
                    case LDC_I8_S:
                        ++ip;
                        sp->type = TYPE_I8;
                        var t1 = *ip;
                        ++ip;
                        var t2 = *ip;
                        sp->data.l = t2 << 32 | t1 & 0xffffffffL; 
                        ++ip;
                        ++sp;
                        break;
                    case RET:
                        ++ip;
                        --sp;
                        invocation.returnValue = &*sp;
                        stack = null;
                        locals = null;
                        return;
                    case LDNULL:
                        sp->type = TYPE_OBJECT;
                        ++sp;
                        break;
                    case THROW:
                        break;
                    case NEWOBJ:
                    {
                        ++ip;
                        sp->type = TYPE_CLASS;
                        sp->data.p = (nint)
                        IshtarGC.AllocObject(
                            (RuntimeIshtarClass) // TODO optimize search
                            _module.FindType(_module.GetTypeNameByIndex((int)*ip), true));
                        ++ip;
                        ++sp;
                    } break;
                    case CALL:
                    {
                        ++ip;
                        var callctx = (CallContext)(*ip);
                        if (callctx == CallContext.THIS_CALL)
                        {
                            var child_frame = new CallFrame();
                            ++ip;
                            var tokenIdx = *ip;
                            var owner = readTypeName(*++ip, _module);
                            var method = GetMethod(tokenIdx, owner, _module);
                            #if DEBUG_IL
                            printf("%%call %ws self function.\n", method->Name.c_str());
                            #endif


                            var method_args = stackval.Alloc(method.ArgLength);
                            for (var i = 0; i != method.ArgLength; i++)
                            {
                                var _a = method.Arguments[i];
                                // TODO, type eq validate
                                --sp;
                                method_args[i] = *sp;
                            }
                            child_frame.level = invocation.level + 1;
                            child_frame.parent = invocation;
                            fixed(stackval* p = method_args)
                                child_frame.args = p;
                            child_frame.method = method;
                            if (method.IsExtern)
                                exec_method_native(child_frame);
                            else
                                exec_method(child_frame);

                            if (child_frame.exception is not null)
                            {
                                invocation.exception = child_frame.exception;
                                method_args = null;
                                child_frame = null;
                                break;
                            }
                            if (method.ReturnType.TypeCode != TYPE_VOID)
                            {
                                if (child_frame.returnValue is null)
                                {
                                    FastFail(WNE.STATE_CORRUPT, "Method has return zero memory.");
                                    continue;
                                }
                                *sp = *child_frame.returnValue;
                                sp++;
                            }
                            method_args = null;
                            child_frame = null;
                            break;
                        }

                        throw new NotImplementedException();
                    } break;
                    case LOC_INIT:
                    {
                        ++ip;
                        var locals_size = *ip;
                        fixed(stackval* p = stackval.Alloc((int)locals_size))
                            locals = p;
                        ++ip;
                        for (var i = 0u; i != locals_size; i++)
                        {
                            var type_name = readTypeName(*++ip, _module);
                            var type = _module.FindType(type_name, true);
                            if (type.IsPrimitive)
                                locals[i].type = type.TypeCode;
                            else
                            {
                                locals[i].type = TYPE_OBJECT;
                                locals[i].data.p = new IntPtr(0);
                            }

                            ++ip;
                        }
                    }
                    break;
                    case JMP_L:
                    {
                        ++ip;
                        --sp;
                        var first = *sp;
                        --sp;
                        var second = *sp;

                        if (first.type == second.type)
                        {
                            switch (first.type)
                            {
                                case TYPE_I1:
                                    if (first.data.b < second.data.b)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U1:
                                    if (first.data.ub < second.data.ub)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I2:
                                    if (first.data.s < second.data.s)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U2:
                                    if (first.data.us < second.data.us)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I4:
                                    if (first.data.i < second.data.i)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U4:
                                    if (first.data.ui < second.data.ui)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I8:
                                    if (first.data.l < second.data.l)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U8:
                                    if (first.data.ul < second.data.ul)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R2:
                                    if (first.data.hf < second.data.hf)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R4:
                                    if (first.data.f_r4 < second.data.f_r4)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R8:
                                    if (first.data.f < second.data.f)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R16:
                                    if (first.data.d < second.data.d)
                                        jump_now();
                                    else ++ip; break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        else
                            throw new NotImplementedException();
                    } break;
                    case JMP_T:
                    {
                        ++ip;
                        --sp;
                        var first = *sp;
                            
                        switch (first.type)
                            {
                                case TYPE_I1:
                                    if (first.data.b != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U1:
                                    if (first.data.ub != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I2:
                                    if (first.data.s != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U2:
                                    if (first.data.us != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I4:
                                    if (first.data.i != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U4:
                                    if (first.data.ui != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I8:
                                    if (first.data.l != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U8:
                                    if (first.data.ul != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R2:
                                    if ((float)first.data.hf != 0.0f)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R4:
                                    if (first.data.f_r4 != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R8:
                                    if (first.data.f != 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R16:
                                    if (first.data.d != 0)
                                        jump_now();
                                    else ++ip; break;
                                default:
                                    throw new NotImplementedException();
                            }
                    } break;
                    case JMP_LQ:
                    {
                        ++ip;
                        --sp;
                        var first = *sp;
                        --sp;
                        var second = *sp;

                        if (first.type == second.type)
                        {
                            switch (first.type)
                            {
                                case TYPE_I1:
                                    if (first.data.b <= second.data.b)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U1:
                                    if (first.data.ub <= second.data.ub)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I2:
                                    if (first.data.s <= second.data.s)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U2:
                                    if (first.data.us <= second.data.us)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I4:
                                    if (first.data.i <= second.data.i)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U4:
                                    if (first.data.ui <= second.data.ui)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I8:
                                    if (first.data.l <= second.data.l)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U8:
                                    if (first.data.ul <= second.data.ul)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R2:
                                    if (first.data.hf <= second.data.hf)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R4:
                                    if (first.data.f_r4 <= second.data.f_r4)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R8:
                                    if (first.data.f <= second.data.f)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R16:
                                    if (first.data.d <= second.data.d)
                                        jump_now();
                                    else ++ip; break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        else
                            throw new NotImplementedException();
                    } break;
                    case JMP_NN:
                    {
                        ++ip;
                        --sp;
                        var first = *sp;
                        --sp;
                        var second = *sp;

                        if (first.type == second.type)
                        {
                            switch (first.type)
                            {
                                case TYPE_I1:
                                    if (first.data.b != second.data.b)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U1:
                                    if (first.data.ub != second.data.ub)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I2:
                                    if (first.data.s != second.data.s)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U2:
                                    if (first.data.us != second.data.us)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I4:
                                    if (first.data.i != second.data.i)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U4:
                                    if (first.data.ui != second.data.ui)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I8:
                                    if (first.data.l != second.data.l)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U8:
                                    if (first.data.ul != second.data.ul)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R2:
                                    if (first.data.hf != second.data.hf)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R4:
                                    if (first.data.f_r4 != second.data.f_r4)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R8:
                                    if (first.data.f != second.data.f)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R16:
                                    if (first.data.d != second.data.d)
                                        jump_now();
                                    else ++ip; break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        else
                            throw new NotImplementedException();
                    } break;
                    case JMP:
                        ++ip;
                        jump_now();
                        break;
                    case LDLOC_0:
                    case LDLOC_1:
                    case LDLOC_2:
                    case LDLOC_3:
                    case LDLOC_4:
                        * sp = locals[(*ip) - (int)LDLOC_0];
                        #if DEBUG_IL
                        printf("load from locals -> %s %d\n", VAL_NAMES[sp->type], sp->data.i);
                        #endif
                        ++ip;
                        ++sp;
                        break;
                    case STLOC_0:
                    case STLOC_1:
                    case STLOC_2:
                    case STLOC_3:
                    case STLOC_4:
                        --sp;
                        locals[(*ip) - (int)STLOC_0] = *sp;
                        #if DEBUG_IL
                        printf("load from locals -> %s %d\n", VAL_NAMES[sp->type], sp->data.i);
                        #endif
                        ++ip;
                        break;
                    case RESERVED_0:
                        ++ip;
                        println($"*** DUMP ***");
                        println($"\tsp[-1] {sp[0].type}");
                        println($"\tsp[-1] {sp[0].data.l} {sp[0].data.l:X8}");
                        break;
                    case RESERVED_1:
                        ++ip;
                        println($"*** DUMP ***");
                        println($"\tsp[-1] {sp[0].type}");
                        println($"\tsp[-1] {sp[0].data.l} {sp[0].data.l:X8}");
                        println("*** BREAKED ***");
                        Console.ReadKey();
                        break;
                    case RESERVED_2:
                        ++ip;
                        println($"*** GC DUMP ***");
                        println($"\talive_objects: {IshtarGC.GCStats.alive_objects}");
                        println($"\ttotal_allocations: {IshtarGC.GCStats.total_allocations}");
                        println($"\ttotal_bytes_requested: {IshtarGC.GCStats.total_bytes_requested}");
                        println($"*** END GC DUMP ***");
                        break;
                    case LDC_STR:
                    {
                        ++ip;
                        sp->type = TYPE_STRING;
                        var str = _module.GetConstStringByIndex((int) *ip);
                        sp->data.p = (IntPtr)IshtarGC.AllocString(str);
                        ++sp;
                        ++ip;

                    } break;
                    default:
                        CallFrame.FillStackTrace(invocation);

                        FastFail(WNE.STATE_CORRUPT, $"Unknown opcode: {*ip}\n" +
                                                                    $"{ip - start}\n" +
                                                                    $"{invocation.exception.stack_trace}");
                        ++ip;
                        break;
                }
            }
        }



        public static void Assert(bool conditional, WNE type, string msg, CallFrame frame = null)
        {
            if (conditional)
                return;
            FastFail(type, msg, frame);
            ValidateLastError();
        }
    }

    public class WaveObject
    {
        public WaveClass clazz;
    };
    
    public struct stackval
    {
        public stack_union data;
        public WaveTypeCode type;

        public static stackval[] Alloc(int size) => GC.AllocateArray<stackval>(size, true);
    }
    [StructLayout(LayoutKind.Explicit)] 
    public unsafe struct stack_union
    {
        [FieldOffset(0)] public sbyte b;
        [FieldOffset(0)] public short s;
        [FieldOffset(0)] public int i;
        [FieldOffset(0)] public long l;
        [FieldOffset(0)] public byte ub;
        [FieldOffset(0)] public ushort us;
        [FieldOffset(0)] public uint ui;
        [FieldOffset(0)] public ulong ul;
        [FieldOffset(0)] public float f_r4;
        [FieldOffset(0)] public double f;
        [FieldOffset(0)] public decimal d;
        [FieldOffset(0)] public Half hf;
        [FieldOffset(0)] public IntPtr p;
    }
}