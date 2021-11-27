namespace vein.syntax
{
    using System.Linq;
    using Sprache;
    using stl;

    public partial class VeinSyntax
    {
        // example: SomeValue
        protected internal virtual Parser<EnumMemberDeclarationSyntax> EnumMember =>
            from heading in MemberDeclarationHeading
            from identifier in IdentifierExpression.Commented(this)
            select new EnumMemberDeclarationSyntax(heading)
            {
                Identifier = identifier.Value,
                TrailingComments = identifier.TrailingComments.ToList(),
            };

        // example: public enum Weekday { Monday, Thursday }
        protected internal virtual Parser<EnumDeclarationSyntax> EnumDeclaration =>
            from heading in MemberDeclarationHeading
            from @enum in EnumDeclarationBody
            select new EnumDeclarationSyntax(heading)
            {
                Identifier = @enum.Identifier,
                Members = @enum.Members,
            };

        // example: enum Weekday { Monday, Thursday }
        protected internal virtual Parser<EnumDeclarationSyntax> EnumDeclarationBody =>
            from @enum in Parse.IgnoreCase("enum").Token()
            from identifier in IdentifierExpression
            from skippedComments in CommentParser.AnyComment.Token().Many()
            from openBrace in Parse.Char('{').Token()
            from members in EnumMember.DelimitedBy(Parse.Char(',').Commented(this))
            from closeBrace in Parse.Char('}').Commented(this)
            select new EnumDeclarationSyntax
            {
                Identifier = identifier,
                Members = members.ToList(),
                InnerComments = closeBrace.LeadingComments.ToList(),
                TrailingComments = closeBrace.TrailingComments.ToList(),
            };
    }
}
