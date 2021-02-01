namespace wave.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using stl;

    public partial class WaveSyntax : ICommentParserProvider
    {
        public virtual IComment CommentParser => new CommentParser();
        
        
        
        protected internal virtual Parser<string> RawIdentifier =>
            from identifier in Parse.Identifier(Parse.Letter, Parse.LetterOrDigit.Or(Parse.Char('_')))
            where !WaveKeywords.list.Contains(identifier)
            select identifier;

        protected internal virtual Parser<string> Identifier =>
            RawIdentifier.Token().Named("Identifier");
        
        protected internal virtual Parser<IEnumerable<string>> QualifiedIdentifier =>
            Identifier.DelimitedBy(Parse.Char('.').Token())
                .Named("QualifiedIdentifier");
        
        internal virtual Parser<string> Keyword(string text) =>
            Parse.IgnoreCase(text).Then(_ => Parse.LetterOrDigit.Or(Parse.Char('_')).Not()).Return(text);
        
        protected internal virtual Parser<TypeSyntax> SystemType =>
            Keyword("byte").Or(
                    Keyword("sbyte")).Or(
                    Keyword("int16")).Or(
                    Keyword("uint16")).Or(
                    Keyword("int32")).Or(
                    Keyword("uint32")).Or(
                    Keyword("int64")).Or(
                    Keyword("uint64")).Or(
                    Keyword("bool")).Or(
                    Keyword("string")).Or(
                    Keyword("char")).Or(
                    Keyword("void"))
                .Token().Select(n => new TypeSyntax(n))
                .Named("SystemType");
        
        protected internal virtual Parser<string> Modifier =>
            Keyword("public").Or(
                    Keyword("protected")).Or(
                    Keyword("private")).Or(
                    Keyword("static")).Or(
                    Keyword("abstract")).Or(
                    Keyword("const")).Or(
                    Keyword("readonly")).Or(
                    Keyword("global")).Or(
                    Keyword("extern"))
                .Text().Token().Named("Modifier");
        
        internal virtual Parser<TypeSyntax> NonGenericType =>
            SystemType.Or(QualifiedIdentifier.Select(qi => new TypeSyntax(qi)));
        
        internal virtual Parser<TypeSyntax> TypeReference =>
            from type in NonGenericType
            from parameters in TypeParameters.Optional()
            from arraySpecifier in Parse.Char('[').Token().Then(_ => Parse.Char(']').Token()).Optional()
            select new TypeSyntax(type)
            {
                TypeParameters = parameters.GetOrElse(Enumerable.Empty<TypeSyntax>()).ToList(),
                IsArray = arraySpecifier.IsDefined,
            };
        
        internal virtual Parser<IEnumerable<TypeSyntax>> TypeParameters =>
            from open in Parse.Char('<').Token()
            from types in TypeReference.DelimitedBy(Parse.Char(',').Token())
            from close in Parse.Char('>').Token()
            select types;
        
        
        
        internal virtual Parser<ParameterSyntax> ParameterDeclaration =>
            from modifiers in Modifier.Token().Many().Commented(this)
            from name in Identifier.Commented(this)
            from @as in Parse.Char(':').Token().Commented(this)
            from type in TypeReference.Commented(this)
            select new ParameterSyntax(type.Value, name.Value)
            {
                LeadingComments = modifiers.LeadingComments.Concat(type.LeadingComments).ToList(),
                Modifiers = modifiers.Value.ToList(),
                TrailingComments = name.TrailingComments.ToList(),
            };
        
        protected internal virtual Parser<IEnumerable<ParameterSyntax>> ParameterDeclarations =>
            ParameterDeclaration.DelimitedBy(Parse.Char(',').Token());

        // example: (string a, char delimiter)
        protected internal virtual Parser<List<ParameterSyntax>> MethodParameters =>
            from openBrace in Parse.Char('(').Token()
            from param in ParameterDeclarations.Optional()
            from closeBrace in Parse.Char(')').Token()
            select param.GetOrElse(Enumerable.Empty<ParameterSyntax>()).ToList();
        
        
        
        // examples: string Name, void Test
        protected internal virtual Parser<ParameterSyntax> TypeAndName =>
            from type in TypeReference
            from name in Identifier.Optional()
            select new ParameterSyntax(type, name.GetOrDefault());
        // examples: /* this is a member */ public
        protected internal virtual Parser<MemberDeclarationSyntax> MemberDeclarationHeading =>
            from comments in CommentParser.AnyComment.Token().Many()
            from modifiers in Modifier.Many()
            select new MemberDeclarationSyntax
            {
                LeadingComments = comments.ToList(),
                Modifiers = modifiers.ToList(),
            };
        
        // examples:
        // @isTest void Test() {}
        // public static void Hello() {}
        protected internal virtual Parser<MethodDeclarationSyntax> MethodDeclaration =>
            from heading in MemberDeclarationHeading
            from typeAndName in TypeAndName
            from methodBody in MethodParametersAndBody
            select new MethodDeclarationSyntax(heading)
            {
                Identifier = typeAndName.Identifier ?? typeAndName.Type.Identifier,
                ReturnType = typeAndName.Type,
                Parameters = methodBody.Parameters,
                Body = methodBody.Body,
            };
        // examples:
        // void Test() {}
        // string Hello(string name) {}
        // int Dispose();
        protected internal virtual Parser<MethodDeclarationSyntax> MethodParametersAndBody =>
            from parameters in MethodParameters
            from methodBody in Block.Or(Parse.Char(';').Return(default(BlockSyntax))).Token()
            select new MethodDeclarationSyntax
            {
                Parameters = parameters,
                Body = methodBody,
            };

        public virtual Parser<DocumentDeclaration> CompilationUnit =>
            from includes in UseSyntax.Many().Optional()
            from members in ClassDeclaration.Select(c => c as MemberDeclarationSyntax).Or(EnumDeclaration).Many()
            from whiteSpace in Parse.WhiteSpace.Many()
            from trailingComments in CommentParser.AnyComment.Token().Many().End()
            select new DocumentDeclaration
            {
                Members = members.Select(x => x.WithTrailingComments(trailingComments)),
                Uses = includes.GetOrElse(new List<UseSyntax>())
            };
        
        
    }
    
    public class DocumentDeclaration
    {
        public string Name { get; set; }
        public IEnumerable<UseSyntax> Uses { get; set; }
        public IEnumerable<MemberDeclarationSyntax> Members { get; set; }
    }
}