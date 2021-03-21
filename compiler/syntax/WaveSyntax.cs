namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Sprache;
    using stl;

    public static class StringEx
    {
        public static string Capitalize(this string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;
            return $"{char.ToUpperInvariant(target[0])}{target.Remove(0, 1)}";
        }
    }
    public partial class WaveSyntax : ICommentParserProvider
    {
        public virtual IComment CommentParser => new CommentParser();
        
        
        
        protected internal virtual Parser<string> RawIdentifier =>
            from identifier in Parse.Identifier(Parse.Letter.Or(Parse.Chars("_@")), Parse.LetterOrDigit.Or(Parse.Char('_')))
            where !WaveKeywords.list.Contains(identifier)
            select identifier;

        protected internal virtual Parser<string> Identifier =>
            RawIdentifier.Token().Named("Identifier");
        
        protected internal virtual Parser<IEnumerable<string>> QualifiedIdentifier =>
            Identifier.DelimitedBy(Parse.Char('.').Token())
                .Named("QualifiedIdentifier");
        
        internal virtual Parser<string> Keyword(string text) =>
            Parse.IgnoreCase(text).Then(_ => Parse.LetterOrDigit.Or(Parse.Char('_')).Not()).Return(text);
        internal virtual Parser<WaveAnnotationKind> Keyword(WaveAnnotationKind value) =>
            Parse.IgnoreCase(value.ToString().ToLowerInvariant()).Then(_ => Parse.LetterOrDigit.Or(Parse.Char('_')).Not())
                .Return(value);
        
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
                .Token().Select(n => new TypeSyntax(n.Capitalize()))
                .Named("SystemType");

        protected internal virtual Parser<ModificatorSyntax> Modifier =>
            (from mod in Keyword("public").Or(
                    Keyword("protected")).Or(
                    Keyword("private")).Or(
                    Keyword("internal")).Or(
                    Keyword("override")).Or(
                    Keyword("static")).Or(
                    Keyword("const")).Or(
                    Keyword("global")).Or(
                    Keyword("extern"))
                .Text()
            select new ModificatorSyntax(mod))
            .Token()
            .Named("Modifier")
            .Positioned();

        protected internal virtual Parser<WaveAnnotationKind> Annotation =>
            Keyword(WaveAnnotationKind.Getter)
                .Or(Keyword(WaveAnnotationKind.Setter))
                .Or(Keyword(WaveAnnotationKind.Native))
                .Or(Keyword(WaveAnnotationKind.Readonly))
                .Or(Keyword(WaveAnnotationKind.Special))
                .Or(Keyword(WaveAnnotationKind.Virtual))
                .Token().Named("Annotation");
        
        internal virtual Parser<TypeSyntax> NonGenericType =>
            SystemType.Or(QualifiedIdentifier.Select(qi => new TypeSyntax(qi)));
        
        internal virtual Parser<TypeSyntax> TypeReference =>
            (from type in NonGenericType
                from parameters in TypeParameters.Optional()
                from arraySpecifier in Parse.Char('[').Token().Then(_ => Parse.Char(']').Token()).Optional()
                select new TypeSyntax(type)
                {
                    TypeParameters = parameters.GetOrElse(Enumerable.Empty<TypeSyntax>()).ToList(),
                    IsArray = arraySpecifier.IsDefined,
                }).Token().Positioned();
        
        internal virtual Parser<IEnumerable<TypeSyntax>> TypeParameters =>
            from open in Parse.Char('<').Token()
            from types in TypeReference.Token().Positioned().DelimitedBy(Parse.Char(',').Token())
            from close in Parse.Char('>').Token()
            select types;
        
        
        
        internal virtual Parser<ParameterSyntax> ParameterDeclaration =>
            from modifiers in Modifier.Token().Many().Commented(this)
            from name in Identifier.Commented(this)
            from @as in Parse.Char(':').Token().Commented(this)
            from type in TypeReference.Token().Positioned().Commented(this)
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
        
        // examples: /* this is a member */ public
        protected internal virtual Parser<MemberDeclarationSyntax> MemberDeclarationHeading =>
            (from comments in CommentParser.AnyComment.Token().Many()
                from annotation in AnnotationExpression.Optional()
                from modifiers in Modifier.Many()
                select new MemberDeclarationSyntax
                {
                    Annotations = annotation.GetOrEmpty().ToList(),
                    LeadingComments = comments.ToList(),
                    Modifiers = modifiers.ToList(),
                }).Token().Positioned();
        
        // examples:
        // @isTest void Test() {}
        // public static void Hello() {}
        protected internal virtual Parser<MethodDeclarationSyntax> MethodDeclaration =>
            from heading in MemberDeclarationHeading
            from name in Identifier
            from methodBody in MethodParametersAndBody
            select new MethodDeclarationSyntax(heading)
            {
                Identifier = name,
                Parameters = methodBody.Parameters,
                Body = methodBody.Body,
                ReturnType = methodBody.ReturnType
            };
        // examples:
        // Test() : void {}
        // Test() : void;
        protected internal virtual Parser<MethodDeclarationSyntax> MethodParametersAndBody =>
            from parameters in MethodParameters
            from @as in Parse.Char(':').Token().Commented(this)
            from type in TypeReference
            from methodBody in Block.Or(Parse.Char(';').Return(default(BlockSyntax)))
                .Token().Positioned()
            select new MethodDeclarationSyntax
            {
                Parameters = parameters,
                Body = methodBody,
                ReturnType = type
            };

        // foo.bar.zet
        protected internal virtual Parser<MemberAccessSyntax> MemberAccessExpression =>
            from identifier in QualifiedIdentifier
            select new MemberAccessSyntax
            {
                MemberName = identifier.Last(),
                MemberChain = identifier.SkipLast(1).ToArray()
            };

        protected internal virtual Parser<AnnotationSyntax> AnnotationSyntax =>
            (from kind in Annotation
                select new AnnotationSyntax(kind))
            .Positioned()
            .Token()
            .Named("annotation");


        protected internal virtual Parser<AnnotationSyntax[]> AnnotationExpression =>
            (from open in Parse.Char('[')
                from kinds in Parse.Ref(() => AnnotationSyntax).Positioned().DelimitedBy(Parse.Char(',').Token())
                from close in Parse.Char(']')
                select kinds.ToArray())
            .Token().Named("annotation list");
        
        // foo.bar()
        // foo.bar(1,24)
        protected internal virtual Parser<InvocationExpressionSyntax> InvocationExpression =>
            from identifier in QualifiedIdentifier
            from open in Parse.Char('(')
            from expression in Parse.Ref(() => argument_list).Optional()
            from close in Parse.Char(')')
            select new InvocationExpressionSyntax
            {
                Arguments = expression.GetOrEmpty().ToList(),
                FunctionName = identifier.Last(),
                MemberChain = identifier.SkipLast(1).ToArray(),
                ExpressionString = $"{identifier.Join('.')}(...)"
            };

        public virtual Parser<DocumentDeclaration> CompilationUnit =>
            from directives in 
                SpaceSyntax.Token()
                .Or(UseSyntax.Token()).Many()
            from members in ClassDeclaration.Token().AtLeastOnce()
            from whiteSpace in Parse.WhiteSpace.Many()
            from trailingComments in CommentParser.AnyComment.Token().Many().End()
            select new DocumentDeclaration
            {
                Directives = directives,
                Members = members.Select(x => x.WithTrailingComments(trailingComments))
            };
        
        
    }
    
    
    
    public class DocumentDeclaration
    {
        public string Name => Directives.OfExactType<SpaceSyntax>().Single().Value.Token;
        public IEnumerable<DirectiveSyntax> Directives { get; set; }
        public IEnumerable<MemberDeclarationSyntax> Members { get; set; }
        public FileInfo FileEntity { get; set; }
        public string SourceText { get; set; }
        public string[] SourceLines => SourceText.Replace("\r", "").Split("\n");

        private List<string> _includes;

        public List<string> Includes => _includes ??= Directives.OfExactType<UseSyntax>().Select(x =>
        {
            var result = x.Value.Token;

            if (!result.StartsWith("global::"))
                return $"global::{result}";
            return result;
        }).ToList();
    }
}