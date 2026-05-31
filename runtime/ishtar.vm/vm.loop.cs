namespace ishtar;

using emit;
using io;
using ishtar.jit;
using ishtar.runtime.gc;
using runtime;
using vein.extensions;
using vein.runtime;
using static OpCodeValue;
using static vein.runtime.VeinTypeCode;
using static WNE;
public unsafe partial struct VirtualMachine : IDisposable
{
    void ForceThrow(RuntimeIshtarClass* clazz, stackval* sp, CallFrame* invocation, string? msg = null)
    {
        CallFrame.FillStackTrace(invocation);
        var exception = gc->AllocObject(clazz, invocation);
        sp->data.p = (nint)exception;
        sp->type = TYPE_CLASS;

        if (msg is null) return;

        if (clazz->FindField("message") is null)
        {
            FastFail(TYPE_MISMATCH, $"Class '{clazz->FullName->NameWithNS}' is not contained 'message' field.", invocation);
            return;
        }

        exception->vtable[clazz->Field["message"]->vtable_offset]
            = gc->ToIshtarObject(msg, invocation);
    }

    void jump_now(uint* start, ref uint* ip, MetaMethodHeader* mh, CallFrame* invocation)
    {
        var labelKey = mh->labels->Get((int)*ip);
        if (mh->labels_map->TryGetValue(labelKey, out var label))
            ip = start + label.pos - 1;
        else
            FastFail(PROTECTED_ZONE_LABEL_CORRUPT, "[jump_now] cannot find protected zone label", invocation);
    }
    void jump_to(int index, uint* start, ref uint* ip, MetaMethodHeader* mh, CallFrame* invocation)
    {
        if (mh->labels_map->TryGetValue(mh->labels->Get(index), out var label))
            ip = start + label.pos - 1;
        else FastFail(PROTECTED_ZONE_LABEL_CORRUPT, "[jump_to] cannot find protected zone label", invocation);
    }
    uint* get_jumper(int index, uint* start, MetaMethodHeader* mh, CallFrame* invocation)
    {
        if (mh->labels_map->TryGetValue(mh->labels->Get(index), out var label))
            return start + label.pos - 1;
        FastFail(PROTECTED_ZONE_LABEL_CORRUPT, "[get_jumper] cannot find protected zone label", invocation);
        return null;
    }

