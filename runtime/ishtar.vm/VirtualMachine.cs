namespace ishtar
{
    using emit;
    using ishtar.runtime.vin;
    using runtime;
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using collections;
    using vein.extensions;
    using vein.reflection;
    using vein.runtime;
    using static OpCodeValue;
    using static vein.runtime.VeinTypeCode;
    using static WNE;
    using runtime.gc;

    public delegate void A_OperationDelegate<T>(ref T t1, ref T t2);

    public unsafe partial class VirtualMachine : IDisposable
    {


        VirtualMachine() {}

        /// <exception cref="OutOfMemoryException">There is insufficient memory to satisfy the request.</exception>
        public static VirtualMachine Create(string name)
        {
            var vm = new VirtualMachine();
            vm.Jit = new IshtarJIT(vm);
            vm.Config = new VMConfig();
            vm.Vault = new AppVault(vm, name);
            vm.trace = new IshtarTrace();
            vm.Types = IshtarTypes.Create(vm.Vault);
            vm.GC = new IshtarGC(vm);

            vm.InternalModule = vm.Vault.DefineModule("$ishtar$");

            vm.InternalClass = vm.InternalModule->DefineClass("sys%global::$ishtar$/global".L(),
                vm.Types->ObjectClass);

            vm.Frames = new IshtarFrames(vm);
            vm.watcher = new DefaultWatchDog(vm);
            
            vm.Config = new VMConfig();
            vm.NativeStorage = new NativeStorage(vm);
            vm.GC.init();
            
            vm.FFI = new ForeignFunctionInterface(vm);

            
            return vm;
        }

        public void Dispose()
        {
            InternalModule->Dispose();
            IshtarGC.FreeImmortalRoot(InternalModule);

            GC.Dispose();
            Vault.Dispose();
            StringStorage.Dispose();
        }


        private RuntimeIshtarModule* InternalModule;
        private RuntimeIshtarClass* InternalClass;

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, params (string name, VeinTypeCode code)[] args)
        {
            var converter_args = RuntimeMethodArgument.Create(Types, args);
            return InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags, converter_args);
        }

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, params (string name, VeinTypeCode code)[] args)
        {
            var converter_args = RuntimeMethodArgument.Create(Types, args);
            return InternalClass->DefineMethod(name, returnType, flags, converter_args);
        }

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags)
            => InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags);

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, params VeinArgumentRef[] args)
            => InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags, RuntimeMethodArgument.Create(this, args));

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, params VeinArgumentRef[] args)
            => InternalClass->DefineMethod(name, returnType, flags, RuntimeMethodArgument.Create(this, args));

        public RuntimeIshtarMethod* DefineEmptySystemMethod(string name)
            => CreateInternalMethod(name, MethodFlags.Extern, TYPE_VOID.AsRuntimeClass(Types), Array.Empty<VeinArgumentRef>());
        public RuntimeIshtarMethod* DefineEmptySystemMethod(string name, RuntimeIshtarClass* clazz)
        {
            var args = IshtarGC.AllocateList<RuntimeMethodArgument>();
            args->Add(RuntimeMethodArgument.Create(Types, "i1", clazz));
            return InternalClass->DefineMethod(VeinMethodBase.GetFullName(name, new List<(string argName, string typeName)>() { ("i1", clazz->Name) }), TYPE_VOID.AsRuntimeClass(Types),
                MethodFlags.Extern | MethodFlags.Private | MethodFlags.Special, args);
        }


        public volatile NativeException CurrentException;
        public volatile IWatchDog watcher;
        public volatile AppVault Vault;
        public volatile IshtarGC GC;
        public volatile ForeignFunctionInterface FFI;
        public volatile VMConfig Config;
        public volatile IshtarFrames Frames;
        public volatile IshtarJIT Jit;
        public volatile NativeStorage NativeStorage;
        internal volatile IshtarTrace trace;
        public IshtarTypes* Types;

        
        public bool HasFaulted() => CurrentException is not null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastFail(WNE type, string msg, CallFrame frame)
        {
            watcher?.FastFail(type, msg, frame);
            watcher?.ValidateLastError();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastFail(bool assert, WNE type, string msg, CallFrame frame)
        {
            if (!assert) return;
            watcher?.FastFail(type, msg, frame);
            watcher?.ValidateLastError();
        }

        [Conditional("DEBUG")]
        public void println(string str) => trace.println(str);

        public void halt(int exitCode = -1) => Environment.Exit(exitCode);

        public void exec_method_external_native(CallFrame frame)
        {
            FastFail(WNE.MISSING_METHOD, "exec_method_external_native not implemented", frame);
            return; // TODO
        }

        public void exec_method_internal_native(CallFrame frame)
        {
            var caller = (delegate*<CallFrame, IshtarObject**, IshtarObject*>)
                frame.method->PIInfo.Addr;
            var args_len = frame.method->ArgLength;
            var args = (IshtarObject**)Marshal.AllocHGlobal(sizeof(IshtarObject*) * args_len);

            if (args == null)
            {
                FastFail(OUT_OF_MEMORY, "Cannot apply boxing memory.", frame);
                return;
            }

            for (var i = 0; i != args_len; i++)
                args[i] = IshtarMarshal.Boxing(frame, &frame.args[i]);

            var result = caller(frame, args);

            Marshal.FreeHGlobal((nint)args);

            if (frame.method->ReturnType->TypeCode == TYPE_VOID)
                return;
            frame.returnValue = stackval.Allocate(frame, 1);
            frame.returnValue.Ref->type = frame.method->ReturnType->TypeCode;
            frame.returnValue.Ref->data.p = (nint)result;
        }

        public void exec_method_native(CallFrame frame)
        {
            if (frame.method->PIInfo.Addr == null)
            {
                FastFail(MISSING_METHOD, "Native method not linked.", frame);
                return;
            }

            if (frame.method->PIInfo.IsExternal())
                exec_method_external_native(frame);
            else
                exec_method_internal_native(frame);
        }

        private void create_violation_zone_for_stack(SmartPointer<stackval> stack, int size)
        {
            for (int i = 0; i < size; i++)
            {
                stack[i].type = (VeinTypeCode)int.MaxValue;
                stack[i].data.l = long.MaxValue;
            }
        }
        private bool assert_violation_zone_writes(CallFrame frame, SmartPointer<stackval> stack, int size)
        {
            for (int i = 0; i < size; i++)
            {
                if (stack[i].type != (VeinTypeCode)int.MaxValue || stack[i].data.l != long.MaxValue)
                {
                    FastFail(STATE_CORRUPT, "stack write to an violation zone has been detected, ", frame);
                    return false;
                }
            }
            return true;
        }


        public void exec_method(CallFrame invocation)
        {
            println($"@.frame> {invocation.method->Owner->Name}::{invocation.method->Name}");

            var _module = invocation.method->Owner->Owner;
            var mh = invocation.method->Header;
            FastFail(mh is null, WNE.MISSING_METHOD, "method code is zero", invocation);
            
            var args = invocation.args;

            var locals = default(SmartPointer<stackval>);

            var ip = mh->code;
            var stack = stackval.Allocate(invocation, mh->max_stack);

            const int STACK_VIOLATION_LEVEL_SIZE = 32;

            create_violation_zone_for_stack(stack, STACK_VIOLATION_LEVEL_SIZE);

            var sp = stack.Ref + STACK_VIOLATION_LEVEL_SIZE;
            var sp_start = sp;
            var start = ip;
            var end = mh->code + mh->code_size;
            var end_stack = sp + mh->max_stack;
            uint* endfinally_ip = null;
            var zone = default(ProtectedZone*);
            void jump_now()
            {
                if (mh->labels_map->TryGetValue(mh->labels->Get((int)*ip), out var label))
                    ip = start + label.pos - 1;
                else FastFail(WNE.PROTECTED_ZONE_LABEL_CORRUPT, "[jump_now] cannot find protected zone label", invocation);
            }
            void jump_to(int index)
            {
                if (mh->labels_map->TryGetValue(mh->labels->Get(index), out var label))
                    ip = start + label.pos - 1;
                else FastFail(WNE.PROTECTED_ZONE_LABEL_CORRUPT, "[jump_to] cannot find protected zone label", invocation);
            }
            uint* get_jumper(int index)
            {
                if (mh->labels_map->TryGetValue(mh->labels->Get(index), out var label))
                    return start + label.pos - 1;
                FastFail(WNE.PROTECTED_ZONE_LABEL_CORRUPT, "[get_jumper] cannot find protected zone label", invocation);
                return null;
            }

            // maybe mutable il code is bad, need research
            void ForceFail(RuntimeIshtarClass* clazz)
            {
                *ip = (uint)THROW;
                sp->data.p = (nint)GC.AllocObject(clazz, invocation);
                sp->type = TYPE_CLASS;
                sp++;
            }

            GC.Collect();

            while (true)
            {
                vm_cycle_start:
                invocation.last_ip = (OpCodeValue)(ushort)*ip;
                println($"@@.{invocation.last_ip} 0x{(nint)ip:X} [sp: {(((nint)sp) - ((nint)sp))}]");

                if (invocation.exception is not null && invocation.level == 0)
                    return;
                FastFail(ip == end, END_EXECUTE_MEMORY, "unexpected end of executable memory.", invocation);
                FastFail(sp >= end_stack, OVERFLOW, "stack overflow detected.", invocation);
                FastFail(sp < sp_start, OVERFLOW, "incorrect sp address beyond sp_start was detected", invocation);

                if (!assert_violation_zone_writes(invocation, stack, STACK_VIOLATION_LEVEL_SIZE))
                    continue;


                switch (invocation.last_ip)
                {
                    case NOP:
                        ++ip;
                        break;
                    case ADD:
                        ++ip;
                        --sp;
                        A_OP(sp, 0, ip, invocation);
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
                        FastFail(args == null, OUT_OF_RANGE,
                            $"Arguments in current function is empty, but trying access it.", invocation);
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
                            sp->type = TYPE_ARRAY;
                            if (invocation.method->IsStatic)
                                sp->data.p = (nint)GC.AllocArray(typeID, size, 1, invocation);
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
                        invocation.returnValue = stackval.Allocate(invocation, 1);
                        invocation.returnValue[0] = *sp;
                        stack.Dispose();
                        locals.Dispose();
                        return;
                    case STSF:
                        {
                            --sp;
                            var fieldIdx = *++ip;
                            var @class = GetClass(*++ip, _module, invocation);
                            var field = GetField(fieldIdx, @class, _module, invocation);

                            @class->vtable[field->vtable_offset] = IshtarMarshal.Boxing(invocation, sp);
                            ++ip;
                        }
                        break;
                    case LDSF:
                        {
                            var fieldIdx = *++ip;
                            var @class = GetClass(*++ip, _module, invocation);
                            var field = GetField(fieldIdx, @class, _module, invocation);
                            var obj = (IshtarObject*)@class->vtable[field->vtable_offset];

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
                            var target_class = this_obj->clazz;
                            this_obj->vtable[field->vtable_offset] = IshtarMarshal.Boxing(invocation, value);
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
                            }
                            //FFI.StaticValidate(invocation, @this, field.Owner);
                            var this_obj = (IshtarObject*)@this->data.p;
                            var target_class = this_obj->clazz;
                            var pt = target_class->Parent;
                            var obj = (IshtarObject*)this_obj->vtable[field->vtable_offset];
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
                        zone = mh->exception_handler_list->Get((int)(*ip));
                        break;
                    case SEH_LEAVE:
                    case SEH_LEAVE_S:
                        while (sp > sp_start) --sp;
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
                            GC.FreeObject(&obj, invocation);
                        }
                        break;
                    case THROW:
                        --sp;
                        if (sp->data.p == IntPtr.Zero)
                        {
                            sp->data.p = (nint)GC.AllocObject(KnowTypes.NullPointerException(invocation), invocation);
                            sp->type = TYPE_CLASS;
                        }
                        goto exception_handle;
                    case NEWOBJ:
                        {
                            ++ip;
                            sp->type = TYPE_CLASS;
                            sp->data.p = (nint)
                            GC.AllocObject(
                                _module->FindType(_module->GetTypeNameByIndex((int)*ip, invocation), true), invocation);
                            ++ip;
                            ++sp;
                        }
                        break;
                    case CALL:
                        {
                            var child_frame = new CallFrame(this);
                            ++ip;
                            var tokenIdx = *ip;
                            var owner = readTypeName(*++ip, _module, invocation);


                            var method = GetMethod(tokenIdx, owner, _module, invocation);
                            child_frame.level++;
                            child_frame.parent = invocation;
                            child_frame.method = method;
                            ++ip;
                            println($"@@@ {owner->NameWithNS}::{method->Name}");
                            var method_args = GC.AllocateStack(child_frame, method->ArgLength);
                            for (int i = 0, y = method->ArgLength - 1; i != method->ArgLength; i++, y--)
                            {
                                var _a = method->Arguments->Get(i); // TODO, type eq validate
                                --sp;

#if DEBUG
                                println($"@@@@<< {StringStorage.GetString(_a->Name, invocation)}: {_a->Type->FullName->NameWithNS}");
                                if (Environment.GetCommandLineArgs().Contains("--sys::ishtar::skip-validate-args=1"))
                                {
                                    method_args[y] = *sp;
                                    continue;
                                }
                                var arg_class = _a->Type;
                                if (arg_class->Name is not "Object" and not "ValueType")
                                {
                                    var sp_obj = IshtarMarshal.Boxing(invocation, sp);

                                    if (sp_obj == null)
                                        continue;

                                    var sp_class = sp_obj->clazz;

                                    if (sp_class == null)
                                        continue;

                                    if (sp_class->ID != arg_class->ID)
                                    {
                                        if (!sp_class->IsInner(arg_class))
                                        {
                                            FastFail(TYPE_MISMATCH,
                                                $"Argument '{StringStorage.GetString(_a->Name, invocation)}: {_a->Type->Name}'" +
                                                $" is not matched for '{method->Name}' function.",
                                                invocation);
                                            break;
                                        }
                                    }
                                }
#endif

                                method_args[y] = *sp;
                            }

                            child_frame.args = method_args;


                            if (method->IsExtern) exec_method_native(child_frame);
                            else exec_method(child_frame);

                            if (method->ReturnType->TypeCode != TYPE_VOID)
                            {
                                invocation.assert(!child_frame.returnValue.IsNull(), STATE_CORRUPT, "Method has return zero memory.");
                                *sp = child_frame.returnValue[0];

                                child_frame.returnValue.Dispose();

                                sp++;
                            }

                            if (child_frame.exception is not null)
                            {
                                sp->type = TYPE_CLASS;
                                sp->data.p = (nint)child_frame.exception.value;

                                GC.FreeStack(child_frame, method_args, method->ArgLength);
                                goto exception_handle;
                            }

                            GC.FreeStack(child_frame, method_args, method->ArgLength);
                        }
                        break;
                    case LOC_INIT:
                        {
                            ++ip;
                            var locals_size = *ip;
                            locals = stackval.Allocate(invocation, (ushort)locals_size);
                            ++ip;
                            for (var i = 0u; i != locals_size; i++)
                            {
                                var type_name = readTypeName(*++ip, _module, invocation);
                                var type = _module->FindType(type_name, true);
                                if (type->IsPrimitive)
                                    locals[i].type = type->TypeCode;
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
                        println($"\talive_objects: {GC.Stats.alive_objects}");
                        println($"\ttotal_allocations: {GC.Stats.total_allocations}");
                        println($"\ttotal_bytes_requested: {GC.Stats.total_bytes_requested}");
                        println($"*** END GC DUMP ***");
                        break;
                    case LDC_STR:
                        {
                            ++ip;
                            sp->type = TYPE_STRING;
                            var str = _module->GetConstStringByIndex((int) *ip);
                            sp->data.p = (nint)GC.ToIshtarObject(str, invocation);
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

                var tryEndAddr = zone != default ? get_jumper(zone->TryEndLabel) : null;

                if (zone != default && tryEndAddr > ip)
                {
                    var label_addr = -1;
                    var exception = (IshtarObject*)sp->data.p;
                    for (int i = 0; i < zone->CatchClass->Length; i++)
                    {
                        var t = zone->CatchClass->Get(i);

                        if (t is null)
                            continue;
                        if (zone->Types->Get(i) == (byte)ExceptionMarkKind.FILTER)
                        {
                            if (t == exception->clazz->FullName)
                            {
                                label_addr = zone->FilterAddr->Get(i);
                                break;
                            }
                        }
                        else if (zone->Types->Get(i) == (byte)ExceptionMarkKind.CATCH_ANY)
                        {
                            label_addr = zone->CatchAddr->Get(i);
                            break;
                        }
                    }

                    if (label_addr == -1)
                    {
                        for (int i = 0; i < zone->CatchClass->Length; i++)
                        {
                            // TODO, need remove using QualityTypeName and replace to RuntimeClass ref
                            var t = zone->CatchClass->Get(i);
                            if (t is null)
                                continue;
                            if (zone->Types->Get(i) != (byte)ExceptionMarkKind.FILTER)
                                continue;
                            if (t->Name.Equals("Void")) // skip empty type
                                continue;

                            var filter_type = KnowTypes.FromCache(t, invocation);
                            var fail_type = exception->clazz;

                            if (fail_type->IsInner(filter_type))
                            {
                                label_addr = zone->FilterAddr->Get(i);
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
            frame?.vm?.FastFail(type, $"static assert failed: '{msg}'", frame);
        }

        public static void GlobalPrintln(string empty) {}
    }
}
