namespace ishtar
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using vein;
    using vein.fs;
    using vein.reflection;
    using vein.runtime;

    public class AppVault : AppVaultSync
    {
        public DirectoryInfo WorkDirecotry { get; set; } = new("./");

        public VirtualMachine vm { get; }
        public string Name { get; }
        protected virtual AssemblyResolver Resolver { get; set; }
        public TokenInterlocker TokenGranted { get; }
        public int ThreadID { get; }

        internal readonly List<RuntimeIshtarModule> Modules = new();

        public AppVault(VirtualMachine vm, string name)
        {
            this.vm = vm;
            Name = name;
            TokenGranted = new TokenInterlocker(this, this);
            ThreadID = Thread.CurrentThread.ManagedThreadId;
        }

        public RuntimeIshtarClass GlobalFindType(QualityTypeName typeName)
        {
            foreach (var module in Modules)
            {
                var r = module.FindType(typeName, false, false);
                if (r is UnresolvedVeinClass)
                    continue;
                return r as RuntimeIshtarClass;
            }
            return null;
        }

        public RuntimeIshtarClass[] GlobalFindType(string name)
        {
            var list = new List<RuntimeIshtarClass>();
            foreach (var module in Modules)
            {
                var r = module.class_table.Where(x => x.Name.Equals(name)).OfType<RuntimeIshtarClass>().ToArray();
                if (r.Length == 0)
                    continue;
                list.AddRange(r);
            }
            return list.ToArray();
        }

        public RuntimeIshtarClass GlobalFindType(RuntimeToken token)
        {
            foreach (var module in Modules)
            {
                var r = module.FindType(token);
                if (r is null)
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
            if (!WorkDirecotry.File("dependency.links").Exists)
                return;

            foreach (var line in File.ReadAllLines(WorkDirecotry.File("dependency.links").FullName)
                         .Select(x => new DirectoryInfo(x))
                         .Where(x => x.Exists))
                Resolver.AddSearchPath(line);
        }

        private void ResolverOnResolved(RuntimeIshtarModule module)
        {
            module.ID = TokenGranted.GrantModuleID();
            Modules.Add(module);
        }

        object AppVaultSync.TokenInterlockerGuard { get; } = new();
        internal ushort LastModuleID;
        internal ushort LastClassID;
    }


    public interface AppVaultSync
    {
        object TokenInterlockerGuard { get; }
    }

}
