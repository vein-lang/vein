namespace ishtar;

using System.Text;
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

    private RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags)
        => InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags);

    public RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, params VeinArgumentRef[] args)
        => InternalClass->DefineMethod(name, TYPE_VOID.AsRuntimeClass(Types), flags, RuntimeMethodArgument.Create(@ref, args, @ref));

    private RuntimeIshtarMethod* CreateInternalMethod(string name, MethodFlags flags, RuntimeIshtarClass* returnType, params VeinArgumentRef[] args)
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
        trace.console_std_write_line($"exit code is {exitCode}");
        if (Config.PressEnterToExit)
        {
            trace.console_std_write_line("Press ENTER to exit...");
            while (System.Console.ReadKey().Key != ConsoleKey.Enter)
                Thread.Sleep(1000);
        }
        Environment.Exit(exitCode);
    }

    public static void Assert(bool conditional, WNE type, string msg, CallFrame* frame = null)
    {
        if (conditional)
            return;
        if (frame is null)
            throw new IshtarExecutionException(type, msg);
        frame->vm->FastFail(type, $"static assert failed: {msg}", frame);
    }

    public static void Assert(bool conditional, WNE type, string msg, IshtarObject* referObj)
    {
        // TODO
        return;
        if (conditional)
            return;
        var vm = referObj->clazz->Owner->vm;
        vm->WriteMemoryDumpFor(referObj);
        vm->FastFail(type, $"static assert failed: {msg}", vm->Frames->EntryPoint);
    }


    public void WriteMemoryDumpFor(IshtarObject* referObj)
    {
#if DEBUG
        var b = new StringBuilder();

        b.AppendLine($"obj: 0x{((nint)referObj):x8}");
        b.AppendLine($"gcid: {referObj->__gc_id}");
        b.AppendLine($"class_td: {referObj->ClassTraceData}");
        b.AppendLine($"create_td: {referObj->CreationTraceData}");
        b.AppendLine($"gc_flags: {referObj->flags}");
        b.AppendLine($"class: {referObj->clazz->Name}");
        b.AppendLine($"vtable_size: {referObj->vtable_size}/{referObj->clazz->vtable_size}");
        b.AppendLine($"class_flags: {referObj->clazz->Flags}");
        b.AppendLine($"vtable:");

        for (int index = 0; index < referObj->clazz->dvtable.vtable_info.Length; index++)
            b.AppendLine($"\tg {index}: {referObj->clazz->dvtable.vtable_info[index]}");
        for (int index = 0; index < referObj->clazz->dvtable.vtable_methods.Length; index++)
        {
            var m = referObj->clazz->dvtable.vtable_methods[index];
            if (m is null)
                b.AppendLine($"\tmethod {index}: NULL_METHOD");
            else
                b.AppendLine($"\tmethod {index}: {m->Name}");
        }
        for (int index = 0; index < referObj->clazz->dvtable.vtable_fields.Length; index++)
        {
            var f = referObj->clazz->dvtable.vtable_fields[index];
            if (f is null)
                b.AppendLine($"\tfield {index}: NULL_FIELD");
            else
                b.AppendLine($"\tfield {index}: {f->Name}: {f->FieldType.Class->FullName->ToString()}");
        }
        
        File.WriteAllText(Path.Combine(Config.SnapshotPath.ToString(), $"{((nint)referObj):x8}-{referObj->clazz->Name}.dump"), b.ToString());
#endif
    }

    public static void Assert(bool when, bool conditional, WNE type, string msg, CallFrame* frame = null)
    {
        if (!when)
            return;
        if (conditional)
            return;
        if (frame is null)
            throw new IshtarExecutionException(type, msg);
        frame->vm->FastFail(type, $"static assert failed: {msg}", frame);
    }

    public static void GlobalPrintln(string empty) { }
}


public class IshtarExecutionException(WNE type, string msg) : Exception($"an error was caused, but the CallFrame is null, [{type}] {msg}");
