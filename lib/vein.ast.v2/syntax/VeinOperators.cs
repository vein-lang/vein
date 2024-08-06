namespace vein.ast.v2.syntax;

using Newtonsoft.Json.Converters;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;

public class VeinOperators
{
    
}

public abstract record BaseSyntax : ITransform
{
    public abstract SyntaxType Kind { get; }

    public TextSpan Transform { get; set; }
    public TextSpan TransformUntil { get; set; }
}

public abstract record ExpressionSyntax : BaseSyntax
{
    public string ExpressionValue { get; protected set; }
}

public record IdentifierExpression : ExpressionSyntax
{
    public IdentifierExpression(string exp) => ExpressionValue = exp;

    public override SyntaxType Kind => SyntaxType.IDENTIFIER;
}

public enum SyntaxType
{
    IDENTIFIER
}

public class VeinParseBase
{
    public TextParser<string> RawIdentifier =>
        from first in Character.Letter.Or(Character.EqualTo('_').Or(Character.EqualTo('@')))
        from rest in Character.LetterOrDigit.Or(Character.EqualTo('_').Or(Character.EqualTo('@'))).Many()
        select first + new string(rest);

    public TextParser<IdentifierExpression> IdentifierExpression =>
        RawIdentifier.Named("Identifier")
            .Select(x => new IdentifierExpression(x))
            .Message("required valid identifier expression")
            .WithLocation();

}

public record VeinKeyword(string name);


public abstract class NumericLiteralExpressionSyntax(string value)
{
    public string Value { get; } = value;
}

public class SByteValueExpressionSyntax(string value) : NumericLiteralExpressionSyntax(value);

public class ByteValueExpressionSyntax(string value) : NumericLiteralExpressionSyntax(value);

public class ShortValueExpressionSyntax(string value) : NumericLiteralExpressionSyntax(value);

public class UShortValueExpressionSyntax(string value) : NumericLiteralExpressionSyntax(value);

public class Int32ValueExpressionSyntax(string value) : NumericLiteralExpressionSyntax(value);

public class UInt32ValueExpressionSyntax(string value) : NumericLiteralExpressionSyntax(value);

public abstract class ExpressionSyntax2
{
    public abstract int Evaluate();
}

public class LiteralExpressionSyntax : ExpressionSyntax2
{
    public NumericLiteralExpressionSyntax NumericLiteral { get; }

    public LiteralExpressionSyntax(NumericLiteralExpressionSyntax numericLiteral)
    {
        NumericLiteral = numericLiteral;
    }

    public override int Evaluate()
    {
        return int.Parse(NumericLiteral.Value);
    }
}

public class BinaryExpressionSyntax(ExpressionSyntax2 left, char @operator, ExpressionSyntax2 right)
    : ExpressionSyntax2
{
    public ExpressionSyntax2 Left { get; } = left;
    public ExpressionSyntax2 Right { get; } = right;
    public char Operator { get; } = @operator;

    public override int Evaluate()
    {
        var leftValue = Left.Evaluate();
        var rightValue = Right.Evaluate();

        return Operator switch
        {
            '+' => leftValue + rightValue,
            '-' => leftValue - rightValue,
            '*' => leftValue * rightValue,
            '/' => leftValue / rightValue,
            _ => throw new InvalidOperationException("Unknown operator")
        };
    }
}


public static class NumericParsers
{
    public static readonly TextParser<string> NumberParser =
        from digits in Character.Digit.AtLeastOnce()
        select new string(digits);

    public static readonly TextParser<string> SuffixParser =
        from suffix in Character.EqualToIgnoreCase('u').OptionalOrDefault()
        select suffix.ToString();

    public static readonly TextParser<NumericLiteralExpressionSyntax> NumericLiteralExpression =
        from number in NumberParser
        from suffix in SuffixParser
        select DetermineType(number, suffix);

    public static NumericLiteralExpressionSyntax DetermineType(string number, string suffix)
    {
        if (suffix == "u")
        {
            uint parsedValue = uint.Parse(number);
            if (parsedValue <= byte.MaxValue)
                return new ByteValueExpressionSyntax(number);
            else if (parsedValue <= ushort.MaxValue)
                return new UShortValueExpressionSyntax(number);
            else if (parsedValue <= uint.MaxValue)
                return new UInt32ValueExpressionSyntax(number);
        }
        else
        {
            int parsedValue = int.Parse(number);
            if (parsedValue is >= sbyte.MinValue and <= sbyte.MaxValue)
                return new SByteValueExpressionSyntax(number);
            else if (parsedValue is >= short.MinValue and <= short.MaxValue)
                return new ShortValueExpressionSyntax(number);
            else if (parsedValue is >= int.MinValue and <= int.MaxValue)
                return new Int32ValueExpressionSyntax(number);
        }

        throw new InvalidOperationException("Unsupported numeric range or suffix.");
    }

    public static readonly TextParser<ExpressionSyntax2> Literal =
        NumericLiteralExpression.Select(numericLiteral => new LiteralExpressionSyntax(numericLiteral) as ExpressionSyntax2);

    public static readonly TextParser<ExpressionSyntax2> Parenthesized =
        from lparen in Character.EqualTo('(')
        from expr in Parse.Ref(() => Expression)
        from rparen in Character.EqualTo(')')
        select expr;

    public static readonly TextParser<ExpressionSyntax2> Factor =
        Parenthesized.Or(Literal);

    public static readonly TextParser<ExpressionSyntax2> Term =
        Parse.Chain(Character.In('*', '/'),
                    Factor,
                    (op, left, right) => new BinaryExpressionSyntax(left, op, right));

    public static readonly TextParser<ExpressionSyntax2> Expression =
        Parse.Chain(Character.In('+', '-'),
                    Term,
                    (op, left, right) => new BinaryExpressionSyntax(left, op, right));

}
