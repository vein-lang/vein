namespace vein.ast.v2.syntax
{
    using System;
    using System.Linq.Expressions;
    using Superpower;
    using Superpower.Display;
    using Superpower.Parsers;

    public partial class ManaSyntax
    {
    }


    public enum ManaTokenKind
    {
        [Token(Example = "{")]
        LBracket,
        [Token(Example = "}")]
        RBracket,
        [Token(Example = "[")]
        LSquareBracket,
        [Token(Example = "]")]
        RSquareBracket,
        [Token(Example = ":")]
        Colon,
        [Token(Example = ",")]
        Comma,
        [Token(Category="operator", Example = "+")]
        Add,
        [Token(Category = "operator", Example = "-")]
        Sub,
        [Token(Category = "operator", Example = "*")]
        Mul,
        [Token(Category = "operator", Example = "/")]
        Div,
        [Token(Category = "operator", Example = "^^")]
        Pow,
    }
}
