namespace vein.syntax
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using extensions;
    using Newtonsoft.Json;

    public class DocumentDeclaration
    {
        public string Name => Directives.OfExactType<SpaceSyntax>().Single().Value.Token;
        public IEnumerable<DirectiveSyntax> Directives { get; set; }
        public IEnumerable<MemberDeclarationSyntax> Members { get; set; }
        public FileInfo FileEntity { get; set; }
        public string SourceText { get; set; }
        public string[] SourceLines => SourceText.Replace("\r", "").Split("\n");

        private List<string> _includes;


        [JsonIgnore]
        public IEnumerable<BaseSyntax> ChildNodes =>
            Members.SelectMany(x => x.ChildNodes)
                .Concat(Directives.SelectMany(x => x.ChildNodes));

        public int[] _line_offsets;

        public List<string> Includes => _includes ??= Directives.OfExactType<UseSyntax>().Select(x =>
        {
            var result = x.Value.Token;

            if (!result.StartsWith("global::"))
                return $"global::{result}";
            return result;
        }).ToList();
    }
}
