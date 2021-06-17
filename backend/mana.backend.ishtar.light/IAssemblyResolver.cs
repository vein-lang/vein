namespace mana.backend.ishtar.light
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using fs;
    using global::ishtar;
    using runtime;
    using mana.ishtar.emit;

    public class AssemblyResolver : IAssemblyResolver
    {
        private readonly List<DirectoryInfo> search_paths = new ();
        private AssemblyBundle assemblyBundle;
        public AssemblyResolver()
        {

        }

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

            var module = RuntimeModuleReader.Read(asm.Sections.First().data, deps, (s, v) =>
                ResolveDep(s, v, deps));
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
