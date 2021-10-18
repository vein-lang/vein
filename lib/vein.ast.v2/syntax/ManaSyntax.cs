namespace vein.ast.v2.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Superpower;
    using Superpower.Display;
    using Superpower.Parsers;

    public partial class ManaSyntax
    {
        public void X()
        {
            
        }

        protected internal TextParser<string> RawIdentifier =
            from first in Character.Letter
            from rest in Character.LetterOrDigit.Or(Character.EqualTo('_')).Many()
            where !ManaKeywords.list.Contains(first + new string(rest))
            select first + new string(rest);

        /*
         * internal virtual Parser<string> RawIdentifier =>
            from identifier in Parse.Identifier(Parse.Letter.Or(Parse.Chars("_@")), Parse.LetterOrDigit.Or(Parse.Char('_')))
            where !ManaKeywords.list.Contains(identifier)
            select identifier;
         */
    }

    public static class ManaKeywords
    {
        public static readonly List<string> list = new()
        {
            "public",
            "private",
            "protected",
            "internal",
            "static",
            "readonly",
            "operation",
            "class",
            "gc",
            "auto",
            "fail",
            "sync",
            "nocontrol",
            "return",
            "body",
            "union",
            "struct",
            "extensions",
            "where",
            "when",
            "as",
            "is",
            "new",
            "const",
            "global",
            "if",
            "else",
            "while",
            "do",
            "for",
            "foreach",
            "extern"
        };
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
