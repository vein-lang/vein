namespace vein.project
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using SharpYaml.Serialization;

    public static class YAML
    {
        public class Project
        {
            [YamlMember("target")]
            public string Target { get; set; }
            [YamlMember("runtime")]
            public string Runtime { get; set; }
            [YamlMember("packages")]
            public List<string> Packages { get; set; }
            [YamlMember("sdk")]
            public string Sdk { get; set; }
            [YamlMember("packable")]
            public bool Packable { get; set; }
            [YamlMember("author")]
            public string Author { get; set; }
            [YamlMember("version")]
            public string Version { get; set; }
            [YamlMember("license")]
            public string License { get; set; }
        }
    }
}
