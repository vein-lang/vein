namespace wave
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using fs;
    using ishtar.emit;
    using runtime;

    public class AssemblyResolver : IAssemblyResolver
    {
        private readonly List<DirectoryInfo> search_paths = new ();
        public AssemblyResolver()
        {
            
        }

        public AssemblyResolver AddSearchPath(DirectoryInfo dir)
        {
            search_paths.Add(dir);
            return this;
        }

        public WaveModule ResolveDep(string name, Version version, List<WaveModule> deps)
        {
            var file = FindInPaths(name);
            if (file is null)
            {
                throw new FileNotFoundException(name);
            }

            var asm = IshtarAssembly.LoadFromFile(file);
            
            var mod = ModuleReader.Read(asm.Sections.First().data, deps, 
                (s, v) => ResolveDep(s, v, deps));

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
            catch (Exception e)
            {
                throw;
            }
        }
    }
}