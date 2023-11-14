namespace ishtar
{
using ishtar.runtime.vin;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using vein.runtime;
using vm;

public unsafe class ForeignFunctionInterface
    {
        public readonly VirtualMachine vm;
        public Dictionary<string, RuntimeIshtarMethod> method_table { get; } = new();

        public ForeignFunctionInterface(VirtualMachine vm)
        {
            this.vm = vm;
            INIT();
        }

        private void INIT()
        {
            B_Out.InitTable(this);
            B_App.InitTable(this);
            B_IEEEConsts.InitTable(this);
            B_Sys.InitTable(this);
            B_String.InitTable(this);
            B_StringBuilder.InitTable(this);
            B_GC.InitTable(this);
            X_Utils.InitTable(this);
            B_Type.InitTable(this);
            B_Field.InitTable(this);
            B_Function.InitTable(this);
            B_NAPI.InitTable(this);
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
            var @class = (*arg1)->decodeClass();
            VirtualMachine.Assert(@class.FindField(name) != null, WNE.TYPE_LOAD,
                $"Field '{name}' not found in '{@class.Name}'.", current);
        }

        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidate(CallFrame frame, stackval* value, VeinClass clazz)
        {
            frame.assert(clazz is RuntimeIshtarClass);
            frame.assert(value->type != VeinTypeCode.TYPE_NONE);
            var obj = IshtarMarshal.Boxing(frame, value);
            frame.assert(obj->__gc_id != -1);
            var currentClass = obj->decodeClass();
            var targetClass = clazz as RuntimeIshtarClass;
            frame.assert(currentClass.ID == targetClass.ID, $"{currentClass.Name}.ID == {targetClass.Name}.ID");
        }

        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticValidate(CallFrame current, IshtarObject** arg1)
        {
            StaticValidate(*arg1, current);
            var @class = (*arg1)->decodeClass();
            VirtualMachine.Assert(@class.is_inited, WNE.TYPE_LOAD, $"Class '{@class.FullName}' corrupted.", current);
            VirtualMachine.Assert(!@class.IsAbstract, WNE.TYPE_LOAD, $"Class '{@class.FullName}' abstract.", current);
        }
        [Conditional("STATIC_VALIDATE_IL")]
        public static void StaticTypeOf(CallFrame current, IshtarObject** arg1, VeinTypeCode code)
        {
            StaticValidate(*arg1, current);
            var @class = (*arg1)->decodeClass();
            VirtualMachine.Assert(@class.TypeCode == code, WNE.TYPE_MISMATCH, $"@class.{@class.TypeCode} == {code}", current);
        }

        public RuntimeIshtarMethod GetMethod(string FullName)
            => method_table.GetValueOrDefault(FullName);


        public static void LinkExternalNativeLibrary(string inportTarget, string fnName,
            RuntimeIshtarMethod importCaller)
        {
            importCaller.PIInfo = PInvokeInfo.New(null) with { iflags = (ushort)PInvokeInfo.Flags.EXTERNAL };

            var entity = new NativeImportEntity(inportTarget, fnName, importCaller);

            if (importCaller.Owner is not RuntimeIshtarClass clazz)
                throw null;
            clazz.nativeImports.Add(entity);
        }



        
        // ==================
        // TODO, move to outside
        // ==================

        private readonly Dictionary<string, NativeImportCache> _cache = new ();

        public void LoadNativeLibrary(NativeImportEntity entity, CallFrame frame)
        {
            if (_cache.ContainsKey(entity.entry))
                return;

            var result = NativeStorage.TryLoad(new FileInfo(entity.entry), out var handle);

            if (!result)
            {
                frame.vm.FastFail(WNE.NATIVE_LIBRARY_COULD_NOT_LOAD, $"{entity.entry}", frame);
                return;
            }

            _cache[entity.entry] = new NativeImportCache(entity.entry, handle);
        }

        public void LoadNativeSymbol(NativeImportEntity entity, CallFrame frame)
        {
            var cached = _cache[entity.entry];

            if (cached.ImportedSymbols.ContainsKey(entity.fn))
                return;

            try
            {
                var symbol = NativeStorage.GetSymbol(cached.handle, entity.fn);

                cached.ImportedSymbols.Add(entity.fn, symbol);
                entity.Handle = symbol;
            }
            catch
            {
                frame.vm.FastFail(WNE.NATIVE_LIBRARY_SYMBOL_COULD_NOT_FOUND, $"{entity.entry}::{entity.fn}", frame);
            }
        }



        public void DisplayDefinedMapping()
        {
            if (!vm.Config.HasFlag(SysFlag.DISPLAY_FFI_MAPPING)) return;

            foreach (var (key, value) in method_table)
                vm.trace.println($"ffi map '{key}' -> 'sys::FFI/{value.Name}'");
        }
    }

    public record NativeImportCache(string entry, nint handle)
    {
        public Dictionary<string, nint> ImportedSymbols = new Dictionary<string, nint>();
    }

    public record NativeImportEntity(string entry, string fn, RuntimeIshtarMethod importer)
    {
        public VeinModule Module => importer.Owner.Owner;

        public nint Handle;

        public bool IsBinded() => Handle != IntPtr.Zero;
    }
}
