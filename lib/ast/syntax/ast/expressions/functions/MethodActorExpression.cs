namespace vein.syntax;

using System;
using System.Linq;
using Sprache;

[Obsolete]
public class MethodActorExpression : ExpressionSyntax, IPositionAware<MethodActorExpression>
{
    public readonly IdentifierExpression Name;
    public readonly MethodInvocationExpression Actor;

    public MethodActorExpression(IdentifierExpression name, MethodInvocationExpression actor)
    {
        (Name, Actor) = (name, actor);
        UpdatePos(name, actor);
    }

    public MethodActorExpression UpdatePos(params ExpressionSyntax[] exps)
    {
        foreach (var exp in exps)
        {
            if (exp.Transform is null)
                throw new CorruptedChainException(exp);
        }

        var sum = exps.Sum(x => x.Transform.len);
        var first = exps.First();
        return this.SetPos(first.Transform.pos, sum);
    }

    public new MethodActorExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}