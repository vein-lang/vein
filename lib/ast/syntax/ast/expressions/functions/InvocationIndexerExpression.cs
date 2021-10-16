namespace vein.syntax;

using System.Collections.Generic;
using System.Linq;
using Sprache;

public class InvocationIndexerExpression : ExpressionSyntax, IPositionAware<InvocationIndexerExpression>
{
    public ExpressionSyntax Name { get; set; }
    public ExpressionSyntax[] Arguments { get; set; }

    public InvocationIndexerExpression(ExpressionSyntax name, IEnumerable<ExpressionSyntax> args)
        => (Name, this.Arguments) = (name, args.ToArray());

    public new InvocationIndexerExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}