    public void exec_method(CallFrame* invocation)
    {
        if (HasFaulted())
            return;
        FastFail(invocation->method is null, ACCESS_VIOLATION, "unexpected call frame method pointer corrupted.", invocation);
        FastFail((invocation->method->Flags & MethodFlags.Abstract) != 0, EXECUTION_CORRUPTED, "unexpected call abstract method", invocation);

        if (invocation->method->IsJitted)
        {
            println($".jit> {invocation->method->Owner->Name}::{invocation->method->Name} [jitted]");
            exec_method_jitted(invocation);
            return;
        }

        // Eager JIT: compiler marked this method as JIT-eligible, compile on first call
        if (!@ref->Config.DisableJIT && !invocation->method->JitRejected &&
            (invocation->method->Flags & MethodFlags.Jit) != 0 && invocation->method->PIInfo.compiled_func_ref == 0)
        {
            var allocator = IshtarGC.CreateAllocatorWithParent(invocation->method);
            if (jit.MethodCompiler.TryJitCompile(invocation->method, allocator))
            {
                println($".jit> {invocation->method->Owner->Name}::{invocation->method->Name} jitted success, call now...");
                exec_method_jitted(invocation);
                return;
            }
            // Compilation failed — print reason
            println($".jit-reject> {invocation->method->Owner->Name}::{invocation->method->Name} — {invocation->method->JitRejectReasonCode}");
        }

        var tag = Profiler.Begin($"vm:exec:({invocation->method->RawName})");

        if (!@ref->Config.DisableValidationInvocationArgs)
        {
            var argsLen = invocation->method->ArgLength;

            for (int i = 0; i != argsLen; i++)
            {
                if (invocation->args[i].type > TYPE_NULL)
                    FastFail(STATE_CORRUPT, $"[arg validation] argument [{i}/{argsLen}] for [{invocation->method->Name}] has corrupted", invocation);
            }
        }

        println($".frame> {invocation->method->Owner->Name}::{invocation->method->Name}");

        var _module = invocation->method->Owner->Owner;
        var mh = invocation->method->Header;
        FastFail(mh is null, MISSING_METHOD, "method code is zero", invocation);
            
        var args = invocation->args;

        var locals = default(SmartPointer<stackval>);

        var ip = mh->code;

        const int STACK_VIOLATION_LEVEL_SIZE = 32;

        // todo, revert to stackalloc
        var stack = stackval.Allocate(invocation, (short)(mh->max_stack + STACK_VIOLATION_LEVEL_SIZE));
            
        create_violation_zone_for_stack(stack, STACK_VIOLATION_LEVEL_SIZE);

        var sp = stack.Ref + STACK_VIOLATION_LEVEL_SIZE;
        var sp_start = sp;
        var start = ip;

        long getStackLen() => sp - sp_start;

        var end = mh->code + mh->code_size;
        var end_stack = sp + mh->max_stack;
        uint* endfinally_ip = null;
        var zone = default(ProtectedZone*);
        tag.Dispose();
        var stopwatch = new Stopwatch();

        // --- Async Resume: restore saved state if this is a resumed frame ---
        if (invocation->resumeState != null)
        {
            var rs = invocation->resumeState;
            invocation->resumeState = null;

            // Restore IP to the saved position
            ip = rs->savedIP;

            // Restore eval stack snapshot
            if (rs->evalStackDepth > 0)
            {
                Buffer.MemoryCopy(rs->evalStack, sp_start,
                    rs->evalStackDepth * sizeof(stackval),
                    rs->evalStackDepth * sizeof(stackval));
                sp = sp_start + rs->evalStackDepth;
            }

            // Push the awaited job's result onto the eval stack
            if (invocation->awaitResult.type != TYPE_VOID)
            {
                *sp = invocation->awaitResult;
                sp++;
            }

            // Restore locals
            if (rs->localsCount > 0)
            {
                locals = stackval.Allocate(invocation, (ushort)rs->localsCount);
                Buffer.MemoryCopy(rs->locals, locals.Ref,
                    rs->localsCount * sizeof(stackval),
                    rs->localsCount * sizeof(stackval));
            }

            // Free the suspended frame (state is now restored into locals)
            FreeSuspendedFrame(rs);
        }
        // --- End Async Resume ---

        while (true)
        {
            vm_cycle_start:
            if (HasFaulted())
                return;
            invocation->last_ip = (OpCodeValue)(ushort)*ip;
            println($".{invocation->last_ip} 0x{(nint)ip:X} [sp: {getStackLen()}]");

            if (!invocation->exception.IsDefault() && invocation->level == 0)
                return;
            FastFail(ip >= end, END_EXECUTE_MEMORY, "unexpected end of executable memory.", invocation);
            FastFail(sp >= end_stack, OVERFLOW, "stack overflow detected.", invocation);
            FastFail(sp < sp_start, OVERFLOW, "incorrect sp address beyond sp_start was detected", invocation);

            if (!assert_violation_zone_writes(invocation, stack, STACK_VIOLATION_LEVEL_SIZE))
                continue;

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
                    Assert(sp->type == TYPE_CLASS, sp->data.p != 0, STATE_CORRUPT, "", invocation);
                    ++sp;
                    ++ip;
                    break;
                case LDARG_S:
                    ++ip;
                    *sp = args[(*ip)];
                    println($"load from args ({sp->type})");
                    Assert(sp->type != TYPE_NONE, STATE_CORRUPT, "", invocation);
                    Assert(sp->type <= TYPE_NULL, STATE_CORRUPT, "", invocation);
                    Assert(sp->type == TYPE_CLASS, sp->data.p != 0, STATE_CORRUPT, "", invocation);
                    ++sp;
                    ++ip;
                    break;
                case LDC_I1_0:
                case LDC_I1_1:
                case LDC_I1_2:
                case LDC_I1_3:
                case LDC_I1_4:
                case LDC_I1_5:
                    sp->type = TYPE_I1;
                    sp->data.i = (int)(*ip) - (int)LDC_I1_0;
                    ++ip;
                    ++sp;
                    break;
                case LDC_U1_0:
                case LDC_U1_1:
                case LDC_U1_2:
                case LDC_U1_3:
                case LDC_U1_4:
                case LDC_U1_5:
                    sp->type = TYPE_U1;
                    sp->data.i = (int)(*ip) - (int)LDC_U1_0;
                    ++ip;
                    ++sp;
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
                case LDC_U2_0:
                case LDC_U2_1:
                case LDC_U2_2:
                case LDC_U2_3:
                case LDC_U2_4:
                case LDC_U2_5:
                    sp->type = TYPE_U2;
                    sp->data.i = (int)(*ip) - (int)LDC_U2_0;
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
                case LDC_U4_0:
                case LDC_U4_1:
                case LDC_U4_2:
                case LDC_U4_3:
                case LDC_U4_4:
                case LDC_U4_5:
                    sp->type = TYPE_U4;
                    sp->data.i = (int)(*ip) - (int)LDC_U4_0;
                    ++ip;
                    ++sp;
                    break;
                case LDC_U8_0:
                case LDC_U8_1:
                case LDC_U8_2:
                case LDC_U8_3:
                case LDC_U8_4:
                case LDC_U8_5:
                    sp->type = TYPE_U8;
                    sp->data.l = (*ip) - (long)LDC_U8_0;
                    ++sp;
                    ++ip;
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
                    sp->data.f_r4 = *(float*)ip;
                    ++ip;
                    ++sp;
                    break;
                case LDC_F8:
                    ++ip;
                    sp->type = TYPE_R8;
                    var f8_l = (long)*ip;
                    ++ip;
                    var f8_r = (long)*ip;
                    var f8_full = f8_r << 32 | f8_l & 0xffffffffL;
                    sp->data.f = *(double*)&f8_full;
                    ++ip;
                    ++sp;
                    break;
                case LDC_F16:
                    ++ip;
                    sp->type = TYPE_R16;
                    var bitsLen = (int)*ip;
                    ++ip;
                    // ReSharper disable once StackAllocInsideLoop
                    var bits = stackalloc int[4];
                    for (var i = 0; i < bitsLen && i < 4; i++)
                    {
                        bits[i] = (int)*ip;
                        ++ip;
                    }
                    sp->data.d = new decimal(new ReadOnlySpan<int>(bits, bitsLen));
                    ++sp;
                    break;
                case LDC_I1_S:
                    ++ip;
                    sp->type = TYPE_I1;
                    sp->data.b = (sbyte)(*ip);
                    ++ip;
                    ++sp;
                    break;
                case LDC_U1_S:
                    ++ip;
                    sp->type = TYPE_U1;
                    sp->data.ub = (byte)(*ip);
                    ++ip;
                    ++sp;
                    break;
                case LDC_I2_S:
                    ++ip;
                    sp->type = TYPE_I2;
                    sp->data.s = (short)(*ip);
                    ++ip;
                    ++sp;
                    break;
                case LDC_U2_S:
                    ++ip;
                    sp->type = TYPE_U2;
                    sp->data.us = (ushort)(*ip);
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
                case LDC_U4_S:
                    ++ip;
                    sp->type = TYPE_U4;
                    sp->data.ui = (*ip);
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
                } break;
                case LDC_U8_S:
                {
                    ++ip;
                    sp->type = TYPE_U8;
                    var t1 = (ulong)*ip;
                    ++ip;
                    var t2 = (ulong)*ip;
                    sp->data.ul = t2 << 32 | t1;
                    ++ip;
                    ++sp;
                } break;
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
                        sp->data.p = (nint)gc->AllocArray(typeID, size, 1, invocation);
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
                    sp->type = TYPE_U8;
                    sp->data.ul = ((IshtarArray*)arr)->length;
                    ++sp;
                }
                    break;
                case RET:
                    ++ip;
                    --sp;

