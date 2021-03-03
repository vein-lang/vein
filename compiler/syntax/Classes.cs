namespace wave.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using stl;

    public partial class WaveSyntax
    {
        // class members: methods, classes, properties
        protected internal virtual Parser<MemberDeclarationSyntax> ClassMemberDeclaration =>
            from heading in MemberDeclarationHeading
            from member in ClassInitializerBody.Select(c => c as MemberDeclarationSyntax)
                .Or(EnumDeclarationBody)
                .Or(ClassDeclarationBody)
                .Or(MethodOrPropertyDeclaration)
                .Or(FieldDeclaration)
            select member.WithProperties(heading);
        
        // examples: { instanceProperty = 0; }, static { staticProperty = 0; }
        protected internal virtual Parser<ClassInitializerSyntax> ClassInitializer =>
            from heading in MemberDeclarationHeading
            from initializer in ClassInitializerBody
            select initializer.WithProperties(heading);

        // examples: { a = 0; }
        protected internal virtual Parser<ClassInitializerSyntax> ClassInitializerBody =>
            from body in Block
            select new ClassInitializerSyntax
            {
                Body = body,
            };
        
        // example: @required public String name { get; set; }
        protected internal virtual Parser<PropertyDeclarationSyntax> PropertyDeclaration =>
            from heading in MemberDeclarationHeading
            from typeAndName in TypeAndName
            from accessors in PropertyAccessors
            select new PropertyDeclarationSyntax(heading)
            {
                Type = typeAndName.Type,
                Identifier = typeAndName.Identifier,
                Accessors = accessors.Accessors,
            };
        // example: private static x, y, z: int = 3;
        protected internal virtual Parser<FieldDeclarationSyntax> FieldDeclaration =>
            from heading in MemberDeclarationHeading
            from declarators in FieldDeclarator.DelimitedBy(Parse.Char(',').Token())
            from twodot in Parse.Char(':').Token()
            from type in TypeReference
            from semicolon in Parse.Char(';').Token()
            select new FieldDeclarationSyntax(heading)
            {
                Type = type,
                Fields = declarators.ToList(),
            };
        // example: now = DateTime.Now()
        protected internal virtual Parser<FieldDeclaratorSyntax> FieldDeclarator =>
            from identifier in Identifier.Commented(this)
            from expression in Parse.Char('=').Token().Then(c => GenericExpression).Optional()
            select new FieldDeclaratorSyntax
            {
                Identifier = identifier.Value,
                Expression = ExpressionSyntax.CreateOrDefault(expression),
                LeadingComments = identifier.LeadingComments.ToList(),
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
            from typeAndName in TypeAndName
            from member in MethodParametersAndBody.Select(c => c as MemberDeclarationSyntax)
                .XOr(PropertyAccessors)
            select member.WithTypeAndName(typeAndName);
        
        // example: @TestFixture public static class Program { static void main() {} }
        public virtual Parser<ClassDeclarationSyntax> ClassDeclaration =>
            from heading in MemberDeclarationHeading
            from classBody in ClassDeclarationBody
            select ClassDeclarationSyntax.Create(heading, classBody);

        // example: class Program { void main() {} }
        protected internal virtual Parser<ClassDeclarationSyntax> ClassDeclarationBody =>
            from @class in 
                Parse.IgnoreCase("class").Text().Token()
                    .Or(Parse.IgnoreCase("interface").Text().Token())
                    .Or(Parse.IgnoreCase("struct").Text().Token())
            from className in Identifier
            from interfaces in Parse.IgnoreCase(":").Token().Then(t => TypeReference.DelimitedBy(Parse.Char(',').Token())).Optional()
            from skippedComments in CommentParser.AnyComment.Token().Many()
            from openBrace in Parse.Char('{').Token()
            from members in ClassMemberDeclaration.Many()
            from closeBrace in Parse.Char('}').Commented(this)
            let classBody = new ClassDeclarationSyntax
            {
                Identifier = className,
                IsInterface = @class == "interface",
                IsStruct = @class == "struct",
                Inheritance = interfaces.GetOrElse(Enumerable.Empty<TypeSyntax>()).ToList(),
                Members = ConvertConstructors(members, className).ToList(),
                InnerComments = closeBrace.LeadingComments.ToList(),
                TrailingComments = closeBrace.TrailingComments.ToList(),
            }
            select ClassDeclarationSyntax.Create(null, classBody);

        private IEnumerable<MemberDeclarationSyntax> ConvertConstructors(IEnumerable<MemberDeclarationSyntax> members, string className)
        {
            foreach (var member in members)
            {
                if (member is MethodDeclarationSyntax m && m.IsConstructor(className))
                {
                    yield return new ConstructorDeclarationSyntax(m);
                    continue;
                }

                yield return member;
            }
        }
    }
}