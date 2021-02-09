namespace wave.project
{
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
            using var reader = new StreamReader(info.FullName);
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
            new (Path.Combine(GetFolderPath(CommonProgramFilesX86), "WaveLang", "sdk", "0.1-preview"));

        public IEnumerable<FileInfo> Libs =>
            RootPath.EnumerateFiles("*.wll", SearchOption.AllDirectories);
    }
}