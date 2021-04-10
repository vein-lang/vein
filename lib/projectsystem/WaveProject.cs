namespace wave.project
{
    using System;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using runtime;
    using @internal;
    using MoreLinq;
    using Sprache;
    using static System.Environment;
    using static System.Environment.SpecialFolder;


    public class WaveProject
    {
        internal readonly XML.Project _project;

        internal WaveProject(FileInfo file, XML.Project project)
        {
            _project = project;
            Name = Path.GetFileNameWithoutExtension(file.FullName);
            WorkDir = file.DirectoryName;
        }

        public string Name { get; }
        public string WorkDir { get; }
        public IEnumerable<string> Sources => Directory
            .GetFiles(WorkDir, "*.wave", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".temp.wave"))
            .Where(x => !x.EndsWith(".generated.wave"));

        public IEnumerable<PackageReference> Packages =>
            _project.Packages.Ref.Select(x => PackageReference.Parser.Parse(x.Name));

        public WaveSDK SDK => new(this);
        
        
        public static WaveProject LoadFrom(FileInfo info)
        {
            var serializer = new XmlSerializer(typeof(XML.Project));
            using var stream = new StreamReader(info.FullName);
            using var reader = new XmlTextReader(stream)
            {
                Namespaces = false
            };
            var p = (XML.Project)serializer.Deserialize(reader);
            return new WaveProject(info, p);
        }
    }
    
    
    public class WaveSDK
    {
        private readonly WaveProject _project;
        internal WaveSDK(WaveProject project) => _project = project;

        public string Name => _project._project.Sdk;

        public DirectoryInfo RootPath => 
            new (Path.Combine(GetFolderPath(ProgramFilesX86), "WaveLang", "sdk", "0.1-preview"));

        public IEnumerable<FileInfo> Libs =>
            RootPath.EnumerateFiles("*.wll", SearchOption.AllDirectories);

        /*
        public WaveModule ResolveDep(string name, Version version, List<WaveModule> deps)
        {
            var file = FindModule(name, version);

            if (file is null)
            {
                Journal.logger.Error("[ResolveDep] Assembly {name}, {version} cannot resolve.", name, version);
                return null;
            }

            var asm = InsomniaAssembly.LoadFromFile(file);

            Journal.logger.Information("[ResolveDep] Success load assembly {name}, {version}.", name, version);

            var bytes = asm.Sections.First();

            var module = ModuleReader.Read(bytes.data, deps, (x,z) => ResolveDep(x,z,deps));

            Journal.logger.Information("[ResolveDep] Success load module {Name}, {Version}.", module.Name, module.Version);
            Journal.logger.Information("[ResolveDep] Module {Name}, {Version} has contained '{Count}' classes.", 
                module.Name, module.Version, module.class_table.Count);

            return module;
        }*/

        private FileInfo FindModule(string name)
        {
            // first, find in sdk folders
            var file = FindModuleInSDK(name);


            if (file is not null)
                return file;

            // second, find in rune cache
            //files = _project.Packages.Where(x => x.Name.Equals(name));

            return null;
        }

        private FileInfo FindModuleInSDK(string name)
        {
            try
            {
                var files = Libs.Where(x => 
                    x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    .Pipe(x => Journal.logger.Information("[FindModuleInSDK] analyze file '{x}'.", x))
                    .ToArray();

                return files.Single(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception e)
            {
                Journal.logger.Error(e, "[FindModuleInSDK] has catch exception.");
                return null;
            }
        }
    }
}