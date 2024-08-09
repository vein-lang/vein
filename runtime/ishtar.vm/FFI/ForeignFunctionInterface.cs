namespace ishtar;

using System.Collections.Generic;
using System.Linq;
using collections;
using ishtar.vm.__builtin.networks;
using networks;
using runtime;
using runtime.gc;
using vein.runtime;

public unsafe class ForeignFunctionInterface
{
    public readonly VirtualMachine vm;
    public NativeDictionary<ulong, RuntimeIshtarMethod>* methods { get; } 
    public Dictionary<string, ulong> method_table { get; } = new();
    public AtomicNativeDictionary<ulong, PInvokeInfo>* deferMethods { get; }

    private ulong _index;

    public ForeignFunctionInterface(VirtualMachine vm)
    {
        this.vm = vm;
        methods = IshtarGC.AllocateDictionary<ulong, RuntimeIshtarMethod>(vm.@ref);
        deferMethods = IshtarGC.AllocateAtomicDictionary<ulong, PInvokeInfo>(vm.@ref);
        INIT();
    }
    public PInvokeInfo AsNative(delegate*<CallFrame*, IshtarObject**, IshtarObject*> p)
    {
        var result = new PInvokeInfo
        {
            isInternal = true,
            compiled_func_ref = (nint)p,
        };
        return result;
    }

    private void INIT()
    {
        B_App.InitTable(this);

        B_Out.InitTable(this);
        B_IEEEConsts.InitTable(this);
        //B_Sys.InitTable(this);
        B_File.InitTable(this);
        B_String.InitTable(this);
        //B_StringBuilder.InitTable(this);
        B_GC.InitTable(this);
        //X_Utils.InitTable(this);
        //B_Type.InitTable(this);
        //B_Field.InitTable(this);
        //B_Function.InitTable(this);
        //B_NAPI.InitTable(this);
        B_Regex.InitTable(this);
        B_Socket.InitTable(this);
        B_Threading.InitTable(this);
        B_Array.InitTable(this);
        B_TempEq.InitTable(this);
        B_Sync.InitTable(this);
        B_Dns.InitTable(this);
        B_Async.InitTable(this);
    }

    public RuntimeIshtarMethod* Add(string name, MethodFlags flags, VeinTypeCode returnType, params (string name, VeinTypeCode type)[] args)
        => Add(name, flags, vm.Types->ByTypeCode(returnType), args);

    public RuntimeIshtarMethod* Add(string name, MethodFlags flags, RuntimeIshtarClass* returnType, params (string name, VeinTypeCode type)[] args)
    {
        _index++;
        var arr = new RuntimeIshtarClass*[args.Length];

        for (int index = 0; index < args.Length; index++)
        {
            var (_, type) = args[index];
            arr[index] = vm.Types->ByTypeCode(type);
        }

        var method = vm.CreateInternalMethod(
            RuntimeIshtarMethod.GetFullName(name, returnType, arr), flags, args);
        method->Assert(method);
        method_table.Add(method->Name, _index);
        methods->Add(_index, method);
        return method;
    }

    public void Add(string fullName, PInvokeInfo nativeInfo)
    {
        _index++;
        method_table.Add(fullName, _index);
        deferMethods->Add(_index, nativeInfo);
    }

    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticValidate(void* p, CallFrame* frame)
    {
        if (p != null) return;
        frame->vm.FastFail(WNE.STATE_CORRUPT, "Null pointer state.", frame);
    }

    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticValidateField(CallFrame* current, IshtarObject** arg1, string name)
    {
        StaticValidate(*arg1, current);
        var @class = ( *arg1)->clazz;
        VirtualMachine.Assert(@class->FindField(name) != null, WNE.TYPE_LOAD,
            $"Field '{name}' not found in '{@class->Name}'.", current);
    }

    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticValidate(CallFrame* frame, stackval* value, RuntimeIshtarClass* clazz)
    {
        frame->assert(clazz is not null);
        frame->assert(value->type != VeinTypeCode.TYPE_NONE);
        var obj = IshtarMarshal.Boxing(frame, value);
        frame->assert(obj->__gc_id != -1);
        var currentClass = obj->clazz;
        var targetClass = clazz;
        frame->assert(currentClass->ID == targetClass->ID, $"{currentClass->Name}.ID == {targetClass->Name}.ID");
    }

    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticValidate(CallFrame* current, IshtarObject** arg1)
    {
        StaticValidate(*arg1, current);
        var @class = (*arg1)->clazz;
        VirtualMachine.Assert(@class->is_inited, WNE.TYPE_LOAD, $"Class '{@class->FullName->NameWithNS}' corrupted.", current);
        VirtualMachine.Assert(!@class->IsAbstract, WNE.TYPE_LOAD, $"Class '{@class->FullName->NameWithNS}' abstract.", current);
    }
    [Conditional("STATIC_VALIDATE_IL")]
    public static void StaticTypeOf(CallFrame* current, IshtarObject** arg1, VeinTypeCode code)
    {
        StaticValidate(*arg1, current);
        var @class = (*arg1)->clazz;
        VirtualMachine.Assert(@class->TypeCode == code, WNE.TYPE_MISMATCH, $"@class.{@class->TypeCode} == {code}", current);
    }

    public void GetMethod(string FullName, out PInvokeInfo nativeHeader)
    {
        if (method_table.TryGetValue(FullName, out var idx))
        {
            if (methods->ContainsKey(idx))
            {
                nativeHeader = methods->Get(idx)->PIInfo;
                return;
            }
            else if (deferMethods->ContainsKey(idx))
            {
                nativeHeader = deferMethods->Get(idx);
                return;
            }
        }
        vm.FastFail(WNE.MISSING_METHOD, $"method '{FullName}' is not found", vm.Frames->NativeLoader);
        throw new EntryPointNotFoundException(FullName);
    }

    private RuntimeIshtarMethod* GetMethod(ulong idx) => methods->Get(idx);


    public static void LinkExternalNativeLibrary(string importModule, string fnName,
        RuntimeIshtarMethod* importCaller)
    {
        var jitter = importCaller->Owner->Owner->vm.Jitter;

        jitter.CompileFFI(importCaller, importModule, fnName);
    }


    public void DisplayDefinedMapping()
    {
        foreach (var (key, value) in method_table)
            vm.trace.println($"ffi map '{key}' -> 'sys::FFI/{(GetMethod(value))->Name}'");
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
