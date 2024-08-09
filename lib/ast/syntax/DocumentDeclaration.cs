namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using extensions;
    using Newtonsoft.Json;
    using runtime;
    using Spectre.Console;

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

        public NamespaceSymbol Namespace => new(Name);

        public IEnumerable<DirectiveSyntax> Directives { get; init; }
        public IEnumerable<MemberDeclarationSyntax> Members { get; init; }
        public IEnumerable<AspectDeclarationSyntax> Aspects { get; set; }
        public IEnumerable<AliasSyntax> Aliases { get; init; }
        public FileInfo FileEntity { get; set; }
        public string SourceText { get; set; }
        public string[] SourceLines => SourceText.Replace("\r", "").Split("\n");

        private List<NamespaceSymbol>? _includes;


        public List<NamespaceSymbol> Includes => _includes ??= Directives.OfExactType<UseSyntax>().Select(x =>
        {
            var result = x.Value.Token;
            return new NamespaceSymbol(result);
        }).ToList();


        public override string ToString() => $"Document [{FileEntity.FullName}]".EscapeMarkup();
    }
}
