namespace ishtar;

using libuv;
using vein.runtime;
using static vein.runtime.VeinTypeCode;

public unsafe partial struct VirtualMachine 
{
    public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, params (string name, VeinTypeCode code)[] args)
    {
        var converter_args = RuntimeMethodArgument.Create(Types, args, @ref);
        return InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags, converter_args);
    }

    public RuntimeIshtarMethod* GetOrCreateSpecialMethod(string name)
    {
        var exist = InternalClass->FindMethod(name,
            x => x->Name.Contains(name));

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
        => InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags, RuntimeMethodArgument.Create(@ref, args, @ref));

    public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, params VeinArgumentRef[] args)
        => InternalClass->DefineMethod(name, returnType, flags, RuntimeMethodArgument.Create(@ref, args, @ref));

    public RuntimeIshtarMethod* DefineEmptySystemMethod(string name)
        => CreateInternalMethod(name, MethodFlags.Extern, TYPE_VOID.AsRuntimeClass(Types), Array.Empty<VeinArgumentRef>());


    public bool HasFaulted() => @ref->currentFault is not null;
    public bool HasStopped() => HasFaulted() || @ref->hasStopRequired;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastFail(WNE type, string msg, CallFrame* frame)
    {
        watcher.FastFail(type, msg, frame);
        watcher.ValidateLastError();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Assert(UV_ERR error, WNE wne, string proc, CallFrame* frame)
    {
        if (error != UV_ERR.OK)
            FastFail(wne, $"{proc} {error}", frame);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FastFail(bool assert, WNE type, string msg, CallFrame* frame)
    {
        if (!assert) return;
        watcher.FastFail(type, msg, frame);
        watcher.ValidateLastError();
    }

    [Conditional("DEBUG")]
    public void println(string str) => trace.log(str);

    public void halt(int exitCode = -1)
    {
#if DEBUG
        trace.log($"exit code is {exitCode}");
        trace.log("Press ENTER to exit...");
        while (System.Console.ReadKey().Key != ConsoleKey.Enter) Thread.Sleep(1);
#endif
        Environment.Exit(exitCode);
    }

    public static void Assert(bool conditional, WNE type, string msg, CallFrame* frame = null)
    {
        if (conditional)
            return;
        if (frame is null)
            return;
        frame->vm->FastFail(type, $"static assert failed: {msg}", frame);
    }

    public static void Assert(bool when, bool conditional, WNE type, string msg, CallFrame* frame = null)
    {
        if (!when)
            return;
        if (conditional)
            return;
        if (frame is null)
            throw new BadImageFormatException();
        frame->vm->FastFail(type, $"static assert failed: {msg}", frame);
    }

    public static void GlobalPrintln(string empty) { }
}
