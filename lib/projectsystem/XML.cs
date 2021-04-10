namespace wave.project
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public static class XML
    {
        [XmlRoot(ElementName="ref")]
        public class RefBlock
        {
            [XmlAttribute(AttributeName="name")]
            public string Name { get; set; }
        }
        [XmlRoot(ElementName="packages")]
        public class Packages 
        {
            [XmlElement(ElementName="ref")]
            public List<RefBlock> Ref { get; set; }
        }
        [XmlRoot(ElementName="project", Namespace = "")]
        public class Project 
        {
            [XmlElement(ElementName="target")]
            public string Target { get; set; }
            [XmlElement(ElementName="packages")]
            public Packages Packages { get; set; }
            [XmlAttribute(AttributeName="sdk")]
            public string Sdk { get; set; }
        }
    }
}