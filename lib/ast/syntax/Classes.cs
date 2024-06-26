namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using stl;

    public partial class VeinSyntax
    {
        protected internal virtual Parser<MemberDeclarationSyntax> ClassMemberDeclaration =>
            from member in
                FieldDeclaration
                    .Or(MethodDeclaration).Token()
                    .Or(PropertyDeclarationShortform).Token()
                    .Or(PropertyDeclaration).Token()
                    .Or(CtorDeclaration).Token()
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
            from heading in MemberDeclarationHeading.Positioned()
            from typeAndName in NameAndType.Positioned()
            from accessors in PropertyBody
            select new PropertyDeclarationSyntax(heading)
            {
                Type = typeAndName.Type,
                Identifier = typeAndName.Identifier,
                Accessors = accessors.Accessors,
            };
        protected internal virtual Parser<PropertyDeclarationSyntax> PropertyDeclarationShortform =>
            from heading in MemberDeclarationHeading.Positioned()
            from typeAndName in NameAndType.Positioned()
            from op in Parse.String("|>").Token()
            from exp in QualifiedExpression.Positioned()
            from end in Parse.IgnoreCase(";").Token()
            select new PropertyDeclarationSyntax(heading)
            {
                Type = typeAndName.Type,
                Identifier = typeAndName.Identifier,
                Expression = exp
            };

        /// example: private static x, y, z: int = 3;
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
                Field = new()
                {
                    Identifier = identifier.Value,
                    Expression = expression.GetOrDefault(),
                    LeadingComments = identifier.LeadingComments.ToList()
                },
            };
        /// examples: get; private set; get { return 0; }
        protected internal virtual Parser<AccessorDeclarationSyntax> PropertyAccessor =>
            from heading in MemberDeclarationHeading.Positioned()
            from keyword in Parse.IgnoreCase("get").Or(Parse.IgnoreCase("set")).Token().Text()
            from body in Parse.Char(';').Return(default(BlockSyntax)).Or(Block).Commented(this)
            select new AccessorDeclarationSyntax(heading)
            {
                IsGetter = keyword == "get",
                Body = body.Value,
                TrailingComments = body.TrailingComments.ToList(),
            };
        /// example: { get; set; }
        protected internal virtual Parser<PropertyDeclarationSyntax> PropertyBody =>
            from openBrace in Parse.Char('{').Token()
            from accessors in PropertyAccessor.Positioned().Many()
            from closeBrace in Parse.Char('}').Token()
            select new PropertyDeclarationSyntax(accessors);

        /// method or property declaration starting with the type and name
        protected internal virtual Parser<MemberDeclarationSyntax> MethodDeclaration =>
            from dec in MemberDeclarationHeading.Positioned()
            from name in IdentifierExpression.Positioned()
            from member in MethodParametersAndBody.Select(c => c as MemberDeclarationSyntax)
            select member.WithName(name).WithProperties(dec);

        protected internal virtual Parser<MemberDeclarationSyntax> CtorDeclaration =>
            from dec in MemberDeclarationHeading.Positioned()
            from kw in KeywordExpression("new").Or(
                KeywordExpression("delete"))
            from member in CtorParametersAndBody.Select(c => c as MemberDeclarationSyntax)
            select member.WithName(kw).WithProperties(dec);

        /// example: @TestFixture public static class Program { static void main() {} }
        public virtual Parser<ClassDeclarationSyntax> ClassDeclaration =>
            from heading in MemberDeclarationHeading.Token().Positioned()
            from classBody in ClassDeclarationBody.Token().Positioned()
            select ClassDeclarationSyntax.Create(heading, classBody)
                .SetEnd(classBody.EndPoint)
                .SetStart(heading.Transform.pos)
                .SetPos<ClassDeclarationSyntax>(classBody.Transform);


        protected internal virtual Parser<ClassDeclarationSyntax> ClassDeclarationBody =>
            from @class in
                Parse.IgnoreCase("class").Text().Token()
                    .Or(Parse.IgnoreCase("interface").Text().Token())
                    .Or(Parse.IgnoreCase("struct").Text().Token()).Commented(this)
            from className in IdentifierExpression.Token().Positioned()
            from generics in GenericsDeclarationParser.Token().Optional()
            from interfaces in Parse.IgnoreCase(":").Token()
                .Then(_ => TypeReference.Positioned().DelimitedBy(Parse.Char(',').Token())).Optional()
            from skippedComments in CommentParser.AnyComment.Token().Many()
            from constraints in GenericConstraintParser.Token().Optional()
            from openBrace in Parse.Char('{').Token().Commented(this)
            from members in ClassMemberDeclaration.Positioned().Token().Many()
            from closeBrace in Parse.Char('}').Token().Commented(this)
            let classBody = new ClassDeclarationSyntax
            {
                Identifier = className,
                IsInterface = @class.Value == Keywords.INTERFACE,
                IsStruct = @class.Value == Keywords.STRUCT,
                Inheritances = interfaces.GetOrEmpty().ToList(),
                Members = ConvertConstructors(members, className).ToList(),
                InnerComments = closeBrace.LeadingComments.ToList(),
                TrailingComments = closeBrace.TrailingComments.ToList(),
                TypeParameterConstraints = constraints.GetOrEmpty().ToList(),
                GenericTypes = generics.GetOrEmpty().ToList()
            }
            select ClassDeclarationSyntax.Create(null, classBody)
                .SetStart(@class.Transform.pos)
                .SetEnd(closeBrace.Transform.pos)
                .As<ClassDeclarationSyntax>();
        
        protected internal virtual Parser<List<TypeParameterConstraintSyntax>> GenericConstraintParser =>
            from keyword in Parse.IgnoreCase("when").Token()
            from data in GenericConstraintUnitParser.Positioned().DelimitedBy(Parse.Char(',').Token())
            select data.ToList();

        protected internal virtual Parser<TypeParameterConstraintSyntax> GenericConstraintUnitParser =>
            from genericIndex in TypeExpression.Token().Positioned()
            from keyword1 in Parse.IgnoreCase("is").Token()
            from constraint in TypeExpression.Token().Positioned()
            select new TypeParameterConstraintSyntax(genericIndex, constraint);

        protected internal virtual Parser<List<TypeExpression>> GenericsDeclarationParser =>
            from openBrace in Parse.Char('<').Token().Commented(this)
            from types in TypeExpression.Token().Positioned().DelimitedBy(Parse.Char(','))
            from closeBrace in Parse.Char('>').Token().Commented(this)
            select types.ToList();




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
