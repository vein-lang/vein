namespace vein.syntax;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Sprache;

public class FieldDeclaratorSyntax : BaseSyntax
{
    public override SyntaxType Kind => SyntaxType.FieldDeclarator;

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

    public IdentifierExpression Identifier { get; set; }

    public ExpressionSyntax Expression
    {
        get => _expression;
        set => _expression = value is UnaryExpressionSyntax
        {
            OperatorType: ExpressionType.Negate,
            Operand: UndefinedIntegerNumericLiteral numeric
        } ? RedefineIntegerExpression(numeric, true) :
            TransformationWalkerExpression(value);
    }


    private ExpressionSyntax _expression;

    private ExpressionSyntax TransformationWalkerExpression(ExpressionSyntax exp)
    {
        if (exp is UndefinedIntegerNumericLiteral numeric)
            return RedefineIntegerExpression(numeric, false);
        return exp; // TODO
    }

    internal static ExpressionSyntax RedefineIntegerExpression(NumericLiteralExpressionSyntax integer, bool isNegate)
    {
        var token = integer.Token;
        var (pos, len) = integer.Transform;

        if (integer.Transform is null)
            throw new ArgumentNullException($"{nameof(integer.Transform)} in '{integer}' has null.");

        if (isNegate)
            token = $"-{token}";
        if (!isNegate && token.StartsWith("-"))
            isNegate = true;

        if (isNegate && !long.TryParse(token, out _))
            throw new ParseException("not valid integer.");
        if (!isNegate && !ulong.TryParse(token, out _))
            throw new ParseException("not valid integer.");



        if (!isNegate)
        {
            if (byte.TryParse(token, out _))
                return new ByteLiteralExpressionSyntax(byte.Parse(token)).SetPos(pos, len);
            if (short.TryParse(token, out _))
                return new Int16LiteralExpressionSyntax(short.Parse(token)).SetPos(pos, len);
            if (ushort.TryParse(token, out _))
                return new UInt16LiteralExpressionSyntax(ushort.Parse(token)).SetPos(pos, len);
            if (int.TryParse(token, out _))
                return new Int32LiteralExpressionSyntax(int.Parse(token)).SetPos(pos, len);
            if (uint.TryParse(token, out _))
                return new UInt32LiteralExpressionSyntax(uint.Parse(token)).SetPos(pos, len);
            if (long.TryParse(token, out _))
                return new Int64LiteralExpressionSyntax(long.Parse(token)).SetPos(pos, len);
            if (ulong.TryParse(token, out _))
                return new UInt64LiteralExpressionSyntax(ulong.Parse(token)).SetPos(pos, len);
        }
        else
        {
            if (sbyte.TryParse(token, out _))
                return new SByteLiteralExpressionSyntax(sbyte.Parse(token)).SetPos(pos, len);
            if (short.TryParse(token, out _))
                return new Int16LiteralExpressionSyntax(short.Parse(token)).SetPos(pos, len);
            if (int.TryParse(token, out _))
                return new Int32LiteralExpressionSyntax(int.Parse(token)).SetPos(pos, len);
            if (long.TryParse(token, out _))
                return new Int64LiteralExpressionSyntax(long.Parse(token)).SetPos(pos, len);
        }

        throw new ParseException($"too big number '{token}'"); // TODO custom exception
    }
}
