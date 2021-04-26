﻿namespace ishtar
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.CodeAnalysis;
    using wave.runtime;
    using static OpCodeValue;
    using static wave.runtime.WaveTypeCode;


    public delegate void A_OperationDelegate<T>(ref T t1, ref T t2);

    public static unsafe partial class VM
    {
        public static NativeException VMException { get; set; }

        public static void FastFail(WaveNativeException type, string msg, CallFrame frame = null) 
            => VMException = new NativeException { code = type, msg = msg, frame = frame };


        public static void println(string str)
        {
            Console.WriteLine(str);
        }

        public static void vm_shutdown()
        {

        }

        public static unsafe void exec_method(CallFrame invocation)
        {
            var _module = invocation.method.Owner.Owner;
            var mh = invocation.method.Header;
            var args = invocation.args;

            var locals = default(stackval*);
            
            
            var ip = mh.code;

            fixed(stackval* p = new stackval[mh.max_stack])
                invocation.stack = p;
            fixed (stackval* p = new stackval[0])
                locals = p;

            var stack = invocation.stack;
            var sp = stack;
            var start = (ip + 1) - 1;
            var end = mh.code + mh.code_size;

            while (true)
            {
                //println($".{((OpCodeValue)(ushort)(*ip))}");
                if (VMException is not null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    println($"native exception was thrown.\n\t" +
                            $"[{VMException.code}]\n\t" +
                            $"'{VMException.msg}'");
                    Console.ForegroundColor = ConsoleColor.White;
                    vm_shutdown();
                    return;
                }
                if (ip == end)
                {
                    FastFail(WaveNativeException.END_EXECUTE_MEMORY, "unexpected end of executable memory.");
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
                        sp->data.l = (long)(*ip); // TODO bug
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
                            var method_args = new stackval[method.ArgLength];
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

                            exec_method(child_frame);

                            if (child_frame.exception is not null)
                            {
                                println($"unhandled exception was thrown. \n" +
                                        $"[{child_frame.exception.value.clazz.FullName}] null reference exception\n" +
                                        $"{child_frame.exception.stack_trace}");
                                vm_shutdown();
                                return;
                            }
                            if (method.ReturnType.TypeCode != WaveTypeCode.TYPE_VOID)
                            {
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
                        fixed(stackval* p = new stackval[(int)locals_size])
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

                        void jump_now() => ip = start + mh.labels_map[mh.labels[(int)*ip]].pos;

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

                        void jump_now() => ip = start + mh.labels_map[mh.labels[(int)*ip]].pos;

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

                        void jump_now() => ip = start + mh.labels_map[mh.labels[(int)*ip]].pos;

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

                        void jump_now() => ip = start + mh.labels_map[mh.labels[(int)*ip]].pos;

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
                        ip = start + mh.labels_map[mh.labels[(int) *ip]].pos;
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
                    case DUMP_0:
                        ++ip;
                        println($"*** DUMP ***");
                        println($"sp[-1] {sp[0].type}");
                        println($"sp[-1] {sp[0].data.l} {sp[0].data.l:X8}");
                        break;
                    default:
                        ++ip;
                        FastFail(WaveNativeException.STATE_CORRUPT, $"Unknown opcode: {*ip}");
                        break;
                }
            }
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
    }
    [StructLayout(LayoutKind.Explicit)] 
    public struct stack_union
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