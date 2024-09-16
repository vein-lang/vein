namespace ishtar.runtime.platform;

using gc;
using io;
using ishtar.collections;

public unsafe struct PlatformApi(VirtualMachine* vm)
{
    private NativeDictionary<nint, InternedString>* loadedModules { get; set; }
    private NativeDictionary<nint, NativeDictionary<nint, InternedString>>* loadedSymbols { get; set; }

    public static PlatformApi* create(VirtualMachine* vm)
    {
        var p = IshtarGC.AllocateImmortal<PlatformApi>(null);
        *p = new PlatformApi(vm);
        p->loadedModules = IshtarGC.AllocateDictionary<nint, InternedString>(null);
        p->loadedSymbols = IshtarGC.AllocateDictionary<nint, NativeDictionary<nint, InternedString>>(null);
        return p;
    }


    public ModuleHandle* GetOrCreate(string name, bool throwOnNotFound = false)
    {
        if (NativeLibrary.TryLoad(name, out var result))
        {
            loadedModules->Add(result, StringStorage.Intern(name, null));
            return (ModuleHandle*)result;
        }
        if (throwOnNotFound)
            vm->FastFail(WNE.NATIVE_LIBRARY_COULD_NOT_LOAD, "", vm->Frames->NativeLoader);
        return null;
    }

    public SymbolHandle* GetFrom(ModuleHandle* module, string symbolName, bool throwOnNotFound = false)
    {
        if (!loadedModules->TryGetValue((nint)module, out _))
        {

        }


        if (NativeLibrary.TryGetExport((nint)module, symbolName, out var result))
        {
            

            return (SymbolHandle*)result;
        }

        if (throwOnNotFound)
            vm->FastFail(WNE.NATIVE_LIBRARY_COULD_NOT_LOAD, $"symbol '{symbolName}' not found", vm->Frames->NativeLoader);
        return null;
    }
}


public struct ModuleHandle;
public struct SymbolHandle;
