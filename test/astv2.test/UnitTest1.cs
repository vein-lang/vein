namespace astv2.test;

using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;
using System.Globalization;
using System.Linq.Expressions;
using vein.ast;

public class Tests
{

    [Test]
    public void Test1()
    {
        var tokenizer = new TokenizerBuilder<TokenType>()
            .Ignore(Span.WhiteSpace)
            .Match(Character.EqualTo('+'), TokenType.Plus)
            .Match(Character.EqualTo('-'), TokenType.Minus)
            .Match(Character.EqualTo('*'), TokenType.Multiply)
            .Match(Character.EqualTo('/'), TokenType.Divide)
            .Match(Character.EqualTo('('), TokenType.OpenParen)
            .Match(Character.EqualTo(')'), TokenType.CloseParen)
            .Match(DecimalFloat, TokenType.Number_Float32)
            .Match(Numerics.IntegerInt32, TokenType.Number_Int32)
            .Match(Numerics.IntegerInt64, TokenType.Number_Int64)
            .Build();



        var result = ArithmeticExpressionParser.Ast.Parse(tokenizer.Tokenize("182 + 1 * 95 / 9102"));

    }

    public static TextParser<float> DecimalFloat { get; } = Numerics.Decimal.Select(span => float.Parse(span.ToStringValue(), CultureInfo.InvariantCulture));

}

class ArithmeticExpressionParser
{
    static readonly TokenListParser<TokenType, ExpressionType> Add =
        Token.EqualTo(TokenType.Plus).Value(ExpressionType.AddChecked);

    static readonly TokenListParser<TokenType, ExpressionType> Subtract =
        Token.EqualTo(TokenType.Minus).Value(ExpressionType.SubtractChecked);

    static readonly TokenListParser<TokenType, ExpressionType> Multiply =
        Token.EqualTo(TokenType.Multiply).Value(ExpressionType.MultiplyChecked);

    static readonly TokenListParser<TokenType, ExpressionType> Divide =
        Token.EqualTo(TokenType.Divide).Value(ExpressionType.Divide);

    static readonly TokenListParser<TokenType, ExpressionType> Modulo =
        Token.EqualTo(TokenType.Modulo).Value(ExpressionType.Modulo);

    static readonly TokenListParser<TokenType, ExpressionType> Power =
        Token.EqualTo(TokenType.Power).Value(ExpressionType.Power);

    static readonly TokenListParser<TokenType, Expression> Constant =
        Token.EqualTo(TokenType.Number_Int32)
            .Apply(Numerics.IntegerInt32)
            .Select(n => (Expression)Expression.Constant(n))
        .Or(Token.EqualTo(TokenType.Number_Float32)
            .Apply(Numerics.Decimal)
            .Select(f => (Expression)Expression.Constant(f)));


    static readonly TokenListParser<TokenType, Expression> Factor =
        (from lparen in Token.EqualTo(TokenType.OpenParen)
         from expr in Parse.Ref(() => Expr)
         from rparen in Token.EqualTo(TokenType.CloseParen)
         select expr)
        .Or(Constant);

    static readonly TokenListParser<TokenType, Expression> Operand =
        (from sign in Token.EqualTo(TokenType.Minus)
         from factor in Factor
         select (Expression)Expression.Negate(factor))
        .Or(Factor).Named("expression");

    static readonly TokenListParser<TokenType, Expression> Term =
        Parse.Chain(Multiply.Or(Divide).Or(Modulo).Or(Power), Operand, Expression.MakeBinary);

    static readonly TokenListParser<TokenType, Expression> Expr =
        Parse.Chain(Add.Or(Subtract), Term, Expression.MakeBinary);

    public static TokenListParser<TokenType, Expression<Func<T>>> Lambda<T>() where T : struct
        => Expr.AtEnd().Select(body => Expression.Lambda<Func<T>>(body));

    public static readonly TokenListParser<TokenType, Expression> Ast
        = Expr.AtEnd();
}
