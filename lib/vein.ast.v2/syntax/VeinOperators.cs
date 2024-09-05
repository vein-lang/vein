namespace vein.ast;

using Newtonsoft.Json.Converters;
using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

public enum TokenType
{
    Number_Int8,
    Number_Int16,
    Number_Int32,
    Number_Int64,
    Number_Float32,
    [Token(Category = "operator", Example = "+")]
    Plus,
    [Token(Category = "operator", Example = "-")]
    Minus,
    [Token(Category = "operator", Example = "*")]
    Multiply,
    [Token(Category = "operator", Example = "/")]
    Divide,
    [Token(Category = "operator", Example = "%")]
    Modulo,
    [Token(Category = "operator", Example = "^")]
    Power,
    [Token(Category = "operator", Example = "as")]
    As,
    [Token(Category = "operator", Example = "<")]
    LessThan,
    [Token(Category = "operator", Example = ">")]
    GreaterThan,
    OpenParen,
    CloseParen,
    Identifier,
    Comma
}

