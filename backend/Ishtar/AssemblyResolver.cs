namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using insomnia;
    using MoreLinq;
    using Serilog;
    using wave;
    using wave.fs;
    using wave.ishtar.emit;
    using wave.runtime;

    public class AssemblyResolver : IAssemblyResolver
    {
        private ILogger logger => Journal.Get(nameof(AssemblyResolver));

        public DirectoryInfo RootPath { get; private set; }
        public IEnumerable<FileInfo> Libs =>
            RootPath.EnumerateFiles("*.wll", SearchOption.AllDirectories);

        public AssemblyResolver(DirectoryInfo root) => this.RootPath = root;

        public WaveModule ResolveDep(string name, Version version, List<WaveModule> deps)
        {
            var file = FindModule(name);

            if (file is null)
            {
                logger.Error("[ResolveDep] Assembly {name}, {version} cannot resolve.", name, version);
                return null;
            }

            var asm = IshtarAssembly.LoadFromFile(file);

            logger.Information("[ResolveDep] Success load assembly {name}, {version}.", name, version);

            var bytes = asm.Sections.First();

            var module = ModuleReader.Read(bytes.data, deps, (x,z) => ResolveDep(x,z,deps));

            logger.Information("[ResolveDep] Success load module {Name}, {Version}.", module.Name, module.Version);
            logger.Information("[ResolveDep] Module {Name}, {Version} has contained '{Count}' classes.", 
                module.Name, module.Version, module.class_table.Count);

            return module;
        }

        private FileInfo FindModule(string name)
        {
            // first, find in sdk folders
            var file = FindModuleInSDK(name);


            if (file is not null)
                return file;

            // second, find in rune cache
            //files = _project.Packages.Where(x => x.Name.Equals(name));
            throw new NotImplementedException();
            return null;
        }

        private FileInfo FindModuleInSDK(string name)
        {
            try
            {
                var files = Libs.Where(x => 
                    x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .Pipe(x => logger.Information("[FindModuleInSDK] analyze file '{x}'.", x))
                    .ToArray();

                return files.Single(x => x.Name.Equals($"{name}.wll", StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception e)
            {
                logger.Error(e, "[FindModuleInSDK] has catch exception.");
                return null;
            }
        }
    }
}