namespace ishtar;

using vein.runtime;

/// <summary>
/// Marshals calls from the VM's stackval-based calling convention to native function pointers.
/// Replacement for the LLVM JIT compilation pipeline — acts as a managed DllImport analog.
/// </summary>
public unsafe struct NativeCallMarshaller
{
    /// <summary>
    /// Links an external native method by loading the native library and resolving the symbol.
    /// Generates a compiled_func_ref that can be called as delegate*&lt;stackval*, int, stackval&gt;.
    /// </summary>
    public static void LinkNativeMethod(RuntimeIshtarMethod* method, string moduleName, string fnName)
    {
        var moduleHandle = NativeLibrary.Load(moduleName);
        var symbolHandle = NativeLibrary.GetExport(moduleHandle, fnName);

        method->PIInfo = new PInvokeInfo
        {
            module_handle = moduleHandle,
            symbol_handle = symbolHandle,
            isInternal = false
        };
    }

    /// <summary>
    /// Execute an external native call by marshalling stackval arguments to native types,
    /// invoking the native function, and marshalling the return value back.
    /// </summary>
    public static stackval Invoke(CallFrame* frame)
    {
        var method = frame->method;
        ref var pinfo = ref method->PIInfo;
        var fnPtr = pinfo.symbol_handle;
        var argCount = method->ArgLength;
        var returnTypeCode = method->ReturnType->TypeCode;

        // Fast path for common signatures
        if (argCount == 0)
            return InvokeNoArgs(fnPtr, returnTypeCode);

        // General path: marshal arguments via platform invoke
        return InvokeWithArgs(fnPtr, frame->args, argCount, method, returnTypeCode);
    }

    private static stackval InvokeNoArgs(nint fnPtr, VeinTypeCode returnTypeCode)
    {
        var result = new stackval();

        switch (returnTypeCode)
        {
            case VeinTypeCode.TYPE_VOID:
                ((delegate* unmanaged[Cdecl]<void>)fnPtr)();
                result.type = VeinTypeCode.TYPE_VOID;
                break;
            case VeinTypeCode.TYPE_I1:
                result.data.b = ((delegate* unmanaged[Cdecl]<sbyte>)fnPtr)();
                result.type = VeinTypeCode.TYPE_I1;
                break;
            case VeinTypeCode.TYPE_U1:
                result.data.ub = ((delegate* unmanaged[Cdecl]<byte>)fnPtr)();
                result.type = VeinTypeCode.TYPE_U1;
                break;
            case VeinTypeCode.TYPE_I2:
                result.data.s = ((delegate* unmanaged[Cdecl]<short>)fnPtr)();
                result.type = VeinTypeCode.TYPE_I2;
                break;
            case VeinTypeCode.TYPE_U2:
                result.data.us = ((delegate* unmanaged[Cdecl]<ushort>)fnPtr)();
                result.type = VeinTypeCode.TYPE_U2;
                break;
            case VeinTypeCode.TYPE_I4:
                result.data.i = ((delegate* unmanaged[Cdecl]<int>)fnPtr)();
                result.type = VeinTypeCode.TYPE_I4;
                break;
            case VeinTypeCode.TYPE_U4:
                result.data.ui = ((delegate* unmanaged[Cdecl]<uint>)fnPtr)();
                result.type = VeinTypeCode.TYPE_U4;
                break;
            case VeinTypeCode.TYPE_I8:
                result.data.l = ((delegate* unmanaged[Cdecl]<long>)fnPtr)();
                result.type = VeinTypeCode.TYPE_I8;
                break;
            case VeinTypeCode.TYPE_U8:
                result.data.ul = ((delegate* unmanaged[Cdecl]<ulong>)fnPtr)();
                result.type = VeinTypeCode.TYPE_U8;
                break;
            case VeinTypeCode.TYPE_R4:
                result.data.f_r4 = ((delegate* unmanaged[Cdecl]<float>)fnPtr)();
                result.type = VeinTypeCode.TYPE_R4;
                break;
            case VeinTypeCode.TYPE_R8:
                result.data.f = ((delegate* unmanaged[Cdecl]<double>)fnPtr)();
                result.type = VeinTypeCode.TYPE_R8;
                break;
            case VeinTypeCode.TYPE_RAW:
                result.data.p = ((delegate* unmanaged[Cdecl]<nint>)fnPtr)();
                result.type = VeinTypeCode.TYPE_RAW;
                break;
            default:
                result.data.p = ((delegate* unmanaged[Cdecl]<nint>)fnPtr)();
                result.type = returnTypeCode;
                break;
        }

        return result;
    }

