namespace ishtar;

using System.Collections.Generic;
using System.Diagnostics;
using runtime;
using vein.runtime;

public unsafe class ForeignFunctionInterface
{
    public readonly VirtualMachine vm;
    public Dictionary<string, nint> method_table { get; } = new();

    public ForeignFunctionInterface(VirtualMachine vm)
    {
        this.vm = vm;
        INIT();
    }

    private void INIT()
    {
        B_App.InitTable(this);

        B_Out.InitTable(this);
        B_IEEEConsts.InitTable(this);
        //B_Sys.InitTable(this);
        B_String.InitTable(this);
        //B_StringBuilder.InitTable(this);
        B_GC.InitTable(this);
        //X_Utils.InitTable(this);
        //B_Type.InitTable(this);
        //B_Field.InitTable(this);
        //B_Function.InitTable(this);
        //B_NAPI.InitTable(this);
    }

    public RuntimeIshtarMethod* Add(string name, MethodFlags flags, params (string name, VeinTypeCode type)[] args)
    {
        var method = vm.CreateInternalMethod(
            VeinMethodBase.GetFullName(name, args.Select(x => (x.name, vm.Types->ByTypeCode(x.type)->Name))), flags, args);
        method->Assert(method);
        method_table.Add(method->Name, (nint)method);
        return method;
    }

    public RuntimeIshtarMethod* Add(string name, MethodFlags flags, RuntimeIshtarClass* returnType, params (string name, VeinTypeCode type)[] args)
    {
        var n = VeinMethodBase.GetFullName(name, args.Select(x => (x.name, vm.Types->ByTypeCode(x.type)->Name)));
        var method = vm.CreateInternalMethod(n, flags, returnType, args);
        method->Assert(method);
        method_table.Add(method->Name, (nint)method);
        return method;
    }

    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticValidate(void* p, CallFrame frame)
    {
        if (p != null) return;
        frame.vm.FastFail(WNE.STATE_CORRUPT, "Null pointer state.", frame);
    }
    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticValidateField(CallFrame current, IshtarObject** arg1, string name)
    {
        StaticValidate(*arg1, current);
        var @class = ( *arg1)->clazz;
        VirtualMachine.Assert(@class->FindField(name) != null, WNE.TYPE_LOAD,
            $"Field '{name}' not found in '{@class->Name}'.", current);
    }

    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticValidate(CallFrame frame, stackval* value, RuntimeIshtarClass* clazz)
    {
        frame.assert(clazz is not null);
        frame.assert(value->type != VeinTypeCode.TYPE_NONE);
        var obj = IshtarMarshal.Boxing(frame, value);
        frame.assert(obj->__gc_id != -1);
        var currentClass = obj->clazz;
        var targetClass = clazz;
        frame.assert(currentClass->ID == targetClass->ID, $"{currentClass->Name}.ID == {targetClass->Name}.ID");
    }

    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticValidate(CallFrame current, IshtarObject** arg1)
    {
        StaticValidate(*arg1, current);
        var @class = (*arg1)->clazz;
        VirtualMachine.Assert(@class->is_inited, WNE.TYPE_LOAD, $"Class '{@class->FullName->NameWithNS}' corrupted.", current);
        VirtualMachine.Assert(!@class->IsAbstract, WNE.TYPE_LOAD, $"Class '{@class->FullName->NameWithNS}' abstract.", current);
    }
    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticTypeOf(CallFrame current, IshtarObject** arg1, VeinTypeCode code)
    {
        StaticValidate(*arg1, current);
        var @class = (*arg1)->clazz;
        VirtualMachine.Assert(@class->TypeCode == code, WNE.TYPE_MISMATCH, $"@class.{@class->TypeCode} == {code}", current);
    }

    public RuntimeIshtarMethod* GetMethod(string FullName)
        => (RuntimeIshtarMethod*)method_table.GetValueOrDefault(FullName);


    public static void LinkExternalNativeLibrary(string importModule, string fnName,
        RuntimeIshtarMethod* importCaller)
    {
        var jitter = importCaller->Owner->Owner->vm.Jitter;

        jitter.Compile21FFI(importCaller, importModule, fnName);
    }


    public void DisplayDefinedMapping()
    {
        if (!vm.Config.HasFlag(SysFlag.DISPLAY_FFI_MAPPING)) return;

        foreach (var (key, value) in method_table)
            vm.trace.println($"ffi map '{key}' -> 'sys::FFI/{((RuntimeIshtarClass*)value)->Name}'");
    }
}

public record NativeImportCache(string entry, nint handle)
{
    public Dictionary<string, nint> ImportedSymbols = new();
}

public unsafe class NativeImportEntity(string entry, string fn, nint importer)
{
    public string Entry { get; } = entry;
    public string Fn { get; } = fn;
    public RuntimeIshtarMethod* Importer => (RuntimeIshtarMethod*)importer;
    public RuntimeIshtarModule* Module => Importer->Owner->Owner;

    public nint Handle;

    public bool IsBinded() => Handle != IntPtr.Zero;
}
