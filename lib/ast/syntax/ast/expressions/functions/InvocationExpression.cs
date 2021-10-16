namespace vein.syntax;

using System.Linq;
using Sprache;
using stl;

public class InvocationExpression : ExpressionSyntax, IPositionAware<InvocationExpression>
{
    public ExpressionSyntax Name { get; set; }
    public ExpressionSyntax[] Arguments { get; set; }

    public InvocationExpression(ExpressionSyntax name, IOption<ExpressionSyntax[]> args)
        => (Name, this.Arguments) = (name, args.GetOrEmpty().ToArray());

    public new InvocationExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}