    private static stackval InvokeWithArgs(nint fnPtr, stackval* args, int argCount,
        RuntimeIshtarMethod* method, VeinTypeCode returnTypeCode)
    {
        // Pack arguments into nint-sized slots for the generic invoker
        var nativeArgs = stackalloc nint[argCount];

        for (var i = 0; i < argCount; i++)
        {
            var arg = &args[i];
            nativeArgs[i] = MarshalArgToNative(arg);
        }

        var result = new stackval();
        result.type = returnTypeCode;

        switch (argCount)
        {
            case 1:
                InvokeN1(fnPtr, nativeArgs, returnTypeCode, &result);
                break;
            case 2:
                InvokeN2(fnPtr, nativeArgs, returnTypeCode, &result);
                break;
            case 3:
                InvokeN3(fnPtr, nativeArgs, returnTypeCode, &result);
                break;
            case 4:
                InvokeN4(fnPtr, nativeArgs, returnTypeCode, &result);
                break;
            default:
                InvokeNGeneric(fnPtr, nativeArgs, argCount, returnTypeCode, &result);
                break;
        }

        return result;
    }

    private static nint MarshalArgToNative(stackval* arg)
    {
        return arg->type switch
        {
            VeinTypeCode.TYPE_I1 => arg->data.b,
            VeinTypeCode.TYPE_U1 => arg->data.ub,
            VeinTypeCode.TYPE_I2 => arg->data.s,
            VeinTypeCode.TYPE_U2 => arg->data.us,
            VeinTypeCode.TYPE_I4 => arg->data.i,
            VeinTypeCode.TYPE_U4 => (nint)arg->data.ui,
            VeinTypeCode.TYPE_I8 => (nint)arg->data.l,
            VeinTypeCode.TYPE_U8 => (nint)arg->data.ul,
            VeinTypeCode.TYPE_R4 => *(nint*)&arg->data.f_r4,
            VeinTypeCode.TYPE_R8 => *(nint*)&arg->data.f,
            VeinTypeCode.TYPE_RAW => arg->data.p,
            VeinTypeCode.TYPE_STRING => arg->data.p,
            VeinTypeCode.TYPE_CLASS => arg->data.p,
            _ => arg->data.p
        };
    }

    private static void StoreResult(nint rawResult, VeinTypeCode returnTypeCode, stackval* result)
    {
        switch (returnTypeCode)
        {
            case VeinTypeCode.TYPE_VOID:
                break;
            case VeinTypeCode.TYPE_I1:
                result->data.b = (sbyte)rawResult;
                break;
            case VeinTypeCode.TYPE_U1:
                result->data.ub = (byte)rawResult;
                break;
            case VeinTypeCode.TYPE_I2:
                result->data.s = (short)rawResult;
                break;
            case VeinTypeCode.TYPE_U2:
                result->data.us = (ushort)rawResult;
                break;
            case VeinTypeCode.TYPE_I4:
                result->data.i = (int)rawResult;
                break;
            case VeinTypeCode.TYPE_U4:
                result->data.ui = (uint)rawResult;
                break;
            case VeinTypeCode.TYPE_I8:
                result->data.l = (long)rawResult;
                break;
            case VeinTypeCode.TYPE_U8:
                result->data.ul = (ulong)rawResult;
                break;
            case VeinTypeCode.TYPE_R4:
                result->data.f_r4 = *(float*)&rawResult;
                break;
            case VeinTypeCode.TYPE_R8:
                result->data.f = *(double*)&rawResult;
                break;
            default:
                result->data.p = rawResult;
                break;
        }
    }

    private static void InvokeN1(nint fnPtr, nint* args, VeinTypeCode ret, stackval* result)
    {
        if (ret == VeinTypeCode.TYPE_VOID)
        {
            ((delegate* unmanaged[Cdecl]<nint, void>)fnPtr)(args[0]);
            return;
        }
        if (ret is VeinTypeCode.TYPE_R4)
        {
            result->data.f_r4 = ((delegate* unmanaged[Cdecl]<nint, float>)fnPtr)(args[0]);
            return;
        }
        if (ret is VeinTypeCode.TYPE_R8)
        {
            result->data.f = ((delegate* unmanaged[Cdecl]<nint, double>)fnPtr)(args[0]);
            return;
        }
        var r = ((delegate* unmanaged[Cdecl]<nint, nint>)fnPtr)(args[0]);
        StoreResult(r, ret, result);
    }

