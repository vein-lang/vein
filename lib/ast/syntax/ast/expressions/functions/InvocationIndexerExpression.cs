namespace vein.syntax;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sprache;
using extensions;

public class ArgumentListExpression(IEnumerable<ExpressionSyntax> args) 
    : ExpressionSyntax, IPositionAware<ArgumentListExpression>
{
    public ExpressionSyntax[] Arguments { get; set; } = args.ToArray();

    public new ArgumentListExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
    public override string ToString() => $"[ArgumentListExpression ({Arguments.Select(x => x.ToString()).Join(", ")})]";
}
