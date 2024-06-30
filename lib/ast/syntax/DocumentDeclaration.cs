namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using extensions;
    using Newtonsoft.Json;

    public class DocumentDeclaration
    {
        public string Name
        {
            get
            {
                var space = Directives.OfExactType<SpaceSyntax>().SingleOrDefault();

                if (space is null)
                    throw new Exception($"No #space declaration detected.");

                return Directives.OfExactType<SpaceSyntax>().Single().Value.Token;
            }
        }

        public IEnumerable<DirectiveSyntax> Directives { get; set; }
        public IEnumerable<MemberDeclarationSyntax> Members { get; set; }
        public IEnumerable<AspectDeclarationSyntax> Aspects { get; set; }
        public IEnumerable<AliasSyntax> Aliases { get; set; }
        public FileInfo FileEntity { get; set; }
        public string SourceText { get; set; }
        public string[] SourceLines => SourceText.Replace("\r", "").Split("\n");

        private List<string>? _includes;

        public int[]? _line_offsets;

        public List<string> Includes => _includes ??= Directives.OfExactType<UseSyntax>().Select(x =>
        {
            var result = x.Value.Token;

            if (!result.StartsWith(""))
                return $"{result}";
            return result;
        }).ToList();
    }
}