    private static void InvokeN2(nint fnPtr, nint* args, VeinTypeCode ret, stackval* result)
    {
        if (ret == VeinTypeCode.TYPE_VOID)
        {
            ((delegate* unmanaged[Cdecl]<nint, nint, void>)fnPtr)(args[0], args[1]);
            return;
        }
        if (ret is VeinTypeCode.TYPE_R4)
        {
            result->data.f_r4 = ((delegate* unmanaged[Cdecl]<nint, nint, float>)fnPtr)(args[0], args[1]);
            return;
        }
        if (ret is VeinTypeCode.TYPE_R8)
        {
            result->data.f = ((delegate* unmanaged[Cdecl]<nint, nint, double>)fnPtr)(args[0], args[1]);
            return;
        }
        var r = ((delegate* unmanaged[Cdecl]<nint, nint, nint>)fnPtr)(args[0], args[1]);
        StoreResult(r, ret, result);
    }

    private static void InvokeN3(nint fnPtr, nint* args, VeinTypeCode ret, stackval* result)
    {
        if (ret == VeinTypeCode.TYPE_VOID)
        {
            ((delegate* unmanaged[Cdecl]<nint, nint, nint, void>)fnPtr)(args[0], args[1], args[2]);
            return;
        }
        if (ret is VeinTypeCode.TYPE_R4)
        {
            result->data.f_r4 = ((delegate* unmanaged[Cdecl]<nint, nint, nint, float>)fnPtr)(args[0], args[1], args[2]);
            return;
        }
        if (ret is VeinTypeCode.TYPE_R8)
        {
            result->data.f = ((delegate* unmanaged[Cdecl]<nint, nint, nint, double>)fnPtr)(args[0], args[1], args[2]);
            return;
        }
        var r = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint>)fnPtr)(args[0], args[1], args[2]);
        StoreResult(r, ret, result);
    }

    private static void InvokeN4(nint fnPtr, nint* args, VeinTypeCode ret, stackval* result)
    {
        if (ret == VeinTypeCode.TYPE_VOID)
        {
            ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, void>)fnPtr)(args[0], args[1], args[2], args[3]);
            return;
        }
        if (ret is VeinTypeCode.TYPE_R4)
        {
            result->data.f_r4 = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, float>)fnPtr)(args[0], args[1], args[2], args[3]);
            return;
        }
        if (ret is VeinTypeCode.TYPE_R8)
        {
            result->data.f = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, double>)fnPtr)(args[0], args[1], args[2], args[3]);
            return;
        }
        var r = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint>)fnPtr)(args[0], args[1], args[2], args[3]);
        StoreResult(r, ret, result);
    }

    /// <summary>
    /// Fallback for functions with more than 4 arguments.
    /// Uses a trampoline approach via marshalled delegate.
    /// </summary>
    private static void InvokeNGeneric(nint fnPtr, nint* args, int argCount, VeinTypeCode ret, stackval* result)
    {
        // For 5+ args we use a switch-based dispatch up to a reasonable maximum
        switch (argCount)
        {
            case 5:
            {
                if (ret == VeinTypeCode.TYPE_VOID)
                {
                    ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, void>)fnPtr)(
                        args[0], args[1], args[2], args[3], args[4]);
                    return;
                }
                var r = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, nint>)fnPtr)(
                    args[0], args[1], args[2], args[3], args[4]);
                StoreResult(r, ret, result);
                break;
            }
            case 6:
            {
                if (ret == VeinTypeCode.TYPE_VOID)
                {
                    ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, nint, void>)fnPtr)(
                        args[0], args[1], args[2], args[3], args[4], args[5]);
                    return;
                }
                var r = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, nint, nint>)fnPtr)(
                    args[0], args[1], args[2], args[3], args[4], args[5]);
                StoreResult(r, ret, result);
                break;
            }
            case 7:
            {
                if (ret == VeinTypeCode.TYPE_VOID)
                {
                    ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, nint, nint, void>)fnPtr)(
                        args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                    return;
                }
                var r = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, nint, nint, nint>)fnPtr)(
                    args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                StoreResult(r, ret, result);
                break;
            }
            case 8:
            {
                if (ret == VeinTypeCode.TYPE_VOID)
                {
                    ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, nint, nint, nint, void>)fnPtr)(
                        args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
                    return;
                }
                var r = ((delegate* unmanaged[Cdecl]<nint, nint, nint, nint, nint, nint, nint, nint, nint>)fnPtr)(
                    args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
                StoreResult(r, ret, result);
                break;
            }
            default:
                throw new NotSupportedException($"Native calls with {argCount} arguments are not supported. Maximum is 8.");
        }
    }
}
