namespace vein.syntax;

using System.Collections.Generic;
using System.Linq;
using Sprache;
using extensions;

public class ArgumentListExpression : ExpressionSyntax, IPositionAware<ArgumentListExpression>
{
    public ExpressionSyntax[] Arguments { get; set; }

    public ArgumentListExpression(IEnumerable<ExpressionSyntax> args)
        => this.Arguments = args.ToArray();

    public new ArgumentListExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }

    public override string ToString() => $"{Arguments.Select(x => x.ToString()).Join(", ")}";
}
