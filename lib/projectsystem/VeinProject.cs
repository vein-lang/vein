namespace vein.project
{
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Sprache;


    public class VeinProject
    {
        internal readonly XML.Project _project;

        internal VeinProject(FileInfo file, XML.Project project)
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
            .GetFiles(WorkDir, "*.vein", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".temp.vein"))
            .Where(x => !x.EndsWith(".generated.vein"));

        public IEnumerable<IProjectRef> Packages =>
            _project.Packages?.Ref?.Select(x => PackageReference.Convert(x.Name))
            ?? new List<PackageReference>();

        public VeinSDK SDK => VeinSDK.Resolve(_project.Sdk);


        public static VeinProject LoadFrom(FileInfo info)
        {
            var serializer = new XmlSerializer(typeof(XML.Project));
            using var stream = new StreamReader(info.FullName);
            using var reader = new XmlTextReader(stream)
            {
                Namespaces = false
            };
            var p = (XML.Project)serializer.Deserialize(reader);
            return new VeinProject(info, p);
        }
    }
}
