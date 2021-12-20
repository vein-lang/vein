namespace vein.syntax;

using Sprache;
using System.Collections.Generic;
using System.Linq;
using stl;

public partial class VeinSyntax
{
    public virtual Parser<AspectDeclarationSyntax> AspectDeclaration =>
        from heading in MemberDeclarationHeading.Token().Positioned()
        from classBody in AspectDeclarationBody.Token().Positioned()
        select classBody
            .SetEnd(classBody.EndPoint)
            .SetStart(heading.Transform.pos)
            .SetPos<AspectDeclarationSyntax>(classBody.Transform)
            .WithHead(heading);
    /// example: aspect foo(i: int64);
    protected internal virtual Parser<AspectDeclarationSyntax> AspectDeclarationBody =>
        from @class in
            Parse.IgnoreCase("aspect").Text().Token().Commented(this)
        from aspectName in IdentifierExpression.Token().Positioned()
        from skippedComments in CommentParser.AnyComment.Token().Many()
        from @params in MethodParameters.Token()
        from end in Parse.Char(';').Token().Commented(this)
        select new AspectDeclarationSyntax
        {
            Identifier = aspectName,
            Args = @params
        };

}
