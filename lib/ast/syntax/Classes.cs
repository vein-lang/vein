namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using stl;

    public partial class ManaSyntax
    {
        protected internal virtual Parser<MemberDeclarationSyntax> ClassMemberDeclaration =>
            from member in
                CtorDeclaration.Or(MethodOrPropertyDeclaration).Token()
                    .OrPreview(FieldDeclaration.Token())
                    .Commented(this)
            select member.Value
                .WithLeadingComments(member.LeadingComments)
                .WithTrailingComments(member.TrailingComments);

        protected internal virtual Parser<ParameterSyntax> NameAndType =>
            from name in IdentifierExpression.Optional()
            from @as in Parse.Char(':').Token().Commented(this)
            from type in TypeReference.Token().Positioned()
            select new ParameterSyntax(type, name.GetOrDefault());
        /// <summary></summary>
        /// <example>
        /// foo: Type;
        /// [special] foo: Type;
        /// [special] public foo: Type;
        /// </example>
        protected internal virtual Parser<PropertyDeclarationSyntax> PropertyDeclaration =>
            from heading in MemberDeclarationHeading
            from typeAndName in NameAndType
            from accessors in PropertyAccessors
            select new PropertyDeclarationSyntax(heading)
            {
                Type = typeAndName.Type,
                Identifier = typeAndName.Identifier,
                Accessors = accessors.Accessors,
            };
        // example: private static x, y, z: int = 3;
        protected internal virtual Parser<FieldDeclarationSyntax> FieldDeclaration =>
            from heading in MemberDeclarationHeading.Token()
            from identifier in IdentifierExpression.Commented(this)
            from twodot in Parse.Char(':').Token()
            from type in TypeReference.Token().Positioned()
            from expression in Parse.Char('=').Token().Then(_ => QualifiedExpression).Positioned().Optional()
            from semicolon in Parse.Char(';').Token()
            select new FieldDeclarationSyntax(heading)
            {
                Type = type,
                Field = new ()
                {
                    Identifier = identifier.Value,
                    Expression = expression.GetOrDefault(),
                    LeadingComments = identifier.LeadingComments.ToList()
                },
            };
        // examples: get; private set; get { return 0; }
        protected internal virtual Parser<AccessorDeclarationSyntax> PropertyAccessor =>
            from heading in MemberDeclarationHeading
            from keyword in Parse.IgnoreCase("get").Or(Parse.IgnoreCase("set")).Token().Text()
            from body in Parse.Char(';').Return(default(BlockSyntax)).Or(Block).Commented(this)
            select new AccessorDeclarationSyntax(heading)
            {
                IsGetter = keyword == "get",
                Body = body.Value,
                TrailingComments = body.TrailingComments.ToList(),
            };
        // example: { get; set; }
        protected internal virtual Parser<PropertyDeclarationSyntax> PropertyAccessors =>
            from openBrace in Parse.Char('{').Token()
            from accessors in PropertyAccessor.Many()
            from closeBrace in Parse.Char('}').Token()
            select new PropertyDeclarationSyntax(accessors);
        
        // method or property declaration starting with the type and name
        protected internal virtual Parser<MemberDeclarationSyntax> MethodOrPropertyDeclaration =>
            from dec in MemberDeclarationHeading
            from name in IdentifierExpression
            from member in MethodParametersAndBody.Select(c => c as MemberDeclarationSyntax)
                .XOr(PropertyAccessors)
            select member.WithName(name).WithProperties(dec);

        protected internal virtual Parser<MemberDeclarationSyntax> CtorDeclaration =>
            from dec in MemberDeclarationHeading
            from kw in KeywordExpression("new").Or(
                KeywordExpression("delete"))
            from member in CtorParametersAndBody.Select(c => c as MemberDeclarationSyntax)
            select member.WithName(kw).WithProperties(dec);
        
        // example: @TestFixture public static class Program { static void main() {} }
        public virtual Parser<ClassDeclarationSyntax> ClassDeclaration =>
            from heading in MemberDeclarationHeading.Token()
            from classBody in ClassDeclarationBody.Token()
            select ClassDeclarationSyntax.Create(heading, classBody);

        // example: class Program { void main() {} }
        protected internal virtual Parser<ClassDeclarationSyntax> ClassDeclarationBody =>
            from @class in 
                Parse.IgnoreCase("class").Text().Token()
                    .Or(Parse.IgnoreCase("interface").Text().Token())
                    .Or(Parse.IgnoreCase("struct").Text().Token())
            from className in IdentifierExpression.Token().Positioned()
            from interfaces in Parse.IgnoreCase(":").Token().Then(t => TypeReference.Positioned().DelimitedBy(Parse.Char(',').Token())).Optional()
            from skippedComments in CommentParser.AnyComment.Token().Many()
            from openBrace in Parse.Char('{').Token()
            from members in ClassMemberDeclaration.Token().Many()
            from closeBrace in Parse.Char('}').Token().Commented(this)
            let classBody = new ClassDeclarationSyntax
            {
                Identifier = className,
                IsInterface = @class == "interface",
                IsStruct = @class == "struct",
                Inheritances = interfaces.GetOrElse(Enumerable.Empty<TypeSyntax>()).ToList(),
                Members = ConvertConstructors(members, className).ToList(),
                InnerComments = closeBrace.LeadingComments.ToList(),
                TrailingComments = closeBrace.TrailingComments.ToList(),
            }
            select ClassDeclarationSyntax.Create(null, classBody);

        private IEnumerable<MemberDeclarationSyntax> ConvertConstructors(IEnumerable<MemberDeclarationSyntax> members, IdentifierExpression className)
        {
            foreach (var member in members)
            {
                if (member is MethodDeclarationSyntax m && m.IsConstructor(className.ExpressionString))
                {
                    yield return new ConstructorDeclarationSyntax(m);
                    continue;
                }

                yield return member;
            }
        }
    }
}