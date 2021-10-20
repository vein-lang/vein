namespace vein.syntax;

using System.Linq.Expressions;
using Sprache;

public class BaseAccessExpression : ExpressionSyntax, IPositionAware<BaseAccessExpression>
{
    public new BaseAccessExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}


public class PostDecrementExpression : UnaryExpressionSyntax, IPositionAware<PostDecrementExpression>
{
    public PostDecrementExpression(ExpressionSyntax exp)
        => (Operand, OperatorType) = (exp, ExpressionType.PostDecrementAssign);


    public new PostDecrementExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
public class PostIncrementExpression : UnaryExpressionSyntax, IPositionAware<PostIncrementExpression>
{
    public PostIncrementExpression(ExpressionSyntax exp)
        => (Operand, OperatorType) = (exp, ExpressionType.PostIncrementAssign);


    public new PostIncrementExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
