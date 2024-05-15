namespace ishtar
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using runtime;
    using vein;
    using vein.reflection;
    using vein.runtime;
    using collections;
    using vm;

    public unsafe class AppVault : AppVaultSync, IDisposable
    {
        public DirectoryInfo WorkDirectory { get; set; } = new("./");

        public VirtualMachine vm { get; }
        public string Name { get; }
        protected virtual AssemblyResolver Resolver { get; set; }
        public TokenInterlocker TokenGranted { get; }
        public int ThreadID { get; }

        internal readonly DirectNativeList<RuntimeIshtarModule>* Modules = DirectNativeList<RuntimeIshtarModule>.New(8);

        public AppVault(VirtualMachine vm, string name)
        {
            this.vm = vm;
            Name = name;
            TokenGranted = new TokenInterlocker(this, this);
            ThreadID = Thread.CurrentThread.ManagedThreadId;
        }

        public RuntimeIshtarClass* GlobalFindType(RuntimeQualityTypeName* typeName)
        {
            using var enumerator = Modules->GetEnumerator();

            while (enumerator.MoveNext())
            {
                var module = (RuntimeIshtarModule*)enumerator.Current;
                var r = module->FindType(typeName, false, false);
                if (r->IsUnresolved)
                    continue;
                return r;
            }

            return null;
        }

        public RuntimeIshtarClass* GlobalFindType(QualityTypeName typeName)
        {
            using var enumerator = Modules->GetEnumerator();

            while (enumerator.MoveNext())
            {
                var module = (RuntimeIshtarModule*)enumerator.Current;
                var r = module->FindType(typeName, false, false);
                if (r->IsUnresolved)
                    continue;
                return r;
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
        internal ushort LastModuleID;
        internal ushort LastClassID;

        public void Dispose() => Modules->Clear();

        public RuntimeIshtarModule* DefineModule(string @internal)
        {
            var module = IshtarGC.AllocateImmortalRoot<RuntimeIshtarModule>();

            *module = new RuntimeIshtarModule(vm.Vault, @internal, module, new IshtarVersion());

            return module;
        }
    }


    public interface AppVaultSync
    {
        object TokenInterlockerGuard { get; }
    }

}
