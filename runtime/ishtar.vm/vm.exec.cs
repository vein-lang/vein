namespace ishtar;

using vein.runtime;
using static vein.runtime.VeinTypeCode;
using static WNE;

public unsafe partial struct VirtualMachine
{
    private void exec_method_external_native(CallFrame* frame)
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

    private void exec_method_internal_native(CallFrame* frame)
    {
        var caller = (delegate*<CallFrame*, IshtarObject**, IshtarObject*>)
                frame->method->PIInfo.compiled_func_ref;
        var args_len = frame->method->ArgLength;

        var args = stackalloc IshtarObject*[args_len];
        
        for (var i = 0; i != args_len; i++)
            args[i] = IshtarMarshal.Boxing(frame, &frame->args[i]);

        var result = caller(frame, args);

        if (result is not null && frame->method->ReturnType->TypeCode == TYPE_VOID)
        {
            FastFail(STATE_CORRUPT, $"[exec_method_internal_native] ReturnValue from {frame->method->Name} has exits, but declaration return type is void", frame);
            return;
        }
        
        if (frame->method->ReturnType->TypeCode == TYPE_VOID)
            return;
        if (frame->method->ReturnType->TypeCode == TYPE_NONE)
        {
            FastFail(STATE_CORRUPT, $"[exec_method_internal_native] ReturnValue from {frame->method->Name} has incorrect", frame);
            return;
        }

        if (result->clazz->TypeCode == TYPE_NULL)
        {
            frame->returnValue = stackval.Allocate(frame, 1);
            frame->returnValue.Ref->type = TYPE_NULL;
            frame->returnValue.Ref->data.p = 0;
            return;
        }

        if (result->clazz->TypeCode == TYPE_CLASS || result->clazz->TypeCode == TYPE_OBJECT)
        {
            frame->returnValue = stackval.Allocate(frame, 1);
            frame->returnValue.Ref->type = frame->method->ReturnType->TypeCode;
            frame->returnValue.Ref->data.p = (nint)result;
            return;
        }
        frame->returnValue = stackval.Allocate(frame, 1);
        *frame->returnValue.Ref = IshtarMarshal.UnBoxing(frame, result);
    }

    private void exec_method_native(CallFrame* frame)
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
        for (int i = 0; i < size; i++) if (stack[i].type != (VeinTypeCode)int.MaxValue || stack[i].data.l != long.MaxValue)
        {
            FastFail(STATE_CORRUPT, "stack write to an violation zone has been detected, ", frame);
            return false;
        }
        return true;
    }

}
