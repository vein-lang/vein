namespace vein.syntax;

using System.Linq;
using Sprache;
using stl;
using extensions;

public class InvocationExpression(ExpressionSyntax name, IOption<ArgumentListExpression> args)
    : ExpressionSyntax, IPositionAware<InvocationExpression>
{
    public ExpressionSyntax Name { get; set; } = name;
    public ArgumentListExpression Arguments { get; set; } = args.GetOrDefault() ?? new ArgumentListExpression(Array.Empty<ExpressionSyntax>());
    public bool NoReturn { get; private set; }

    public new InvocationExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }

    public InvocationExpression WithNoReturn()
    {
        NoReturn = true;
        return this;
    }

    public override string ToString() => $"{Name}({Arguments.Arguments.Select(x => x.ToString()).Join(", ")})";
}
