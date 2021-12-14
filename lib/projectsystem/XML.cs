namespace vein.project;


using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using YamlDotNet.Serialization;

public static class YAML
{
    public class Project
    {
        [YamlMember(Alias = "target", Order = 0)]
        public string Target { get; set; }
        [YamlMember(Alias = "runtime", Order = 1)]
        public string Runtime { get; set; }
        [YamlMember(Alias = "packages", Order = 999)]
        public List<string> Packages { get; set; }
        [YamlMember(Alias = "description", Order = 2)]
        public string Description { get; set; }
        [YamlMember(Alias = "sdk", Order = 2)]
        public string Sdk { get; set; }
        [YamlMember(Alias = "packable", Order = 15)]
        public bool? Packable { get; set; }
        [YamlMember(Alias = "author", Order = 3)]
        public List<PackageAuthor> Authors { get; set; }
        [YamlMember(Alias = "version", Order = 4)]
        public string Version { get; set; }
        [YamlMember(Alias = "license", Order = 5)]
        public string License { get; set; }
        [YamlMember(Alias = "urls", Order = 6)]
        public PackageUrls Urls { get; set; }

        public static Project Load(FileInfo info)
        {
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            var text = info.ReadToEnd();

            return deserializer.Deserialize<Project>(text);
        }

        public void Save(FileInfo info)
        {
            var serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();

            var text = serializer.Serialize(this);

            File.WriteAllText(info.FullName, text);
        }
    }
}
