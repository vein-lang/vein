namespace vein.syntax;

using System.Collections.Generic;
using System.Linq;
using Sprache;
using stl;
using extensions;

public class InvocationExpression(ExpressionSyntax name, IOption<ArgumentListExpression> args)
    : ExpressionSyntax, IPositionAware<InvocationExpression>
{
    public ExpressionSyntax Name { get; set; } = name;
    public ArgumentListExpression Arguments { get; set; } = args?.GetOrDefault() ?? new ArgumentListExpression(Array.Empty<ExpressionSyntax>());
    public bool NoReturn { get; private set; }

    /// <summary>Explicit method type-arguments at the call site, e.g. the <c>i32</c> in <c>foo&lt;i32&gt;(5)</c>.</summary>
    public List<TypeExpression> TypeArguments { get; set; } = new();

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

    public InvocationExpression(ExpressionSyntax name, InvocationExpression other) : this(name, (IOption<ArgumentListExpression>?)null!)
        => Arguments = other.Arguments;

    public override string ToString() => $"{Name}({Arguments.Arguments.Select(x => x.ToString()).Join(", ")})";
}
