namespace ishtar
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using mana.backend.ishtar.light;
    using mana.fs;
    using mana.runtime;

    public class AppVault : AppVaultSync
    {
        public static AppVault CurrentVault { get; internal set; } = new("app");


        public string Name { get; }
        protected virtual AssemblyResolver Resolver { get; set; }
        public TokenInterlocker TokenGranted { get; }
        public int ThreadID { get; }

        internal readonly List<RuntimeIshtarModule> Modules = new();

        public AppVault(string name)
        {
            Name = name;
            TokenGranted = new TokenInterlocker(this, this);
            ThreadID = Thread.CurrentThread.ManagedThreadId;
        }


        public AssemblyResolver GetResolver()
        {
            if (Resolver is not null)
                return Resolver;
            Resolver = new AssemblyResolver(this);
            Resolver.Resolved += ResolverOnResolved;

            return Resolver;
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
