namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using fs;
    using global::ishtar;
    using vein.runtime;
    using ishtar.emit;

    public delegate void ModuleResolvedEvent(RuntimeIshtarModule module);

    public class AssemblyResolver : IAssemblyResolver
    {
        public AppVault Vault { get; }
        private readonly List<DirectoryInfo> search_paths = new ();
        private AssemblyBundle assemblyBundle;
        public event ModuleResolvedEvent Resolved;
        public AssemblyResolver(AppVault vault) => Vault = vault;

        public AssemblyResolver AddSearchPath(DirectoryInfo dir)
        {
            search_paths.Add(dir);
            return this;
        }

        public AssemblyResolver AddInMemory(AssemblyBundle bundle)
        {
            assemblyBundle = bundle;
            return this;
        }

        public ManaModule ResolveDep(string name, Version version, List<ManaModule> deps)
        {
            var asm = Find(name, version, deps);

            var module = RuntimeIshtarModule.Read(Vault, asm.Sections.First().data, deps, (s, v) =>
                ResolveDep(s, v, deps));
            Resolved?.Invoke(module);
            return module;
        }

        public RuntimeIshtarModule Resolve(IshtarAssembly assembly)
        {
            var (_, code) = assembly.Sections.First();
            var module = RuntimeIshtarModule.Read(Vault, code, new List<ManaModule>(), (s, version) =>
                this.ResolveDep(s, version, new List<ManaModule>()));

            Resolved?.Invoke(module);

            return module;
        }

        public IshtarAssembly Find(string name, Version version, List<ManaModule> deps)
        {
            var file = FindInPaths(name);

            if (file is not null)
                return IshtarAssembly.LoadFromFile(file);
            var asm = FindInBundle(name);

            if (asm is not null)
                return asm;

            throw new FileNotFoundException(name);
        }

        private IshtarAssembly FindInBundle(string name)
        {
            if (assemblyBundle is null)
                return null;

            return assemblyBundle.Assemblies.Single(x =>
                x.Name.Equals($"{name}.wll", StringComparison.InvariantCultureIgnoreCase));
        }

        private FileInfo FindInPaths(string name)
        {
            try
            {
                var files = search_paths.Where(x => x.Exists)
                    .SelectMany(x => x.EnumerateFiles("*.wll"))
                    .Where(x =>
                        x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

                return files.Single(x => x.Name.Equals($"{name}.wll", StringComparison.InvariantCultureIgnoreCase));
            }
            catch (InvalidOperationException)
            {
                VM.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, $"Assembly '{name}' cannot be loaded.");
                VM.ValidateLastError();
                return null;
            }
        }
    }
}
