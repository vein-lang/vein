namespace vein.project
{
    using System;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using extensions;
    using Sprache;


    public class VeinProject
    {
        internal readonly XML.Project _project;

        internal VeinProject(FileInfo file, XML.Project project)
        {
            _project = project;
            Name = Path.GetFileNameWithoutExtension(file.FullName);
            WorkDir = file.Directory;
            Dependencies = new(this);
        }

        public string Name { get; }
        public DirectoryInfo WorkDir { get; }

        public string Runtime
        {
            get => _project.Runtime;
            internal set => _project.Runtime = value;
        }

        public IReadOnlyCollection<FileInfo> Sources => WorkDir
            .EnumerateFiles("*.vein", SearchOption.AllDirectories)
            .Where(x => !x.Name.EndsWith(".temp.vein"))
            .Where(x => !x.Name.EndsWith(".generated.vein"))
            .ToList()
            .AsReadOnly();

        private IEnumerable<IProjectRef> refs =>
            _project.Packages?.Ref?.Select(x => PackageReference.Convert(x.Name))
            ?? new List<PackageReference>();

        public Dep Dependencies { get; }

        public class Dep
        {
            private readonly VeinProject _project;

            internal Dep(VeinProject p) => _project = p;

            public IReadOnlyCollection<ProjectReference> Projects => _project
                .refs
                .OfExactType<ProjectReference>()
                .ToList()
                .AsReadOnly();

            public IReadOnlyCollection<PackageReference> Packages => _project
                .refs
                .OfExactType<PackageReference>()
                .ToList()
                .AsReadOnly();
        }


        public VeinSDK SDK => VeinSDK.Resolve(_project.Sdk);


        public static VeinProject LoadFrom(FileInfo info)
        {
            try
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
            catch
            {
                return null;
            }
        }
    }
}
