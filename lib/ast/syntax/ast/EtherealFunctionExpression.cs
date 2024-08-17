namespace vein.syntax;

using Sprache;

public abstract class EtherealFunctionExpression(List<TypeExpression> generics, IOption<ExpressionSyntax> expression) : ExpressionSyntax, IPositionAware<EtherealFunctionExpression>
{
    public List<TypeExpression> Generics { get; } = generics;
    public IOption<ExpressionSyntax> Expression { get; } = expression;

    public new EtherealFunctionExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }

    public abstract bool IsRuntimeRequired { get; }


    public static EtherealFunctionExpression Select(string keyword, List<TypeExpression> generics, IOption<ExpressionSyntax> expression)
    {
        if (keyword.Equals("nameof"))
            return new NameOfFunctionExpression(generics, expression);
        if (keyword.Equals("as"))
            return new TypeAsFunctionExpression(generics, expression);
        if (keyword.Equals("is"))
            return new TypeIsFunctionExpression(generics, expression);
        if (keyword.Equals("typeof"))
            return new TypeOfFunctionExpression(generics, expression);
        if (keyword.Equals("sizeof"))
            return new SizeOfFunctionExpression(generics, expression);
        throw new InvalidOperationException();
    }
}


public sealed class NameOfFunctionExpression(List<TypeExpression> generics, IOption<ExpressionSyntax> expression)
    : EtherealFunctionExpression(generics, expression), IPositionAware<NameOfFunctionExpression>
{
    public override bool IsRuntimeRequired => false;
    public new NameOfFunctionExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}

public sealed class TypeOfFunctionExpression(List<TypeExpression> generics, IOption<ExpressionSyntax> expression)
    : EtherealFunctionExpression(generics, expression), IPositionAware<TypeOfFunctionExpression>
{
    public override bool IsRuntimeRequired => true;
    public new TypeOfFunctionExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
public sealed class TypeIsFunctionExpression(List<TypeExpression> generics, IOption<ExpressionSyntax> expression)
    : EtherealFunctionExpression(generics, expression), IPositionAware<TypeIsFunctionExpression>
{
    public override bool IsRuntimeRequired => true;
    public new TypeIsFunctionExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}

public sealed class TypeAsFunctionExpression(List<TypeExpression> generics, IOption<ExpressionSyntax> expression)
    : EtherealFunctionExpression(generics, expression), IPositionAware<TypeAsFunctionExpression>
{
    public override bool IsRuntimeRequired => true;
    public new TypeAsFunctionExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}


public sealed class SizeOfFunctionExpression(List<TypeExpression> generics, IOption<ExpressionSyntax> expression)
    : EtherealFunctionExpression(generics, expression), IPositionAware<SizeOfFunctionExpression>
{
    public override bool IsRuntimeRequired => false;
    public new SizeOfFunctionExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