                    // Async RET: resolve the ownerJob instead of storing returnValue
                    if (invocation->asyncJob != null)
                    {
                        if (invocation->method->ReturnType->TypeCode == TYPE_VOID)
                            invocation->asyncJob->SetCompleted();
                        else
                            invocation->asyncJob->SetResult(*sp);
                        stack.Dispose();
                        locals.Dispose();
                        return;
                    }

                    // Synchronous RET (existing behavior)
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
                        ForceThrow(KnowTypes.NullPointerException(invocation), sp, invocation);
                        goto exception_handle;
                    }
                    //FFI.StaticValidate(invocation, @this, field.Owner);
                    var value = sp;
                    var this_obj = (IshtarObject*)@this->data.p;

                    if (!@ref->Config.SkipValidateStfType && !field->FieldType.IsGeneric)
                    {
                        if ((value->type != TYPE_NULL) && field->FieldType.Class->TypeCode != value->type)
                        {
                            if (field->FieldType.Class->TypeCode != TYPE_OBJECT || value->type != TYPE_CLASS)
                            {
                                CallFrame.FillStackTrace(invocation);
                                ForceThrow(KnowTypes.IncorrectCastFault(invocation), sp, invocation,
                                    $"Cannot cast '{value->type}' to '{field->FieldType.Class->TypeCode}', maybe invalid IL");
                                goto exception_handle;
                            }
                        }
                    }

                    println($".STF -> {value->type} (to {field->Name})");

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
                        FastFail(STATE_CORRUPT, $".LDF invalid @this object loaded, TYPE_NONE, maybe corrupted IL code", invocation);
                    }

                    if (@this->type == TYPE_NULL)
                    {
                        CallFrame.FillStackTrace(invocation);
                        ForceThrow(KnowTypes.NullPointerException(invocation), sp, invocation);
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
                    println($".LDF -> {sp->type} (from {field->Name})");
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
                        ForceThrow(KnowTypes.NullPointerException(invocation), sp, invocation);
                        goto exception_handle;
                    }
                    var r = IshtarObject.IsInstanceOf(invocation, t1, t2);
                    if (r == null)
                    {
                        ForceThrow(KnowTypes.IncorrectCastFault(invocation), sp, invocation);
                        goto exception_handle;
                    }
                    ++sp;
                }
                break;
                case CAST_G:
                {
                    var type1IsGeneric = *++ip == 1;
                    var typeIndex1 = *++ip;
                    var type2IsGeneric = *++ip == 1;
                    var typeIndex2 = *++ip;

                    if (type1IsGeneric | type2IsGeneric)
                    {
                        ForceThrow(KnowTypes.IncorrectCastFault(invocation), sp, invocation);
                        goto exception_handle;
                    }

                    var fromClass = GetClass(typeIndex1, _module, invocation);
                    var toClass = GetClass(typeIndex2, _module, invocation);

                    //if (!fromClass->TypeCode.IsCompatibleNumber(toClass->TypeCode))
                    //{
                    //    ForceThrow(KnowTypes.IncorrectCastFault(invocation), sp, invocation);
                    //    goto exception_handle;
                    //}
                    (sp - 1)->type = toClass->TypeCode;
                    ip++;
                } break;
                case SEH_ENTER:
                    ip++;
                    zone = mh->exception_handler_list->Get((int)(*ip));
                    ip++;
                    break;
                case SEH_LEAVE:
                case SEH_LEAVE_S:
                    while (sp > sp_start) --sp;
                    invocation->last_ip = (OpCodeValue)(*ip);
                    if (*ip == (uint)SEH_LEAVE_S)
                    {
                        ++ip;
                        jump_now(start, ref ip, mh, invocation);
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
                    break;
                case THROW:
                    --sp;
                    if (sp->data.p == IntPtr.Zero)
                    {
                        sp->data.p = (nint)gc->AllocObject(KnowTypes.NullPointerException(invocation), invocation);
                        sp->type = TYPE_CLASS;
                    }
                    goto exception_handle;
                case NEWOBJ:
                {
                    ++ip;
                    sp->type = TYPE_CLASS;
                    sp->data.p = (nint)
                        gc->AllocObject(
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

                    var raw = gc->AllocRawValue(invocation); // TODO destroy

                    raw->type = VeinRawCode.ISHTAR_METHOD;
                    raw->data.m = method;

                    sp->type = TYPE_RAW; 
                    sp->data.p = (nint)raw;
                    ++sp;
                } break;
                case CALL_V:
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
                    else if (invocation->last_ip == CALL_V)
                    {
                        ++ip;
                        var tokenIdx = *ip;
                        var owner = readTypeName(*++ip, _module, invocation);

                        var targetMethod = GetMethod(tokenIdx, owner, _module, invocation);

                        Assert((targetMethod->Flags & MethodFlags.Static) == 0, TYPE_MISMATCH, "call.v fail", invocation);

                        // take this* ref from stack for getting overrides method from vtable
                        var shiftSp = sp - targetMethod->ArgLength;

                        if (shiftSp->type == TYPE_NULL)
                        {
                            @ref->ForceThrow(KnowTypes.NullPointerException(invocation), sp, invocation);
                            goto exception_handle;
                            return;
                        }

                        ++ip;
                        Assert(shiftSp->type == targetMethod->Owner->TypeCode, TYPE_MISMATCH, "call.v fail", invocation);
                        var targetObj = (IshtarObject*)shiftSp->data.p;

                        Assert(shiftSp->type == TYPE_CLASS, targetObj != null, GC_MOVED_UNMOVABLE_MEMORY, "closure scope deleted", invocation);

                        if (!IshtarObject.IsAssignableFrom(invocation, targetObj->clazz, targetMethod->Owner))
                        {
                            Assert(false, TYPE_MISMATCH, "call.v fail", invocation);
                            return;
                        }

                        method = (RuntimeIshtarMethod*)targetObj->vtable[targetMethod->vtable_offset];
                    }
                    else if (invocation->last_ip == CALL_SP)
                    {
                        ++ip;
                        sp--;

                        if (sp->type == TYPE_NULL)
                        {
                            ForceThrow(KnowTypes.NullPointerException(invocation), sp, invocation);
                            goto exception_handle;
                        }

                        var raw = (rawval*)sp->data.p;

                        if (raw->type == VeinRawCode.ISHTAR_ERROR)
                        {
                            ForceThrow(KnowTypes.IncorrectCastFault(invocation), sp, invocation);
                            goto exception_handle;
                        }
                        Assert(raw->type == VeinRawCode.ISHTAR_METHOD, MISSING_TYPE, "", invocation);
                        method = raw->data.m;
                    }
                    else
                        FastFail(true, EXECUTION_CORRUPTED,
                            "incorrect opcode behaviour", invocation);

                    if (invocation->last_ip != CALL_V)
                    {
                        FastFail((method->Flags & MethodFlags.Abstract) != 0, EXECUTION_CORRUPTED,
                            "opcode CALL/CALL_SP cannot execute abstract method", invocation);
                        FastFail((method->Flags & MethodFlags.Virtual) != 0, EXECUTION_CORRUPTED,
                            "opcode CALL/CALL_SP cannot execute virtual method", invocation);
                    }
                    
                    var child_frame = invocation->CreateChild(method);
                    println($".call {method->Owner->Name}::{method->Name}");
                    var method_args = gc->AllocateStack(child_frame, method->ArgLength);
                    for (int i = 0, y = method->ArgLength - 1; i != method->ArgLength; i++, y--)
                    {
                        var _a = method->Arguments->Get((method->ArgLength - 1) - i); // TODO, type eq validate
                        --sp;
                        if (!@ref->Config.CallOpCodeSkipValidateArgs)
                        {
                            println(_a->Type.IsGeneric
                                ? $"@@@<< {StringStorage.GetString(_a->Name, invocation)}: {StringStorage.GetString(_a->Type.TypeArg->Name, invocation)}"
                                : $"@@@<< {StringStorage.GetString(_a->Name, invocation)}: {_a->Type.Class->FullName->NameWithNS}");

                            if (_a->Type.IsGeneric)
                                continue;
                            var arg_class = _a->Type.Class;

                            if (arg_class->Name is not "Object" and not "ValueType")
                            {
                                var sp_obj = IshtarMarshal.Boxing(invocation, sp);

                                if (sp_obj == null)
                                {
                                    FastFail(STATE_CORRUPT, $"sp object is null", invocation);
                                    return;
                                }
                                var sp_class = sp_obj->clazz;

                                if (sp_class == null)
                                {
                                    FastFail(STATE_CORRUPT, $"sp class is null", invocation);
                                    return;
                                }

                                if (sp_class->ID != arg_class->ID)
                                {
                                    if (!sp_class->IsInner(arg_class))
                                    {
                                        FastFail(TYPE_MISMATCH,
                                            $"Argument '{StringStorage.GetString(_a->Name, invocation)}: {(_a->Type.IsGeneric ? StringStorage.GetString(_a->Type.TypeArg->Name, invocation) : _a->Type.Class->Name)}'" +
                                            $" is not matched for '{method->Name}' function.",
                                            invocation);
                                        break;
                                    }
                                }
                            }
                        }

                        println($".arg {method->Owner->Name}::{method->Name} (argument {y} is {sp->type} type) sp: {getStackLen()}");

                        if (sp->type > TYPE_NULL)
                            FastFail(STATE_CORRUPT, $"[call arg validation] trying fill corrupted argument [{y}/{method->ArgLength}] for [{method->Name}]", invocation);
                        Assert(sp->type == TYPE_CLASS, sp->data.p != 0, GC_MOVED_UNMOVABLE_MEMORY, "argument incorrect, maybe gc dropped memory from callframe", invocation);
                        method_args[y] = *sp;
                    }

                    child_frame->args = method_args;

                    var isAsyncMethod = (method->Flags & MethodFlags.Async) != 0;

                    if (isAsyncMethod)
                    {
                        // Async method call: create a Job, attach to frame, execute
                        // The method may complete synchronously or suspend at AWAIT
                        var job = IshtarAsyncJob.Create(@ref);
                        child_frame->asyncJob = job;

                        // Create a Job<T> IshtarObject to push onto caller's stack
                        var jobClass = method->ReturnType; // This is Job<T>
                        var jobObj = gc->AllocObject(jobClass, invocation);
                        SetJobOnObject(jobObj, job, invocation);
                        job->owner = jobObj;

                        // Execute the async method body
                        // It will either:
                        // a) Complete synchronously (no pending AWAIT hit) → job is Completed after return
                        // b) Suspend at AWAIT → job stays Pending, frame is saved as continuation
                        if (method->IsExtern)
                            exec_method_native(child_frame);
                        else
                            exec_method(child_frame);

                        // If method completed synchronously without suspending,
                        // check if it resolved the job via RET
                        if (!child_frame->exception.IsDefault() && job->state == JobState.Pending)
                        {
                            // Method threw synchronously (before any AWAIT) — capture the fault
                            // into the Job rather than propagating to the caller. This is the
                            // standard async contract: the caller observes the exception only
                            // when it AWAITs the returned Job, not at the call site.
                            job->SetException(child_frame->exception);
                        }
                        else if (job->state == JobState.Pending && child_frame->exception.IsDefault())
                        {
                            // Method suspended at AWAIT — do NOT free args or dispose child_frame.
                            // Ownership of method_args is transferred to the SuspendedFrame
                            // which will be freed after resume completes.
                            sp->type = TYPE_CLASS;
                            sp->data.p = (nint)jobObj;
                            sp++;
                            println($".call.async suspended {method->Owner->Name}::{method->Name}, sp: {getStackLen()}");
                            break;
                        }

                        // Push the Job<T> object onto caller's eval stack regardless of job state
                        sp->type = TYPE_CLASS;
                        sp->data.p = (nint)jobObj;
                        sp++;

                        println($".call.async after {method->Owner->Name}::{method->Name}, sp: {getStackLen()}");
                        gc->FreeStack(child_frame, method_args, method->ArgLength);
                        child_frame->Dispose();
                    }
                    else
                    {
                        // Synchronous method call (existing behavior)
                        if (method->IsJitted)
                        {
                            println($".call jit> {method->Owner->Name}::{method->Name} [jitted]");
                            exec_method_jitted(child_frame);
                        }
                        else if (method->IsExtern)
                            exec_method_native(child_frame);
                        else
                            task_scheduler->execute_method(child_frame);

                        if (!child_frame->exception.IsDefault())
                        {
                            sp->type = TYPE_CLASS;
                            sp->data.p = (nint)child_frame->exception.value;
                            invocation->exception = child_frame->exception;

                            gc->FreeStack(child_frame, method_args, method->ArgLength);
                            child_frame->Dispose();
                            goto exception_handle;
                        }

                        if (method->ReturnType->TypeCode != TYPE_VOID)
                        {
                            invocation->assert(!child_frame->returnValue.IsNull(), STATE_CORRUPT, "Method has return zero memory.");
                            *sp = child_frame->returnValue[0];
                            Assert(sp->type != TYPE_NONE, STATE_CORRUPT, "returnValue from child frame is bad", child_frame);
                            Assert(sp->type <= TYPE_NULL, STATE_CORRUPT, "returnValue from child frame is bad", child_frame);
                            child_frame->returnValue.Dispose();

                            sp++;
                        }

                        println($".call after {method->Owner->Name}::{method->Name}, sp: {getStackLen()}");
                        gc->FreeStack(child_frame, method_args, method->ArgLength);
                        child_frame->Dispose();
                        gc->Collect();
                    }
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

                        Assert(type->TypeCode != TYPE_NONE, TYPE_MISMATCH, "in LOC_INIT not allowed using undefined type", invocation);
                        Assert(type->TypeCode != TYPE_VOID, TYPE_MISMATCH, "in LOC_INIT not allowed using void type", invocation);

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

                    *sp = _comparer(second, first, invocation->last_ip, invocation);
                    println($"$$$ {invocation->last_ip} : {debug_comparer_get_symbol(second, first, invocation->last_ip)} == {sp->data.i == 1}");
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
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U1:
                                if (first.data.ub < second.data.ub)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I2:
                                if (first.data.s < second.data.s)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U2:
                                if (first.data.us < second.data.us)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I4:
                                if (first.data.i < second.data.i)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U4:
                                if (first.data.ui < second.data.ui)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I8:
                                if (first.data.l < second.data.l)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U8:
                                if (first.data.ul < second.data.ul)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R2:
                                if (first.data.hf < second.data.hf)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R4:
                                if (first.data.f_r4 < second.data.f_r4)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R8:
                                if (first.data.f < second.data.f)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R16:
                                if (first.data.d < second.data.d)
                                    jump_now(start, ref ip, mh, invocation);
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
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_U1:
                            if (first.data.ub != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_I2:
                            if (first.data.s != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_U2:
                            if (first.data.us != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_I4:
                        case TYPE_BOOLEAN:
                            if (first.data.i != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_U4:
                            if (first.data.ui != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_I8:
                            if (first.data.l != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_U8:
                            if (first.data.ul != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_R2:
                            if ((float)first.data.hf != 0.0f)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_R4:
                            if (first.data.f_r4 != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_R8:
                            if (first.data.f != 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_R16:
                            if (first.data.d != 0)
                                jump_now(start, ref ip, mh, invocation);
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
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U1:
                                if (first.data.ub <= second.data.ub)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I2:
                                if (first.data.s <= second.data.s)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U2:
                                if (first.data.us <= second.data.us)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I4:
                                if (first.data.i <= second.data.i)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U4:
                                if (first.data.ui <= second.data.ui)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I8:
                                if (first.data.l <= second.data.l)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U8:
                                if (first.data.ul <= second.data.ul)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R2:
                                if (first.data.hf <= second.data.hf)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R4:
                                if (first.data.f_r4 <= second.data.f_r4)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R8:
                                if (first.data.f <= second.data.f)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R16:
                                if (first.data.d <= second.data.d)
                                    jump_now(start, ref ip, mh, invocation);
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
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U1:
                                if (first.data.ub != second.data.ub)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I2:
                                if (first.data.s != second.data.s)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U2:
                                if (first.data.us != second.data.us)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I4:
                                if (first.data.i != second.data.i)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U4:
                                if (first.data.ui != second.data.ui)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_I8:
                                if (first.data.l != second.data.l)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_U8:
                                if (first.data.ul != second.data.ul)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R2:
                                if (first.data.hf != second.data.hf)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R4:
                                if (first.data.f_r4 != second.data.f_r4)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R8:
                                if (first.data.f != second.data.f)
                                    jump_now(start, ref ip, mh, invocation);
                                else ++ip; break;
                            case TYPE_R16:
                                if (first.data.d != second.data.d)
                                    jump_now(start, ref ip, mh, invocation);
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
                    jump_now(start, ref ip, mh, invocation);
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
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_U1:
                            if (first.data.ub == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_I2:
                            if (first.data.s == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_U2:
                            if (first.data.us == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_I4:
                        case TYPE_BOOLEAN:
                            if (first.data.i == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_U4:
                            if (first.data.ui == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_I8:
                            if (first.data.l == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_U8:
                            if (first.data.ul == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_R2:
                            if (first.data.hf == (Half)0f)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_R4:
                            if (first.data.f_r4 == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_R8:
                            if (first.data.f == 0)
                                jump_now(start, ref ip, mh, invocation);
                            else ++ip; break;
                        case TYPE_R16:
                            if (first.data.d == 0)
                                jump_now(start, ref ip, mh, invocation);
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
                case LDLOC_5:
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
                case STLOC_5:
                    --sp;
                    Assert(sp->type != TYPE_NONE, STATE_CORRUPT, $"{invocation->last_ip}", invocation);
                    Assert(sp->type <= TYPE_NULL, STATE_CORRUPT, $"{invocation->last_ip}", invocation);
                    locals[(*ip) - (int)STLOC_0] = *sp;
                    println($"stage to locals ({sp->type})");
                    ++ip;
                    break;
                case STLOC_S:
                    ++ip;
                    --sp;
                    Assert(sp->type != TYPE_NONE, STATE_CORRUPT, "STLOC_S", invocation);
                    Assert(sp->type <= TYPE_NULL, STATE_CORRUPT, "STLOC_S", invocation);
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
                    println("*** BREAK ***");
                    Console.ReadKey();
                    break;
                case RESERVED_2:
                    ++ip;
                    println($"*** GC DUMP ***");
                    println($"\talive_objects: {gc->alive_objects}");
                    println($"\ttotal_allocations: {gc->total_allocations}");
                    println($"\ttotal_bytes_requested: {gc->total_bytes_requested}");
                    println($"*** END GC DUMP ***");
                    break;
                case LDC_STR:
                {
                    ++ip;
                    sp->type = TYPE_STRING;
                    var str = _module->GetConstStringByIndex((int) *ip);
                    sp->data.p = (nint)gc->ToIshtarObject(str, invocation);
                    ++sp;
                    ++ip;
                }
                    break;
                case AWAIT:
                {
                    ++ip;
                    --sp;

                    // Top of eval stack is a Job<T> object (IshtarObject* with class=Job<T>)
                    var jobObj = (IshtarObject*)sp->data.p;
                    var job = GetJobFromObject(jobObj, invocation);

                    if (job == null)
                    {
                        FastFail(STATE_CORRUPT, "AWAIT: object on stack is not a Job or is null", invocation);
                        break;
                    }

                    // Fast path: job already completed — push result and continue
                    if (job->state == JobState.Completed)
                    {
                        if (job->result.type != TYPE_VOID)
                        {
                            *sp = job->result;
                            sp++;
                        }
                        break;
                    }

                    // Fast path: job already faulted — propagate exception
                    if (job->state == JobState.Faulted)
                    {
                        sp->type = TYPE_CLASS;
                        sp->data.p = (nint)job->exception.value;
                        invocation->exception = job->exception;
                        goto exception_handle;
                    }

                    // Check if inside SEH — not supported, crash
                    if (zone != null)
                    {
                        FastFail(EXECUTION_CORRUPTED, "AWAIT inside try/catch/finally is not yet supported", invocation);
                        break;
                    }

                    // Slow path: job is pending — suspend this frame
                    var stackDepth = (int)(sp - sp_start);
                    var localsCount = locals.IsNull() ? 0 : (int)locals.size;

                    // Determine the ownerJob for this async method
                    // It was stored in the invocation frame when the async method was entered
                    var ownerJob = invocation->asyncJob;

                    var suspended = CaptureFrame(
                        invocation->method,
                        ip, // resume point: right after the AWAIT opcode
                        sp_start,
                        stackDepth,
                        locals.IsNull() ? null : locals.Ref,
                        localsCount,
                        invocation->parent,
                        args,
                        invocation->method->ArgLength,
                        mh->max_stack,
                        ownerJob,
                        job);

                    if (job->TryRegisterContinuation(suspended))
                    {
                        // Frame is suspended — do NOT dispose stack/locals, 
                        // they're snapshotted in SuspendedFrame.
                        // Return without cleanup — the frame will be resumed later.
                        return;
                    }
                    else
                    {
                        // Job completed between our check and registration — continue synchronously
                        FreeSuspendedFrame(suspended);
                        if (job->state == JobState.Completed && job->result.type != TYPE_VOID)
                        {
                            *sp = job->result;
                            sp++;
                        }
                        else if (job->state == JobState.Faulted)
                        {
                            sp->type = TYPE_CLASS;
                            sp->data.p = (nint)job->exception.value;
                            invocation->exception = job->exception;
                            goto exception_handle;
                        }
                    }
                }
                    break;
                case BOX:
                {
                    // Box a value type: top of stack is value, convert to heap-allocated object reference
                    ++ip;
                    var typeIdx = *ip;
                    ++ip;
                    --sp;
                    var boxClass = GetClass(typeIdx, _module, invocation);
                    var boxed = gc->AllocObject(boxClass, invocation);
                    // Store value directly into the boxed object's !!value vtable slot
                    var valField = boxClass->FindField("!!value");
                    if (valField is not null)
                    {
                        if (sp->type == TYPE_R16)
                        {
                            FastFail(STATE_CORRUPT, "BOX: TYPE_R16 is not supported", invocation);
                            return;
                        }
                        boxed->vtable[valField->vtable_offset] = (void*)sp->data.p;
                    }
                    sp->type = TYPE_CLASS;
                    sp->data.p = (nint)boxed;
                    ++sp;
                }
                    break;
                case UNBOX:
                {
                    // Unbox: top of stack is object reference, extract value
                    ++ip;
                    var typeIdx = *ip;
                    ++ip;
                    --sp;
                    var unboxClass = GetClass(typeIdx, _module, invocation);
                    var obj = (IshtarObject*)sp->data.p;
                    if (obj == null)
                    {
                        ForceThrow(KnowTypes.NullPointerException(invocation), sp, invocation);
                        goto exception_handle;
                    }

                    if (obj->clazz == null || obj->clazz->ID != unboxClass->ID)
                    {
                        CallFrame.FillStackTrace(invocation);
                        ForceThrow(KnowTypes.IncorrectCastFault(invocation), sp, invocation,
                            $"Cannot unbox '{obj->clazz->Name}' as '{unboxClass->Name}'.");
                        goto exception_handle;
                    }

                    var unboxField = obj->clazz->FindField("!!value");
                    if (unboxField is not null)
                    {
                        var fieldType = unboxField->FieldType.Class->TypeCode;
                        if (fieldType == TYPE_R16)
                        {
                            FastFail(STATE_CORRUPT, "UNBOX: TYPE_R16 is not supported", invocation);
                            return;
                        }
                        sp->type = fieldType;
                        sp->data.p = (nint)obj->vtable[unboxField->vtable_offset];
                    }
                    else
                    {
                        sp->type = unboxClass->TypeCode;
                        sp->data.p = (nint)obj;
                    }
                    ++sp;
                }
                    break;
                case INITSTRUCT:
                {
                    // Zero-initialize a struct on the evaluation stack
                    ++ip;
                    var typeIdx = *ip;
                    ++ip;
                    var structClass = GetClass(typeIdx, _module, invocation);

                    var structObj = gc->AllocObject(structClass, invocation);

                    // AllocObject() copies the class vtable where instance field slots default to null.
                    // For INITSTRUCT we want default-zero semantics for primitive fields.
                    for (var i = 0; i != structClass->Fields->Count; i++)
                    {
                        var f = structClass->Fields->Get(i);
                        if ((f->Flags & FieldFlags.Static) != 0) continue;

                        var ft = f->FieldType.Class;
                        if (ft is null) continue;

                        if (ft->IsPrimitive && ft->TypeCode != TYPE_RAW)
                        {
                            var zero = new stackval { type = ft->TypeCode };
                            structObj->vtable[f->vtable_offset] = IshtarMarshal.Boxing(invocation, &zero);
                        }
                    }

                    sp->type = TYPE_CLASS;
                    sp->data.p = (nint)structObj;
                    ++sp;
                }
                    break;
                case CPSTRUCT:
                {
                    // Copy struct value (top of stack) — creates a shallow copy
                    ++ip;
                    var typeIdx = *ip;
                    ++ip;
                    --sp;
                    var cpClass = GetClass(typeIdx, _module, invocation);
                    var srcObj = (IshtarObject*)sp->data.p;
                    if (srcObj == null)
                    {
                        ForceThrow(KnowTypes.NullPointerException(invocation), sp, invocation);
                        goto exception_handle;
                    }
                    var copyObj = gc->AllocObject(cpClass, invocation);
                    IshtarUnsafe.CopyBlock(copyObj->vtable, srcObj->vtable,
                        (uint)cpClass->computed_size * (uint)sizeof(void*));
                    sp->type = TYPE_CLASS;
                    sp->data.p = (nint)copyObj;
                    ++sp;
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

            var tryEndAddr = zone != default ? get_jumper(zone->TryEndLabel, start, mh, invocation) : null;

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
                    jump_to(label_addr, start, ref ip, mh, invocation);
                    ++sp;
                }
                else fill_frame_exception();
            }
            else
            {
                fill_frame_exception();
                if (invocation->level == 0)
                    handle_unhandled_exception(invocation);
                return;
            }

            goto vm_cycle_start;
        }
    }

    private void handle_unhandled_exception(CallFrame* frame)
    {
        if (!frame->exception.IsDefault())
        {
            var exceptionValue = frame->exception.value;
            var exceptionClass = exceptionValue->clazz;

            if (exceptionClass->FindField("message") is null)
            {
                trace.error($"unhandled exception '{frame->exception.value->clazz->Name}' was thrown. \n" +
                                $"{frame->exception.GetStackTrace()}");
            }
            else
            {
                var msg = exceptionValue->vtable[exceptionClass->Field["message"]->vtable_offset];
                if (msg is null)
                {
                    trace.error($"unhandled exception '{frame->exception.value->clazz->Name}' was thrown. \n" +
                                    $"{frame->exception.GetStackTrace()}");
                }
                else
                {
                    var message = IshtarMarshal.ToDotnetString((IshtarObject*)msg, frame);
                    trace.error(
                        $"""
                         unhandled exception '{frame->exception.value->clazz->Name}' was thrown.
                         '{message}'
                         {frame->exception.GetStackTrace()}
                         """);
                }
            }
            halt();
        }
    }
}
