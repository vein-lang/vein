### ECMA Specification for Vein Language Literal Expressions

#### 1. Introduction

This section defines the behavior for parsing various literal expressions in the Vein Language, including float literals, integer literals, boolean literals, null literals, and more. Literal expressions represent constant values directly embedded into the code.

#### 2. General Grammar

A literal expression in the Vein Language could be one of the following:

- Float literal
- Integer literal
- Boolean literal
- Null literal
- String literal
- Binary literal

#### 3. Detailed Grammar

##### Float Literal Expression

The syntax for a float literal is:
```
FloatLiteral ::= NumberPart '.' FractionPart ExponentPart? FloatTypeSuffix?
NumberPart ::= Digit (('_')*  Digit)*
FractionPart ::= Digit (('_')*  Digit)*
Digit ::= '0' | '1' | ... | '9'
ExponentPart ::= ('e' | 'E') ('+' | '-')? Digit (('_')*  Digit)*
FloatTypeSuffix ::= 'f' | 'F' | 'd' | 'D' | 'm' | 'M' | 'h' | 'H'
```

##### Parser Implementation

The parser rule for `FloatLiteralExpression` is structured as follows:

```csharp
// [eE] ('+' | '-')? [0-9] ('_'* [0-9])*;
internal Parser<string> ExponentPart =>
    Parse.Chars("eE").Then(x =>
        Parse.Chars("+-").Optional().Then(polarity =>
            Parse.Number.Then(n => Parse.Char('_').Many().Then(_ => Parse.Number).Many()
                .Select(w => $"{x}{polarity.GetOrDefault()}{n}{w.Join().Replace("_", "")}"
                    .Replace($"{default(char)}", "")))));

// [0-9] ('_'* [0-9])*
private Parser<string> NumberChainBlock =>
    from number in Parse.Number
    from chain in Parse.Char('_').Many().Then(_ => Parse.Number).Many().Select(x => x.Join())
    select $"{number}{chain.Replace("_", "")}";

protected internal virtual Parser<NumericLiteralExpressionSyntax> FloatLiteralExpression =>
    (from f1block in NumberChainBlock
     from dot in Parse.Char('.')
     from f2block in NumberChainBlock.AtLeastOnce()
     from exp in ExponentPart.Optional()
     from suffix in FloatTypeSuffix.Optional()
     select FromFloat($"{f1block}.{f2block.Join()}{exp.GetOrElse("")}", suffix.GetOrDefault())).Or(
        from block in NumberChainBlock
        from other in FloatTypeSuffix.Select(x => ("", x)).Or(ExponentPart.Then(x =>
            FloatTypeSuffix.Optional().Select(z => (x, z.GetOrDefault()))))
        select FromFloat($"{block}{other.Item1}", other.Item2)
    );
```

### `FloatTypeSuffix` Parsing

The parser rule for `FloatTypeSuffix` identifies optional suffixes for floating-point numbers:

```csharp
private Parser<NumericSuffix> FloatTypeSuffix =>
    Parse.Chars("FfDdMmHh").Select(char.ToLowerInvariant).Select(x => x switch
    {
        'f' => NumericSuffix.Float,
        'd' => NumericSuffix.Double,
        'm' => NumericSuffix.Decimal,
        'h' => NumericSuffix.Half,
        _ => NumericSuffix.None
    });
```

### `FromFloat` Function

The function `FromFloat` is used to convert the float literal into an appropriate `NumericLiteralExpressionSyntax` object:

```csharp
private NumericLiteralExpressionSyntax FromFloat(string value, NumericSuffix suffix)
{
    // Conversion logic here based on the value and the suffix.
}
```

##### Integer Literal Expression

The syntax for an integer literal is:
```
IntLiteral ::= NumberPart IntegerTypeSuffix?
NumberPart ::= Digit (('_')* Digit)*
Digit ::= '0' | '1' | ... | '9'
```

##### Parser Implementation

The parser rule for `IntLiteralExpression` is structured as follows:

```csharp
protected internal virtual Parser<LiteralExpressionSyntax> IntLiteralExpression =>
    from number in NumberChainBlock
    from suffix in IntegerTypeSuffix.Optional()
    select FromDefault(number, suffix.GetOrDefault());
```

### `FromDefault` Function

The function `FromDefault` is used to convert the integer literal into an appropriate `LiteralExpressionSyntax` object:

```csharp
private LiteralExpressionSyntax FromDefault(string value, NumericSuffix suffix)
{
    // Conversion logic here based on the value and the suffix.
}
```

##### Boolean Literal Expression

The syntax for a boolean literal is:
```
BooleanLiteral ::= 'true' | 'false'
```

##### Parser Implementation

The parser rule for `BooleanLiteralExpression` is structured as follows:

```csharp
protected internal virtual Parser<LiteralExpressionSyntax> BooleanLiteralExpression =>
    from token in Keyword("false").Or(Keyword("true"))
    select new BoolLiteralExpressionSyntax(token);
```

##### Null Literal Expression

The syntax for a null literal is:
```
NullLiteral ::= 'null'
```

##### Parser Implementation

The parser rule for `NullLiteralExpression` is structured as follows:

```csharp
protected internal virtual Parser<LiteralExpressionSyntax> NullLiteralExpression =>
    from token in Keyword("null")
    select new NullLiteralExpressionSyntax();
```

##### Literal Expression

The overall literal expression combines various literal types:

```csharp
protected internal virtual Parser<LiteralExpressionSyntax> LiteralExpression =>
    from expr in
        FloatLiteralExpression.Log("FloatLiteralExpression").Or(
                IntLiteralExpression.Log("IntLiteralExpression")).XOr(
            StringLiteralExpression.Log("StringLiteralExpression")).XOr(
            BinaryLiteralExpression.Log("BinaryLiteralExpression")).XOr(
            BooleanLiteralExpression.Log("BooleanLiteralExpression")).XOr(
            NullLiteralExpression.Log("NullLiteralExpression"))
            .Positioned().Commented(this)
    select expr.Value
        .WithLeadingComments(expr.LeadingComments)
        .WithTrailingComments(expr.TrailingComments);
```

### Logging and Position Management

Each sub-parser includes logging, positioning, and comment handling to ensure accurate and informative parsing:

- **Logging**: Provides traceability during the parsing process to understand parsing decisions.
- **Position Management**: Tracks the position of literals in the source code for error reporting and debugging.
- **Comment Handling**: Incorporates leading and trailing comments with literal expressions to maintain context.

#### 5. Examples

Example literal expressions:
```vein
// Float Literals
1.23d                 // Double precision floating point
123.456f              // Single precision floating point
789_123.456_789m      // Decimal floating point with underscores for readability

// Integer Literals
1124                  // Decimal integer
111_241               // Integer with underscores for readability

// Boolean Literals
true
false

// Null Literal
null

// String Literals (not explicitly shown above but included for completeness)
"Hello, world!"
"Line1\nLine2"

// Binary Literals
0b1010                   // Binary integer
0B1101_0010              // Binary integer with underscores for readability
```

#### 6. Conclusion

This specification details the rules and behaviors for parsing various types of literal expressions in the Vein Language. By following these guidelines, the Vein parser ensures the accurate and efficient interpretation of literals essential for the language's functionality. This helps in building reliable and maintainable Vein language applications.