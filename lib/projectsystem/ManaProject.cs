namespace mana.project
{
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Sprache;
    using static System.Environment;
    using static System.Environment.SpecialFolder;


    public class ManaProject
    {
        internal readonly XML.Project _project;

        internal ManaProject(FileInfo file, XML.Project project)
        {
            _project = project;
            Name = Path.GetFileNameWithoutExtension(file.FullName);
            WorkDir = file.DirectoryName;
        }

        public string Name { get; }
        public string WorkDir { get; }

        public string Runtime
        {
            get => _project.Runtime;
            internal set => _project.Runtime = value;
        }

        public IEnumerable<string> Sources => Directory
            .GetFiles(WorkDir, "*.mana", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".temp.mana"))
            .Where(x => !x.EndsWith(".generated.mana"));

        public IEnumerable<PackageReference> Packages =>
            _project.Packages?.Ref?.Select(x => PackageReference.Parser.Parse(x.Name))
            ?? new List<PackageReference>();

        public ManaSDK SDK => ManaSDK.Resolve(_project.Sdk);
        
        
        public static ManaProject LoadFrom(FileInfo info)
        {
            var serializer = new XmlSerializer(typeof(XML.Project));
            using var stream = new StreamReader(info.FullName);
            using var reader = new XmlTextReader(stream)
            {
                Namespaces = false
            };
            var p = (XML.Project)serializer.Deserialize(reader);
            return new ManaProject(info, p);
        }
    }
}