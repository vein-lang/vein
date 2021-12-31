namespace ishtar
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using emit;
    using vein.runtime;
    using static OpCodeValue;
    using static vein.runtime.VeinTypeCode;
    using vein.extensions;
    using static WNE;

    public delegate void A_OperationDelegate<T>(ref T t1, ref T t2);

    public static unsafe partial class VM
    {
        static VM() => watcher = new DefaultWatchDog();


        public static volatile NativeException CurrentException;
        public static volatile IWatchDog watcher;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FastFail(WNE type, string msg, CallFrame frame)
        {
            watcher?.FastFail(type, msg, frame);
            watcher?.ValidateLastError();
        }
        [Obsolete]
        public static void ValidateLastError() {}

        [Conditional("DEBUG")]
        public static void println(string str) => Trace.println(str);

        public static void halt(int exitCode = -1)
            => Environment.Exit(exitCode);

        public static unsafe void exec_method_native(CallFrame frame)
        {
            var caller = (delegate*<CallFrame, IshtarObject**, IshtarObject*>)
                frame.method.PIInfo.Addr;
            var args_len = frame.method.ArgLength;
            var args = (IshtarObject**)Marshal.AllocHGlobal(sizeof(IshtarObject*) * args_len);

            if (args == null)
            {
                FastFail(OUT_OF_MEMORY, "Cannot apply boxing memory.", frame);
                ValidateLastError();
                return;
            }

            for (var i = 0; i != args_len; i++)
                args[i] = IshtarMarshal.Boxing(frame, &frame.args[i]);

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
            println($"@.frame> {invocation.method.Owner.Name}::{invocation.method.Name}");

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
            var start = ip;
            var end = mh.code + mh.code_size;
            var end_stack = stack + mh.max_stack;
            uint* endfinally_ip = null;
            var zone = default(ProtectedZone);
            void jump_now() => ip = start + mh.labels_map[mh.labels[(int)*ip]].pos - 1;
            void jump_to(int index) => ip = start + mh.labels_map[mh.labels[index]].pos - 1;
            uint* get_jumper(int index) => start + mh.labels_map[mh.labels[index]].pos - 1;

            // maybe mutable il code is bad, need research
            void ForceFail(RuntimeIshtarClass clazz)
            {
                *ip = (uint)THROW;
                sp->data.p = (nint)IshtarGC.AllocObject(clazz);
                sp->type = TYPE_CLASS;
                sp++;
            }

            while (true)
            {
                vm_cycle_start:
                invocation.last_ip = (OpCodeValue)(ushort)*ip;
                println($"@@.{invocation.last_ip} 0x{(nint)ip:X} [sp: {(((nint)stack) - ((nint)sp))}]");
                ValidateLastError();

                if (invocation.exception is not null && invocation.level == 0)
                    return;

                if (ip == end)
                {
                    FastFail(END_EXECUTE_MEMORY, "unexpected end of executable memory.", invocation);
                    continue;
                }

                if (sp >= end_stack)
                {
                    FastFail(OVERFLOW, "stack overflow error.", invocation);
                    continue;
                }

                switch (invocation.last_ip)
                {
                    case NOP:
                        ++ip;
                        break;
                    case ADD:
                        ++ip;
                        --sp;
                        A_OP(sp, 0, ip, invocation);
                        ForceFail(KnowTypes.NullPointerException(invocation));
                        break;
                    case SUB:
                        ++ip;
                        --sp;
                        A_OP(sp, 1, ip, invocation);
                        break;
                    case MUL:
                        ++ip;
                        --sp;
                        A_OP(sp, 2, ip, invocation);
                        break;
                    case DIV:
                        ++ip;
                        --sp;
                        A_OP(sp, 3, ip, invocation);
                        break;
                    case MOD:
                        ++ip;
                        --sp;
                        A_OP(sp, 4, ip, invocation);
                        break;
                    case DUP:
                        *sp = sp[-1];
                        ++sp;
                        ++ip;
                        break;
                    case LDARG_0:
                    case LDARG_1:
                    case LDARG_2:
                    case LDARG_3:
                    case LDARG_4:
                        if (args == null)
                        {
                            FastFail(OUT_OF_RANGE, $"Arguments in current function is empty, but trying access it.", invocation);
                            ValidateLastError();
                            return;
                        }
                        *sp = args[(*ip) - (short)LDARG_0];
                        println($"load from args ({sp->type})");
                        ++sp;
                        ++ip;
                        break;
                    case LDARG_S:
                        ++ip;
                        *sp = args[(*ip)];
                        println($"load from args ({sp->type})");
                        ++sp;
                        ++ip;
                        break;
                    case LDC_I2_0:
                    case LDC_I2_1:
                    case LDC_I2_2:
                    case LDC_I2_3:
                    case LDC_I2_5:
                        sp->type = TYPE_I2;
                        sp->data.i = (int)(*ip) - (int)LDC_I2_0;
                        ++ip;
                        ++sp;
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
                    case LDC_F4:
                        ++ip;
                        sp->type = TYPE_R4;
                        sp->data.f_r4 = BitConverter.Int32BitsToSingle((int)(*ip));
                        ++ip;
                        ++sp;
                        break;
                    case LDC_F8:
                        {
                            ++ip;
                            sp->type = TYPE_R8;
                            var t1 = (long)*ip;
                            ++ip;
                            var t2 = (long)*ip;
                            sp->data.f = BitConverter.Int64BitsToDouble(t2 << 32 | t1 & 0xffffffffL);
                            ++ip;
                            ++sp;
                        }
                        break;
                    case LDC_F16:
                        {
                            ++ip;
                            sp->type = TYPE_R16;
                            var bits = (int)*ip;
                            var items = new int[bits];
                            ++ip;
                            foreach (var i in ..bits)
                            {
                                items[i] = (int)*ip;
                                ++ip;
                            }
                            sp->data.d = new decimal(items);
                            ++sp;
                        }
                        break;
                    case LDC_I2_S:
                        ++ip;
                        sp->type = TYPE_I2;
                        sp->data.i = (int)(*ip);
                        ++ip;
                        ++sp;
                        break;
                    case LDC_I4_S:
                        ++ip;
                        sp->type = TYPE_I4;
                        sp->data.i = (int)(*ip);
                        ++ip;
                        ++sp;
                        break;
                    case LDC_I8_S:
                        {
                            ++ip;
                            sp->type = TYPE_I8;
                            var t1 = (long)*ip;
                            ++ip;
                            var t2 = (long)*ip;
                            sp->data.l = t2 << 32 | t1;
                            ++ip;
                            ++sp;
                        }
                        break;
                    case LD_TYPE:
                        ++ip;
                        sp->type = TYPE_TOKEN;
                        sp->data.ui = (*ip);
                        ++sp;
                        ++ip;
                        break;
                    case NEWARR:
                        {
                            ++ip;
                            --sp;
                            var size = sp->data.ul;
                            --sp;
                            var typeID = GetClass(sp->data.ui, _module, invocation);
                            --sp;
                            sp->type = TYPE_ARRAY;
                            if (invocation.method.IsStatic)
                                sp->data.p = (nint)IshtarGC.AllocArray(typeID, size, 1, null, invocation);
                            //else fixed (IshtarObject** node = &invocation._this_)
                            //       sp->data.p = (nint)IshtarGC.AllocArray(typeID, size, 1, node, invocation);
                            ++sp;
                        }
                        break;
                    case STELEM_S:
                        ++ip;
                        --sp;
                        (sp - 1)->validate(invocation, TYPE_ARRAY);
                        ((IshtarArray*)(sp - 1)->data.p)->Set(*ip++, IshtarMarshal.Boxing(invocation, sp), invocation);
                        break;
                    case LDELEM_S:
                        {
                            ++ip;
                            --sp;
                            sp->validate(invocation, TYPE_ARRAY);
                            var arr = sp->data.p;
                            ++sp;
                            (*sp) = IshtarMarshal.UnBoxing(invocation, ((IshtarArray*)arr)->Get(*ip++, invocation));
                            ++sp;
                        }
                        break;
                    case LDLEN:
                        {
                            ++ip;
                            --sp;
                            sp->validate(invocation, TYPE_ARRAY);
                            var arr = sp->data.p;
                            ++sp;
                            sp->type = TYPE_U8;
                            sp->data.ul = ((IshtarArray*)arr)->length;
                            ++sp;
                        }
                        break;
                    case RET:
                        ++ip;
                        --sp;
                        invocation.returnValue = &*sp;
                        stack = null;
                        locals = null;
                        return;
                    case STSF:
                        {
                            --sp;
                            var fieldIdx = *++ip;
                            var @class = GetClass(*++ip, _module, invocation);
                            var field = GetField(fieldIdx, @class, _module, invocation);

                            @class.vtable[field.vtable_offset] = IshtarMarshal.Boxing(invocation, sp);
                            ++ip;
                        }
                        break;
                    case LDSF:
                        {
                            var fieldIdx = *++ip;
                            var @class = GetClass(*++ip, _module, invocation);
                            var field = GetField(fieldIdx, @class, _module, invocation);
                            var obj = (IshtarObject*)@class.vtable[field.vtable_offset];

                            *sp = IshtarMarshal.UnBoxing(invocation, obj);
                            ++sp;
                            ++ip;
                        }
                        break;
                    case STF:
                        {
                            --sp;
                            var fieldIdx = *++ip;
                            var @class = GetClass(*++ip, _module, invocation);
                            var field = GetField(fieldIdx, @class, _module, invocation);
                            var @this = sp;
                            --sp;
                            if (@this->type == TYPE_NONE)
                            {
                                ForceFail(KnowTypes.NullPointerException(invocation));
                                break;
                            }
                            //FFI.StaticValidate(invocation, @this, field.Owner);
                            var value = sp;
                            var this_obj = (IshtarObject*)@this->data.p;
                            var target_class = this_obj->decodeClass();
                            this_obj->vtable[field.vtable_offset] = IshtarMarshal.Boxing(invocation, value);
                            ++ip;
                        }
                        break;
                    case LDF:
                        {
                            --sp;
                            var fieldIdx = *++ip;
                            var @class = GetClass(*++ip, _module, invocation);
                            var field = GetField(fieldIdx, @class, _module, invocation);
                            var @this = sp;
                            if (@this->type == TYPE_NONE)
                            {
                                // TODO
                                CallFrame.FillStackTrace(invocation);
                                FastFail(NONE, $"NullReferenceError", invocation);
                                ValidateLastError();
                            }
                            //FFI.StaticValidate(invocation, @this, field.Owner);
                            var this_obj = (IshtarObject*)@this->data.p;
                            var target_class = this_obj->decodeClass();
                            var pt = target_class.Parents.First();
                            var obj = (IshtarObject*)this_obj->vtable[field.vtable_offset];
                            var value = IshtarMarshal.UnBoxing(invocation, obj);
                            *sp = value;
                            ++ip;
                            ++sp;
                        }
                        break;
                    case LDNULL:
                        sp->type = TYPE_OBJECT;
                        sp->data.p = (nint)0;
                        ++sp;
                        ++ip;
                        break;
                    case CAST:
                    {
                        ++ip;
                        --sp;
                        var t2 = GetClass(sp->data.ui, _module, invocation);
                        --sp;
                        var t1 = (IshtarObject*)sp->data.p;
                        if (t1 == null)
                        {
                            ForceFail(KnowTypes.NullPointerException(invocation));
                            continue;
                        }
                        var r = IshtarObject.IsInstanceOf(invocation, t1, t2);
                        if (r == null)
                            ForceFail(KnowTypes.IncorrectCastFault(invocation));
                        else ++sp;
                    }
                        break;
                    case SEH_ENTER:
                        ip++;
                        zone = mh.exception_handler_list[(int)(*ip)];
                        break;
                    case SEH_LEAVE:
                    case SEH_LEAVE_S:
                        while (sp > invocation.stack) --sp;
                        invocation.last_ip = (OpCodeValue)(*ip);
                        if (*ip == (uint)SEH_LEAVE_S)
                        {
                            ++ip;
                            jump_now();
                        }
                        else
                            ip++;
                        endfinally_ip = ip;
                        break;
                    case SEH_FINALLY:
                        ++ip;
                        zone = default;
                        break;
                    case SEH_FILTER: // todo, maybe unused
                        ++ip;
                        break;
                    case POP:
                        ++ip;
                        --sp;
                        if (sp->type == TYPE_CLASS)
                        {
                            var obj = ((IshtarObject*)sp->data.p);
                            IshtarGC.FreeObject(&obj);
                        }
                        break;
                    case THROW:
                        --sp;
                        if (sp->data.p == IntPtr.Zero)
                        {
                            sp->data.p = (nint)IshtarGC.AllocObject(KnowTypes.NullPointerException(invocation));
                            sp->type = TYPE_CLASS;
                        }
                        goto exception_handle;
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
                        }
                        break;
                    case CALL:
                        {
                            var child_frame = new CallFrame();
                            ++ip;
                            var tokenIdx = *ip;
                            var owner = readTypeName(*++ip, _module);
                            var method = GetMethod(tokenIdx, owner, _module, invocation);
                            ++ip;
                            println($"@@@ {owner.NameWithNS}::{method.Name}");
                            var method_args = stackval.Alloc(method.ArgLength);
                            for (int i = 0, y = method.ArgLength - 1; i != method.ArgLength; i++, y--)
                            {
                                var _a = method.Arguments[y]; // TODO, type eq validate
                                --sp;

#if DEBUG
                                println($"@@@@<< {_a.Name}: {_a.Type.FullName.NameWithNS}");
                                if (Environment.GetCommandLineArgs().Contains("--sys::ishtar::skip-validate-args=1"))
                                {
                                    method_args[y] = *sp;
                                    continue;
                                }
                                var arg_class = _a.Type as RuntimeIshtarClass;
                                if (arg_class.Name is not "Object" and not "ValueType")
                                {
                                    var sp_obj = IshtarMarshal.Boxing(invocation, sp);

                                    if (sp_obj == null)
                                        continue;

                                    var sp_class = sp_obj->decodeClass();

                                    if (sp_class == null)
                                        continue;

                                    if (sp_class.ID != arg_class.ID)
                                    {
                                        if (!sp_class.IsInner(arg_class))
                                        {
                                            FastFail(TYPE_MISMATCH,
                                                $"Argument '{_a.Name}: {_a.Type.Name}'" +
                                                $" is not matched for '{method.Name}' function.",
                                                invocation);
                                            break;
                                        }
                                    }
                                }
#endif

                                method_args[y] = *sp;
                            }


                            (child_frame.level, child_frame.parent, child_frame.method)
                                = (invocation.level + 1, invocation, method);
                            fixed (stackval* p = method_args)
                                child_frame.args = p;

                            if (method.IsExtern) exec_method_native(child_frame);
                            else exec_method(child_frame);
                            
                            if (method.ReturnType.TypeCode != TYPE_VOID)
                            {
                                invocation.assert(child_frame.returnValue is not null, STATE_CORRUPT, "Method has return zero memory.");
                                *sp = *child_frame.returnValue;
                                sp++;
                            }

                            if (child_frame.exception is not null)
                            {
                                sp->type = TYPE_CLASS;
                                sp->data.p = (nint)child_frame.exception.value;
                                (method_args, child_frame) = (null, null);
                                goto exception_handle;
                            }

                            (method_args, child_frame) = (null, null);
                        }
                        break;
                    case LOC_INIT:
                        {
                            ++ip;
                            var locals_size = *ip;
                            fixed (stackval* p = stackval.Alloc((int)locals_size))
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
                    case EQL_H:
                        {
                            ++ip;
                            --sp;
                            var first = *sp;
                            --sp;
                            var second = *sp;

                            println($"$$$ EQL_H : {first.data.i} > {second.data.i} == {first.data.i < second.data.i}");

                            if (first.type == second.type)
                            {
                                switch (first.type)
                                {
                                    case TYPE_I1:
                                        if (first.data.b > second.data.b)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_U1:
                                        if (first.data.ub > second.data.ub)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_I2:
                                        if (first.data.s > second.data.s)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_U2:
                                        if (first.data.us > second.data.us)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_I4:
                                        if (first.data.i > second.data.i)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_U4:
                                        if (first.data.ui > second.data.ui)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_I8:
                                        if (first.data.l > second.data.l)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_U8:
                                        if (first.data.ul > second.data.ul)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_R2:
                                        if (first.data.hf > second.data.hf)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_R4:
                                        if (first.data.f_r4 > second.data.f_r4)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_R8:
                                        if (first.data.f > second.data.f)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_R16:
                                        if (first.data.d > second.data.d)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                sp++;
                            }
                            else
                                throw new NotImplementedException();
                        }
                        break;
                    case EQL_L:
                        {
                            ++ip;
                            --sp;
                            var first = *sp;
                            --sp;
                            var second = *sp;

                            println($"$$$ : {first.data.i} < {second.data.i} == {first.data.i < second.data.i}");

                            if (first.type == second.type)
                            {
                                switch (first.type)
                                {
                                    case TYPE_I1:
                                        if (first.data.b < second.data.b)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_U1:
                                        if (first.data.ub < second.data.ub)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_I2:
                                        if (first.data.s < second.data.s)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_U2:
                                        if (first.data.us < second.data.us)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_I4:
                                        if (first.data.i < second.data.i)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_U4:
                                        if (first.data.ui < second.data.ui)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_I8:
                                        if (first.data.l < second.data.l)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_U8:
                                        if (first.data.ul < second.data.ul)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_R2:
                                        if (first.data.hf < second.data.hf)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_R4:
                                        if (first.data.f_r4 < second.data.f_r4)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_R8:
                                        if (first.data.f < second.data.f)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    case TYPE_R16:
                                        if (first.data.d < second.data.d)
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 1;
                                        }
                                        else
                                        {
                                            sp->type = TYPE_I4;
                                            sp->data.i = 0;
                                        }
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                sp++;
                            }
                            else
                                throw new NotImplementedException();
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
                        }
                        break;
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
                        }
                        break;
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
                        }
                        break;
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
                        }
                        break;
                    case JMP:
                        ++ip;
                        jump_now();
                        break;
                    case JMP_F:
                        {
                            ++ip;
                            --sp;
                            var first = *sp;
                            switch (first.type)
                            {
                                case TYPE_I1:
                                    if (first.data.b == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U1:
                                    if (first.data.ub == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I2:
                                    if (first.data.s == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U2:
                                    if (first.data.us == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I4:
                                    if (first.data.i == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U4:
                                    if (first.data.ui == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_I8:
                                    if (first.data.l == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_U8:
                                    if (first.data.ul == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R2:
                                    if (first.data.hf == (Half)0f)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R4:
                                    if (first.data.f_r4 == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R8:
                                    if (first.data.f == 0)
                                        jump_now();
                                    else ++ip; break;
                                case TYPE_R16:
                                    if (first.data.d == 0)
                                        jump_now();
                                    else ++ip; break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        break;
                    case LDLOC_0:
                    case LDLOC_1:
                    case LDLOC_2:
                    case LDLOC_3:
                    case LDLOC_4:
                        *sp = locals[(*ip) - (int)LDLOC_0];
                        println($"load from locals ({sp->type})");
                        ++ip;
                        ++sp;
                        break;
                    case LDLOC_S:
                        ++ip;
                        *sp = locals[(*ip)];
                        ++sp;
                        ++ip;
                        break;
                    case STLOC_0:
                    case STLOC_1:
                    case STLOC_2:
                    case STLOC_3:
                    case STLOC_4:
                        --sp;
                        locals[(*ip) - (int)STLOC_0] = *sp;
                        println($"stage to locals ({sp->type})");
                        ++ip;
                        break;
                    case STLOC_S:
                        ++ip;
                        --sp;
                        locals[(*ip)] = *sp;
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
                            sp->data.p = (nint)IshtarMarshal.ToIshtarObject(str, invocation);
                            ++sp;
                            ++ip;
                        }
                        break;
                    default:
                        CallFrame.FillStackTrace(invocation);

                        FastFail(STATE_CORRUPT, $"Unknown opcode: {invocation.last_ip}\n" +
                            $"{ip - start}\n" +
                            $"{invocation.exception.stack_trace}", invocation);
                        ++ip;
                        break;
                }


                continue;


                exception_handle:


                void fill_frame_exception()
                {
                    invocation.exception = new CallFrameException
                    {
                        last_ip = ip,
                        value = (IshtarObject*)sp->data.p
                    };
                    CallFrame.FillStackTrace(invocation);
                    ip++;
                }

                var tryEndAddr = zone != default ? get_jumper(zone.tryEndLabel) : null;

                if (zone != default && tryEndAddr > ip)
                {
                    var label_addr = -1;
                    var exception = (IshtarObject*)sp->data.p;
                    for (int i = 0; i < zone.catchClass.Length; i++)
                    {
                        var t = zone.catchClass[i];

                        if (t is null)
                            continue;
                        if (zone.types[i] == ExceptionMarkKind.FILTER)
                        {
                            if (t == exception->decodeClass().FullName)
                            {
                                label_addr = zone.filterAddr[i];
                                break;
                            }
                        }
                        else if (zone.types[i] == ExceptionMarkKind.CATCH_ANY)
                        {
                            label_addr = zone.catchAddr[i];
                            break;
                        }
                    }

                    if (label_addr == -1)
                    {
                        for (int i = 0; i < zone.catchClass.Length; i++)
                        {
                            var t = zone.catchClass[i];
                            if (t is null)
                                continue;
                            if (zone.types[i] != ExceptionMarkKind.FILTER)
                                continue;
                            if (t.Name.Equals("Void")) // skip empty type
                                continue;

                            var filter_type = KnowTypes.FromCache(t, invocation);
                            var fail_type = exception->decodeClass();
                            
                            if (fail_type.IsInner(filter_type))
                            {
                                label_addr = zone.filterAddr[i];
                                break;
                            }
                        }
                    }

                    if (label_addr != -1)
                    {
                        jump_to(label_addr);
                        ++sp;
                    }
                    else fill_frame_exception();
                }
                else
                {
                    fill_frame_exception();
                    return;
                }

                goto vm_cycle_start;
            }
        }
        
        public static void Assert(bool conditional, WNE type, string msg, CallFrame frame = null)
        {
            if (conditional)
                return;
            FastFail(type, $"static assert failed: '{msg}'", frame);
            ValidateLastError();
        }
    }
}
