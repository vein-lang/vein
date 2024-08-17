namespace ishtar;

using System.Linq;
using System.Threading;
using runtime;
using vein;
using vein.runtime;
using collections;
using runtime.gc;
using runtime.vin;

public sealed unsafe class AppVault : AppVaultSync, IDisposable
{
    public DirectoryInfo WorkDirectory { get; set; } = new("./");

    public VirtualMachine* vm { get; }
    public string Name { get; }
    private AssemblyResolver Resolver { get; set; }
    public TokenInterlocker TokenGranted { get; }
    public int ThreadID { get; }

    public ForeignFunctionInterface FFI;
    public NativeStorage NativeStorage;

    internal readonly NativeList<RuntimeIshtarModule>* Modules;

    private AppVault(VirtualMachine* vm, string name)
    {
        using var tag = Profiler.Begin("vm:vault:create");

        this.vm = vm;
        Name = name;
        
        TokenGranted = new TokenInterlocker(this, this);
        ThreadID = Thread.CurrentThread.ManagedThreadId;
        Modules = IshtarGC.AllocateList<RuntimeIshtarModule>(vm);
        virtual_machines.Add(vm->Name->ID, this);
    }

    public void PostInit()
    {
        using var tag = Profiler.Begin("vm:init:post");

        FFI = new ForeignFunctionInterface(vm);
        NativeStorage = new NativeStorage(vm);
    }

    public static AppVault Create(VirtualMachine* vm, string name) => new(vm, name);

    private static readonly Dictionary<ulong, AppVault> virtual_machines = new();

    public static AppVault GetVault(VirtualMachine* vm) => virtual_machines[vm->Name->ID];

    // TODO optimization for module search (TypeName already contain module name)
    public RuntimeIshtarClass* GlobalFindType(RuntimeQualityTypeName* typeName, bool findExternally = false, bool dropUnresolved = false)
    {
        using var enumerator = Modules->GetEnumerator();

        while (enumerator.MoveNext())
        {
            var module = (RuntimeIshtarModule*)enumerator.Current;
            var r = module->FindType(typeName, findExternally, dropUnresolved);
            if (r->IsUnresolved)
                continue;
            return r;
        }

        return null;
    }


    public RuntimeQualityTypeName* GlobalFindTypeName(string name)
    {
        using var enumerator = Modules->GetEnumerator();

        RuntimeQualityTypeName* target = null;

        while (enumerator.MoveNext())
        {
            var module = (RuntimeIshtarModule*)enumerator.Current;

            module->types_table->ForEach((x, y) =>
            {
                if (target is not null)
                    return;

                if (y->ToString().Equals(name)) target = y;
            });

            if (target is not null)
                return target;
        }

        return null;
    }

    public RuntimeIshtarClass*[] GlobalFindType(string name)
        => throw new NotImplementedException();

    public RuntimeIshtarClass* GlobalFindType(RuntimeToken token)
    {
        using var enumerator = Modules->GetEnumerator();

        while (enumerator.MoveNext())
        {
            var module = (RuntimeIshtarModule*)enumerator.Current;
            var r = module->FindType(token);
            if (r->IsUnresolved)
                continue;
            return r;
        }

        return null;
    }

    public AssemblyResolver GetResolver()
    {
        if (Resolver is not null)
            return Resolver;
        Resolver = new AssemblyResolver(this);
        Resolver.Resolved += ResolverOnResolved;
        ReadDependencyMetadata();
        return Resolver;
    }

    private void ReadDependencyMetadata()
    {
        if (!WorkDirectory.File("dependency.links").Exists)
            return;

        foreach (var line in File.ReadAllLines(WorkDirectory.File("dependency.links").FullName)
                     .Select(x => new DirectoryInfo(x))
                     .Where(x => x.Exists))
            Resolver.AddSearchPath(line);
    }

    private void ResolverOnResolved(in RuntimeIshtarModule* module)
    {
        module->ID = TokenGranted.GrantModuleID();
        Modules->Add(module);
    }

    object AppVaultSync.TokenInterlockerGuard { get; } = new();
    internal uint LastModuleID;
    internal uint LastClassID;

    public void Dispose()
    {
        VirtualMachine.GlobalPrintln($"Disposed vault '{Name}'");

        Modules->ForEach(x => x->Dispose());
        Modules->Clear();
        IshtarGC.FreeList(Modules);
    }

    public RuntimeIshtarModule* DefineModule(string @internal)
    {
        using var tag = Profiler.Begin("vm:vault:module:alloc");

        var module = IshtarGC.AllocateImmortalRoot<RuntimeIshtarModule>();
        *module = new RuntimeIshtarModule(this, @internal, module, new IshtarVersion());

        return module;
    }
}


public interface AppVaultSync
{
    object TokenInterlockerGuard { get; }
}
