namespace vein
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using compilation;
    using fs;
    using ishtar.emit;
    using runtime;

    public class AssemblyResolver : IAssemblyResolver
    {
        private readonly Compiler _c;
        private readonly List<DirectoryInfo> search_paths = new ();
        public AssemblyResolver(Compiler c) => _c = c;

        public AssemblyResolver AddSearchPath(DirectoryInfo dir)
        {
            _c.PrintInfo($"Assembly search path '{dir}' is added.");
            search_paths.Add(dir);
            return this;
        }

        public VeinModule ResolveDep(string name, Version version, List<VeinModule> deps)
        {
            var file = FindInPaths(name);
            if (file is null)
            {
                _c.PrintWarning($"Dependency '{name}' is not resolved.");
                throw new FileNotFoundException(name);
            }

            var asm = IshtarAssembly.LoadFromFile(file);

            var mod = ModuleReader.Read(asm.Sections.First().data, deps,
                (s, v) => ResolveDep(s, v, deps));
            _c.PrintInfo($"Dependency '{name}@{mod.Version}' is resolved.");
            return mod;
        }

        private FileInfo FindInPaths(string name)
        {
            try
            {
                var files = search_paths
                    .SelectMany(x => x.EnumerateFiles("*.wll"))
                    .Where(x =>
                        x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

                return files.Single(x => x.Name.Equals($"{name}.wll", StringComparison.InvariantCultureIgnoreCase));
            }
            catch
            {
                return null;
            }
        }
    }
}
