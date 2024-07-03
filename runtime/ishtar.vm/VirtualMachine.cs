namespace ishtar
{
    using emit;
    using io;
    using ishtar.llmv;
    using ishtar.runtime.vin;
    using runtime;
    using runtime.gc;
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using debug;
    using vein.extensions;
    using vein.runtime;
    using static OpCodeValue;
    using static vein.runtime.VeinTypeCode;
    using static WNE;

    public delegate void A_OperationDelegate<T>(ref T t1, ref T t2);

    public readonly struct RuntimeInfo()
    {
        public readonly bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public readonly bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public readonly bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public readonly bool isFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        public readonly Architecture Architecture = RuntimeInformation.OSArchitecture;
    }

    public unsafe struct VirtualMachineRef
    {
        public InternedString* Name;

        public IshtarFrames* Frames;
        internal IshtarTrace trace;
        internal LLVMContext Jitter;
        public IshtarTypes* Types;
        public IshtarThreading threading;
        public TaskScheduler* task_scheduler;
        internal RuntimeIshtarModule* InternalModule;
        internal RuntimeIshtarClass* InternalClass;
    }

    public unsafe partial class VirtualMachine : IDisposable
    {
        public readonly RuntimeInfo runtimeInfo = new RuntimeInfo(); 

        /// <exception cref="OutOfMemoryException">There is insufficient memory to satisfy the request.</exception>
        public static VirtualMachine Create(string name)
        {
            var vm = new VirtualMachine();

            BoehmGCLayout.Native.Load(vm.runtimeInfo);
            BoehmGCLayout.Native.GC_set_find_leak(true);
            BoehmGCLayout.Native.GC_init();
            BoehmGCLayout.Native.GC_allow_register_threads();

            vm.@ref = IshtarGC.AllocateImmortal<VirtualMachineRef>(null);

            vm.Jit = new IshtarJIT(vm);
            vm.Config = new VMConfig();
            vm.Vault = new AppVault(vm, name);
            vm.@ref->trace = new IshtarTrace();

            vm.trace.Setup();

            vm.@ref->Types = IshtarTypes.Create(vm.Vault);
            vm.GC = new IshtarGC(vm);

            vm.@ref->InternalModule = vm.Vault.DefineModule("$ishtar$");

            vm.@ref->InternalClass = vm.InternalModule->DefineClass("sys%$ishtar$/global".L(vm.@ref->InternalModule),
                vm.Types->ObjectClass);

            vm.@ref->Frames = IshtarFrames.Create(vm);
            vm.watcher = new DefaultWatchDog(vm);
            
            vm.Config = new VMConfig();
            vm.NativeStorage = new NativeStorage(vm);
            vm.GC.init();
            
            vm.FFI = new ForeignFunctionInterface(vm);
            vm.@ref->Jitter = new LLVMContext();

            vm.@ref->threading = new IshtarThreading();

            vm.@ref->task_scheduler = vm.threading.CreateScheduler(vm);
            
            return vm;
        }

        public void Dispose()
        {
            task_scheduler->Dispose();
            InternalModule->Dispose();
            IshtarGC.FreeImmortalRoot(InternalModule);

            GC.Dispose();
            Vault.Dispose();
            StringStorage.Dispose();
        }


        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, params (string name, VeinTypeCode code)[] args)
        {
            var converter_args = RuntimeMethodArgument.Create(Types, args, @ref);
            return InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags, converter_args);
        }

        public RuntimeIshtarMethod* GetOrCreateSpecialMethod(string name)
        {
            var exist = InternalClass->FindMethod(name, x => x->Name.Contains(name));

            if (exist is not null)
                return exist;

            return CreateInternalMethod(name, MethodFlags.Special | MethodFlags.Static);
        }

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, params (string name, VeinTypeCode code)[] args)
        {
            var converter_args = RuntimeMethodArgument.Create(Types, args, @ref);
            return InternalClass->DefineMethod(name, returnType, flags, converter_args);
        }

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags)
            => InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags);

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, params VeinArgumentRef[] args)
            => InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags, RuntimeMethodArgument.Create(this, args, @ref));

        public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, params VeinArgumentRef[] args)
            => InternalClass->DefineMethod(name, returnType, flags, RuntimeMethodArgument.Create(this, args, @ref));

        public RuntimeIshtarMethod* DefineEmptySystemMethod(string name)
            => CreateInternalMethod(name, MethodFlags.Extern, TYPE_VOID.AsRuntimeClass(Types), Array.Empty<VeinArgumentRef>());

        public VirtualMachineRef* @ref;

        public volatile NativeException CurrentException;
        public volatile IWatchDog watcher;
        public volatile AppVault Vault;
        public volatile IshtarGC GC;
        public volatile ForeignFunctionInterface FFI;
        public volatile VMConfig Config;
        public volatile IshtarJIT Jit;
        public volatile NativeStorage NativeStorage;

        public IshtarFrames* Frames => @ref->Frames;
        internal IshtarTrace trace => @ref->trace;
        internal LLVMContext Jitter => @ref->Jitter;
        public IshtarTypes* Types => @ref->Types;
        public IshtarThreading threading => @ref->threading;
        public TaskScheduler* task_scheduler => @ref->task_scheduler;
        internal RuntimeIshtarModule* InternalModule => @ref->InternalModule;
        internal RuntimeIshtarClass* InternalClass => @ref->InternalClass;


        public bool HasFaulted() => CurrentException is not null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastFail(WNE type, string msg, CallFrame* frame)
        {
            watcher?.FastFail(type, msg, frame);
            watcher?.ValidateLastError();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastFail(bool assert, WNE type, string msg, CallFrame* frame)
        {
            if (!assert) return;
            watcher?.FastFail(type, msg, frame);
            watcher?.ValidateLastError();
        }

        [Conditional("DEBUG")]
        public void println(string str) => trace.println(str);

        public void halt(int exitCode = -1)
        {
#if DEBUG
            trace.println($"exit code is {exitCode}");
            trace.println("Press ENTER to exit...");
            while (System.Console.ReadKey().Key != ConsoleKey.Enter) Thread.Sleep(1);
#endif
            Environment.Exit(exitCode);
        }


        public void exec_method_external_native(CallFrame* frame)
        {
            var executionEngine = Jitter.GetExecutionEngine();


            ref var pinfo = ref frame->method->PIInfo;
            if (pinfo.compiled_func_ref == default)
                pinfo.create_bindings(executionEngine);

            Jitter.PrintAsm(frame->method);

            var caller = (delegate*<stackval*, int, stackval>)
                pinfo.compiled_func_ref;

            var result = caller(frame->args, frame->method->ArgLength);
            Assert(result.type == frame->method->ReturnType->TypeCode, TYPE_MISMATCH,
                $"jit generated incorrect return type for '{frame->method->Name}'");

            if (frame->method->ReturnType->TypeCode is TYPE_VOID)
                return;

            frame->returnValue = stackval.Allocate(frame, 1);
            *frame->returnValue.Ref = result;
        }

        public void exec_method_internal_native(CallFrame* frame)
        {
            // TODO remove using AllocHGlobal
            var caller = (delegate*<CallFrame*, IshtarObject**, IshtarObject*>)
                frame->method->PIInfo.compiled_func_ref;
            var args_len = frame->method->ArgLength;

            var args = (IshtarObject**)Marshal.AllocHGlobal(sizeof(IshtarObject*) * args_len);

            if (args == null)
            {
                FastFail(OUT_OF_MEMORY, "Cannot apply boxing memory.", frame);
                return;
            }

            for (var i = 0; i != args_len; i++)
                args[i] = IshtarMarshal.Boxing(frame, &frame->args[i]);

            var result = caller(frame, args);

            Marshal.FreeHGlobal((nint)args);

            if (frame->method->ReturnType->TypeCode == TYPE_VOID)
                return;
            frame->returnValue = stackval.Allocate(frame, 1);
            frame->returnValue.Ref->type = frame->method->ReturnType->TypeCode;
            frame->returnValue.Ref->data.p = (nint)result;
        }

        public void exec_method_native(CallFrame* frame)
        {
            if (frame->method->PIInfo.Equals(PInvokeInfo.Zero))
            {
                FastFail(MISSING_METHOD, "Native method not linked.", frame);
                return;
            }

            if (!frame->method->PIInfo.isInternal)
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
        private bool assert_violation_zone_writes(CallFrame* frame, SmartPointer<stackval> stack, int size)
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


        public void exec_method(CallFrame* invocation)
        {
            if (!Config.DisableValidationInvocationArgs)
            {
                var argsLen = invocation->method->ArgLength;

                for (int i = 0; i != argsLen; i++)
                {
                    if (invocation->args[i].type > TYPE_NULL)
                        FastFail(STATE_CORRUPT, $"[arg validation] argument [{i}/{argsLen}] for [{invocation->method->Name}] has corrupted", invocation);
                }
            }

            println($"@.frame> {invocation->method->Owner->Name}::{invocation->method->Name}");

            var _module = invocation->method->Owner->Owner;
            var mh = invocation->method->Header;
            FastFail(mh is null, MISSING_METHOD, "method code is zero", invocation);
            
            var args = invocation->args;

            var locals = default(SmartPointer<stackval>);

            var ip = mh->code;

            // todo, revert to stackalloc
            var stack = stackval.Allocate(invocation, mh->max_stack);

            const int STACK_VIOLATION_LEVEL_SIZE = 32;
            
            create_violation_zone_for_stack(stack, STACK_VIOLATION_LEVEL_SIZE);

            var sp = stack.Ref + STACK_VIOLATION_LEVEL_SIZE;
            var sp_start = sp;
            var start = ip;

            long getStackLen() => sp - sp_start;

            var end = mh->code + mh->code_size;
            var end_stack = sp + mh->max_stack;
            uint* endfinally_ip = null;
            var zone = default(ProtectedZone*);
            void jump_now()
            {
                var labelKey = mh->labels->Get((int)*ip);
                if (mh->labels_map->TryGetValue(labelKey, out var label))
                    ip = start + label.pos - 1;
                else
                    FastFail(PROTECTED_ZONE_LABEL_CORRUPT, "[jump_now] cannot find protected zone label", invocation);
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

            void ForceThrow(RuntimeIshtarClass* clazz)
            {
                CallFrame.FillStackTrace(invocation);
                sp->data.p = (nint)GC.AllocObject(clazz, invocation);
                sp->type = TYPE_CLASS;
            }

            var stopwatch = new Stopwatch();

            while (true)
            {
                vm_cycle_start:
                invocation->last_ip = (OpCodeValue)(ushort)*ip;
                println($"@@.{invocation->last_ip} 0x{(nint)ip:X} [sp: {getStackLen()}]");

                if (!invocation->exception.IsDefault() && invocation->level == 0)
                    return;
                FastFail(ip == end, END_EXECUTE_MEMORY, "unexpected end of executable memory.", invocation);
                FastFail(sp >= end_stack, OVERFLOW, "stack overflow detected.", invocation);
                FastFail(sp < sp_start, OVERFLOW, "incorrect sp address beyond sp_start was detected", invocation);

                if (!assert_violation_zone_writes(invocation, stack, STACK_VIOLATION_LEVEL_SIZE))
                    continue;

                Thread.Sleep(1);

                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                    trace.signal_state(invocation->last_ip, *invocation, stopwatch.Elapsed, *sp);
                }

                stopwatch.Restart();

                switch (invocation->last_ip)
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
                        Assert(sp->type != TYPE_NONE, STATE_CORRUPT, "", invocation);
                        Assert(sp->type <= TYPE_NULL, STATE_CORRUPT, "", invocation);
                    ++sp;
                        ++ip;
                        break;
                    case LDARG_S:
                        ++ip;
                        *sp = args[(*ip)];
                        println($"load from args ({sp->type})");
                        Assert(sp->type != TYPE_NONE, STATE_CORRUPT, "", invocation);
                        Assert(sp->type <= TYPE_NULL, STATE_CORRUPT, "", invocation);
                        ++sp;
                        ++ip;
                        break;
                    case LDC_I2_0:
                    case LDC_I2_1:
                    case LDC_I2_2:
                    case LDC_I2_3:
                    case LDC_I2_4:
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
                    case LDC_I4_4:
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
                    case LDC_I8_4:
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
                            if (invocation->method->IsStatic)
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
                        invocation->returnValue = stackval.Allocate(invocation, 1);
                        invocation->returnValue[0] = *sp;
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
                                ForceThrow(KnowTypes.NullPointerException(invocation));
                                goto exception_handle;
                            }
                            //FFI.StaticValidate(invocation, @this, field.Owner);
                            var value = sp;
                            var this_obj = (IshtarObject*)@this->data.p;
                            var target_class = this_obj->clazz;

                            println($"@@@ STF -> {value->type} (to {field->Name})");

                            if (value->type == TYPE_NULL)
                                this_obj->vtable[field->vtable_offset] = null;
                            else if (value->type == TYPE_RAW)
                                this_obj->vtable[field->vtable_offset] = (void*)value->data.p;
                            else
                            {
                                var o = IshtarMarshal.Boxing(invocation, value);

                                this_obj->vtable[field->vtable_offset] = o;
                            }
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
                                CallFrame.FillStackTrace(invocation);
                                FastFail(STATE_CORRUPT, $"[LDF] invalid @this object loaded, TYPE_NONE, maybe corrupted IL code", invocation);
                            }

                            if (@this->type == TYPE_NULL)
                            {
                                CallFrame.FillStackTrace(invocation);
                                ForceThrow(KnowTypes.NullPointerException(invocation));
                                goto exception_handle;
                            }
                            //FFI.StaticValidate(invocation, @this, field.Owner);
                            var this_obj = (IshtarObject*)@this->data.p;
                            var target_class = this_obj->clazz;
                            var pt = target_class->Parent;
                            var obj = (IshtarObject*)this_obj->vtable[field->vtable_offset];
                            if (field->FieldType.IsGeneric)
                                throw new NotImplementedException();
                            if (field->FieldType.Class->TypeCode is TYPE_RAW)
                            {
                                sp->type = TYPE_RAW;
                                sp->data.p = (nint)obj;
                            }
                            else
                            {
                                var value = IshtarMarshal.UnBoxing(invocation, obj);
                                *sp = value;
                            }

                            
                            ++ip;
                            println($"@@@ LDF -> {sp->type} (from {field->Name})");
                            ++sp;
                        }
                        break;
                    case LDNULL:
                        sp->type = TYPE_NULL;
                        sp->data.p = 0;
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
                                ForceThrow(KnowTypes.NullPointerException(invocation));
                                goto exception_handle;
                            }
                            var r = IshtarObject.IsInstanceOf(invocation, t1, t2);
                            if (r == null)
                            {
                                ForceThrow(KnowTypes.IncorrectCastFault(invocation));
                                goto exception_handle;
                            }
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
                        invocation->last_ip = (OpCodeValue)(*ip);
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
                    case LDFN:
                    {
                        ++ip;
                        var tokenIdx = *ip;
                        var owner = readTypeName(*++ip, _module, invocation);
                        var method = GetMethod(tokenIdx, owner, _module, invocation);
                        ++ip;

                        var raw = GC.AllocRawValue(invocation); // TODO destroy

                        raw->type = VeinRawCode.ISHTAR_METHOD;
                        raw->data.m = method;

                        sp->type = TYPE_RAW; 
                        sp->data.p = (nint)raw;
                        ++sp;
                    } break;
                    case CALL_SP:
                    case CALL:
                    {
                        var method = default(RuntimeIshtarMethod*);
                        if (invocation->last_ip == CALL)
                        {
                            ++ip;
                            var tokenIdx = *ip;
                            var owner = readTypeName(*++ip, _module, invocation);

                            method = GetMethod(tokenIdx, owner, _module, invocation);
                            ++ip;
                        }

                        if (invocation->last_ip == CALL_SP)
                        {
                            ++ip;
                            sp--;

                            if (sp->type == TYPE_NULL)
                            {
                                ForceThrow(KnowTypes.NullPointerException(invocation));
                                goto exception_handle;
                            }

                            var raw = (rawval*)sp->data.p;

                            if (raw->type == VeinRawCode.ISHTAR_ERROR)
                            {
                                ForceThrow(KnowTypes.IncorrectCastFault(invocation));
                                goto exception_handle;
                            }
                            Assert(raw->type == VeinRawCode.ISHTAR_METHOD, MISSING_TYPE, "", invocation);
                            method = raw->data.m;
                        }
                        

                        var child_frame = invocation->CreateChild(method);
                        println($"@@@ {method->Owner->Name}::{method->Name}");
                        var method_args = GC.AllocateStack(child_frame, method->ArgLength);
                        for (int i = 0, y = method->ArgLength - 1; i != method->ArgLength; i++, y--)
                        {
                            var _a = method->Arguments->Get(i); // TODO, type eq validate
                            --sp;
//#if DEBUG
//                                if (_a->Type.IsGeneric)
//                                    println($"@@@@<< {StringStorage.GetString(_a->Name, invocation)}: {StringStorage.GetString(_a->Type.TypeArg->Name, invocation)}");
//                                else
//                                    println($"@@@@<< {StringStorage.GetString(_a->Name, invocation)}: {_a->Type.Class->FullName->NameWithNS}");
                            
//                                if (Config.CallOpCodeSkipValidateArgs)
//                                {
//                                    method_args[y] = *sp;
//                                    continue;
//                                }
//                                var arg_class = _a->Type;
//                                if (arg_class->Name is not "Object" and not "ValueType")
//                                {
//                                    var sp_obj = IshtarMarshal.Boxing(invocation, sp);

//                                    if (sp_obj == null)
//                                        continue;

//                                    var sp_class = sp_obj->clazz;

//                                    if (sp_class == null)
//                                        continue;

//                                    if (sp_class->ID != arg_class->ID)
//                                    {
//                                        if (!sp_class->IsInner(arg_class))
//                                        {
//                                            FastFail(TYPE_MISMATCH,
//                                                $"Argument '{StringStorage.GetString(_a->Name, invocation)}: {_a->Type->Name}'" +
//                                                $" is not matched for '{method->Name}' function.",
//                                                invocation);
//                                            break;
//                                        }
//                                    }
//                                }
//#endif

                            println($"@@@ {method->Owner->Name}::{method->Name} (argument {y} is {sp->type} type)");

                            if (sp->type > TYPE_NULL)
                                FastFail(STATE_CORRUPT, $"[call arg validation] trying fill corrupted argument [{y}/{method->ArgLength}] for [{method->Name}]", invocation);

                            method_args[y] = *sp;
                        }

                        child_frame->args = method_args;


                        if (method->IsExtern)
                            exec_method_native(child_frame);
                        else
                            task_scheduler->execute_method(child_frame);

                        if (method->ReturnType->TypeCode != TYPE_VOID)
                        {
                            invocation->assert(!child_frame->returnValue.IsNull(), STATE_CORRUPT, "Method has return zero memory.");
                            *sp = child_frame->returnValue[0];

                            child_frame->returnValue.Dispose();

                            sp++;
                        }

                        if (!child_frame->exception.IsDefault())
                        {
                            sp->type = TYPE_CLASS;
                            sp->data.p = (nint)child_frame->exception.value;
                            invocation->exception = child_frame->exception;
                            
                            GC.FreeStack(child_frame, method_args, method->ArgLength);
                            child_frame->Dispose();
                            goto exception_handle;
                        }
                        
                        GC.FreeStack(child_frame, method_args, method->ArgLength);
                        child_frame->Dispose();
                        GC.Collect();
                    } break;
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
                    case EQL_T:
                    case EQL_F:
                    {
                        ++ip;
                        --sp;
                        var first = *sp;
                        *sp = _comparer(first, default, invocation->last_ip, invocation);
                        println($"$$$ {invocation->last_ip} : {debug_comparer_get_symbol(first, default, invocation->last_ip)} == {sp->data.i == 1}");
                        sp++;
                    }
                    break;
                    case EQL_H:
                    case EQL_L:
                    case EQL_NN:
                    case EQL_HQ:
                    case EQL_LQ:
                    case EQL_NQ:
                    {
                        ++ip;
                        --sp;
                        var first = *sp;
                        --sp;
                        var second = *sp;

                        *sp = _comparer(first, second, invocation->last_ip, invocation);
                        println($"$$$ {invocation->last_ip} : {debug_comparer_get_symbol(first, second, invocation->last_ip)} == {sp->data.i == 1}");
                        sp++;
                    }
                    break;
                    case JMP_L:
                        {
                            ++ip;
                            --sp;
                            var second = *sp;
                            --sp;
                            var first = *sp;

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
                                case TYPE_BOOLEAN:
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
                            var second = *sp;
                            --sp;
                            var first = *sp;

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
                            var second = *sp;
                            --sp;
                            var first = *sp;

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
                                case TYPE_BOOLEAN:
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

                        FastFail(STATE_CORRUPT, $"Unknown opcode: {invocation->last_ip}\n" +
                            $"{ip - start}\n" +
                            $"{invocation->exception.GetStackTrace()}", invocation);
                        ++ip;
                        break;
                }


                continue;


            exception_handle:


                void fill_frame_exception()
                {
                    if (invocation->exception.last_ip is null)
                        invocation->exception.last_ip = ip;
                    invocation->exception.value = (IshtarObject*)sp->data.p;
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

        public static void Assert(bool conditional, WNE type, string msg, CallFrame* frame = null)
        {
            if (conditional)
                return;
            if (frame is null)
                return;
            frame->vm.FastFail(type, $"static assert failed: '{msg}'", frame);
        }

        public static void GlobalPrintln(string empty) {}
    }
}
