namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using extensions;
    using fs;
    using ishtar;
    using ishtar.collections;
    using ishtar.runtime;
    using ishtar.vm;

    public unsafe delegate void ModuleResolvedEvent(in RuntimeIshtarModule* module);

    public unsafe delegate RuntimeIshtarModule* ModuleResolverCallback(string name, IshtarVersion version);

    public unsafe class AssemblyResolver : ModuleResolverBase
    {
        public AppVault Vault { get; }
        private AssemblyBundle assemblyBundle;
        public event ModuleResolvedEvent Resolved;
        public AssemblyResolver(AppVault vault) => Vault = vault;

        public AssemblyResolver AddInMemory(AssemblyBundle bundle)
        {
            assemblyBundle = bundle;
            return this;
        }

        public override VeinModule ResolveDep(string name, Version version, IReadOnlyList<VeinModule> deps)
        {
            throw new NotImplementedException();
        }

        public RuntimeIshtarModule* ResolveDep(string name, IshtarVersion version, DirectNativeList<RuntimeIshtarModule>* deps)
        {
            var asm = Find(name, version);

            var module = RuntimeIshtarModule.Read(Vault, asm.Sections.First().data, deps, (s, v) =>
                ResolveDep(s, v, deps));
            Resolved?.Invoke(module);
            return module;
        }

        public RuntimeIshtarModule* Resolve(IshtarAssembly assembly)
        {
            var (_, code) = assembly.Sections.First();
            var module = RuntimeIshtarModule.Read(Vault, code, DirectNativeList<RuntimeIshtarModule>.New(1), (s, version) =>
                this.ResolveDep(s, version, DirectNativeList<RuntimeIshtarModule>.New(1)));

            Resolved?.Invoke(module);

            return module;
        }
        

        public IshtarAssembly Find(string name, IshtarVersion version)
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
                x.Name.Equals($"{name}.{MODULE_FILE_EXTENSION}", StringComparison.InvariantCultureIgnoreCase));
        }

        private FileInfo FindInPaths(string name)
        {
            var files = search_paths.Where(x => x.Exists)
                .SelectMany(x => x.EnumerateFiles($"*.{MODULE_FILE_EXTENSION}"))
                .Where(x =>
                    x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();
            try
            {
                return files.Single(x => x.Name.Equals($"{name}.{MODULE_FILE_EXTENSION}", StringComparison.InvariantCultureIgnoreCase));
            }
            catch (InvalidOperationException)
            {
                var text = $"Assembly '{name}' cannot be loaded.\n" +
                           $"\t  {search_paths.Select(x => $"Path '{x}', Exist: {x.Exists}").Join("\n\t  ")};";
                if (files.Length != 0)
                    text += $"\n\tfiles checked: {files.Select(x => $"{x}").Join("\n\t\t")}";
                Vault.vm.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, text, sys_frame);
                return null;
            }
        }

        protected override void debug(string s) { }


        public CallFrame sys_frame => Vault.vm.Frames.ModuleLoaderFrame;
    }
}
