### ECMA Specification for Vein Language Expressions and Blocks

#### 1. Introduction

This section defines the behaviors and rules for parsing various expressions and blocks in the Vein Language, including block statements, qualified expressions, assignment expressions, literal expressions, and more.

#### 2. Block Statements

Blocks are fundamental structural elements that contain a sequence of statements enclosed within braces (`{}`).

##### Block Statement

The syntax for a block statement is:
```
Block ::= '{' Statements '}'
Statements ::= Statement*
```

##### Parser Implementation

The parser rule for `Block` is structured as follows:

```csharp
protected internal virtual Parser<BlockSyntax> Block =>
    from comments in CommentParser.AnyComment.Token().Many()
    from openBrace in Parse.Char('{').Token().Commented(this)
    from statements in Statement.Many()
    from closeBrace in Parse.Char('}').Commented(this)
    select new BlockSyntax
    {
        LeadingComments = comments.ToList(),
        Statements = statements.ToList(),
        InnerComments = closeBrace.LeadingComments.ToList(),
        TrailingComments = closeBrace.TrailingComments.ToList(),
    }
    .SetStart(openBrace.Transform.pos)
    .SetEnd(closeBrace.Transform.pos)
    .As<BlockSyntax>();
```

### Shortform Block Statement

The syntax for a shortform block is:
```
BlockShortform ::= '|>' Expression ';'
```

##### Parser Implementation

The parser rule for `BlockShortform` is structured as follows:

```csharp
protected internal virtual Parser<BlockSyntax> BlockShortform<T>() where T : StatementSyntax =>
    from comments in CommentParser.AnyComment.Token().Many()
    from op in Parse.String("|>").Token()
    from exp in QualifiedExpression.Token().Positioned()
    from end in Parse.Char(';').Token()
    select new BlockSyntax
    {
        LeadingComments = comments.ToList(),
        Statements = new List<StatementSyntax>()
        {
            typeof(T) == typeof(ReturnStatementSyntax) ?
                new ReturnStatementSyntax(exp).SetPos<ReturnStatementSyntax>(exp.Transform) :
                new SingleStatementSyntax(exp)
        }
    }
    .SetStart(exp.Transform.pos)
    .SetEnd(exp.Transform.pos)
    .As<BlockSyntax>();
```

#### 3. Qualified Expressions

Qualified expressions include various types such as assignment, conditional, and lambda expressions.

##### Qualified Expression

The syntax for a qualified expression is:
```
QualifiedExpression ::= AssignmentExpression
                      | NonAssignmentExpression
```

##### Parser Implementation

The parser rule for `QualifiedExpression` is structured as follows:

```csharp
protected internal virtual Parser<ExpressionSyntax> _shadow_QualifiedExpression =>
    assignment.Or(non_assignment_expression);

protected internal virtual Parser<ExpressionSyntax> QualifiedExpression =>
    Parse.Ref(() => _shadow_QualifiedExpression);
```

### Assignment Expression

The syntax for an assignment expression is:
```
AssignmentExpression ::= UnaryExpression AssignmentOperator QualifiedExpression
                       | UnaryExpression '??=' FailableExpression
```

##### Parser Implementation

The parser rule for `assignment` is structured as follows:

```csharp
protected internal virtual Parser<ExpressionSyntax> assignment =>
    (
        from exp in unary_expression
        from op in assignment_operator
        from exp2 in QualifiedExpression
        select new BinaryExpressionSyntax(exp, exp2, op)
    )
    .Or(
        from exp in unary_expression
        from op in Parse.String("??=").Text().Token()
        from exp2 in failable_expression
        select new BinaryExpressionSyntax(exp, exp2, op)
    );
```

### Assignment Operators

The syntax for assignment operators is:
```
AssignmentOperator ::= '<<=' | '^=' | '|=' | '&=' | '%=' | '/=' | '*=' | '-=' | '+=' | '='
```

##### Parser Implementation

The parser rule for `assignment_operator` is structured as follows:

```csharp
protected internal virtual Parser<string> assignment_operator =>
    Parse.String("<<=")
        .Or(Parse.String("^="))
        .Or(Parse.String("|="))
        .Or(Parse.String("&="))
        .Or(Parse.String("%="))
        .Or(Parse.String("/="))
        .Or(Parse.String("*="))
        .Or(Parse.String("-="))
        .Or(Parse.String("+="))
        .Or(Parse.String("=")).Token().Text();
```

### Warning         
Abount all spec, in general,        
I understand perfectly well that no one reads it,       
but if you are a reader who has read up to this point, then I am very surprised -       
I will say right away, this is a nominal spec, it is unlikely that I will follow it, plus it is written using  chatgps, chatjpt, chathpt ☺️         
###     

### Non-Assignment Expression

The syntax for a non-assignment expression covers a wide variety of expression types, but fundamentally, it could be either a conditional expression or a lambda expression.

##### Parser Implementation

The parser rule for `non_assignment_expression` is structured as follows:

```csharp
protected internal virtual Parser<ExpressionSyntax> non_assignment_expression =>
    conditional_expression.Or(lambda_expression);
```

### Lambda Expression

The syntax for a lambda expression is:
```
LambdaExpression ::= LFunctionSignature '|>' LFunctionBody
```

##### Parser Implementation

The parser rule for `lambda_expression` is structured as follows:

```csharp
protected internal virtual Parser<ExpressionSyntax> lambda_expression =>
    from dec in lfunction_signature
    from op in Parse.String("|>").Token()
    from body in lfunction_body
    select new AnonymousFunctionExpressionSyntax(dec, body);
```

### LFunction Signature and Body

The parts defining a lambda function include signatures and bodies:

##### Parser Implementations

The parser rules for LFunction signature and body are structured as follows:

```csharp
protected internal virtual Parser<ExpressionSyntax> lfunction_signature =>
    WrappedExpression('(', ')')
        .Or(WrappedExpression('(', ')', explicit_anonymous_function_parameter_list).Select(x => new AnonFunctionSignatureExpression(x)))
        .Or(WrappedExpression('(', ')', implicit_anonymous_function_parameter_list).Select(x => new AnonFunctionSignatureExpression(x)))
        .Or(IdentifierExpression);

protected internal virtual Parser<ExpressionSyntax> lfunction_body =>
    failable_expression.Or(block.Select(x => x.GetOrElse(new BlockSyntax())));
```

### Binary Expression

The syntax for a binary expression is:
```
BinaryExpression ::= Operand Operator Operand
Operator ::= '+', '-', '*', '/', '%', '||', '&&', ...

BinaryOperator ::= AdditiveOperator
                 | MultiplicativeOperator
                 | ConditionalOperator
                 ...
```

##### Parser Implementation

The parser rules for `BinaryExpression` are structured to use recursive patterns:

```csharp
private Parser<ExpressionSyntax> BinaryExpression<T>(Parser<T> t, string op) where T : ExpressionSyntax, IPositionAware<ExpressionSyntax> =>
    from c in t.Token()
    from data in
        (from _op in Parse.String(op).Text().Token()
         from a in t.Token()
         select (_op, a)).Many()
    select FlatIfEmptyOrNull(c, data.EmptyIfNull().ToArray());

private Parser<ExpressionSyntax> BinaryExpression<T>(Parser<T> t, params string[] ops) where T : ExpressionSyntax, IPositionAware<ExpressionSyntax> =>
    from c in t.Token()
    from data in
        (from _op in Parse.Regex(ops.Select(x => $"\\{x}").Join("|"), $"operators '{ops.Join(',')}'")
         from a in t.Token()
         select (_op, a)).Many()
    select FlatIfEmptyOrNull(c, data.EmptyIfNull().ToArray());
```

### Range Expression

The syntax for a range expression is:
```
RangeExpression ::= Operand '..' Operand
```

##### Parser Implementation

The parser rule for `range_expression` is structured as follows:

```csharp
protected internal virtual Parser<ExpressionSyntax> range_expression =>
    (
        from s1 in unary_expression
        from op in Parse.String("..").Token()
        from s2 in unary_expression
        select new RangeExpressionSyntax(s1, s2)
    ).Or(unary_expression);
```

### Conditional Expression

The syntax for a conditional expression can include multiple clauses:

##### Parser Implementation

The parser rule for `conditional_expression` is structured as follows:

```csharp
protected internal virtual Parser<ExpressionSyntax> conditional_expression =>
    from operand in null_coalescing_expression
    from d in Parse.Char('?')
        .Token()
        .Then(_ => failable_expression
            .Then(x => Parse
                .Char(':')
                .Token()
                .Then(_ => failable_expression
                    .Select(z => (x, z)))))
        .Token()
        .Optional()
    select FlatIfEmptyOrNull(operand, new CoalescingExpressionSyntax(d));
```

### Inclusion of Other Expressions

The Parsing rules make use of combining multiple types using `Or` and `XOr`.

##### Parser Implementation

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

##### Literal Expressions

The syntax covers literals of various types like int, float, boolean, null, etc.

#### Conclusion

This specification outlines the rules and behaviors for parsing blocks and various expressions in the Vein Language, ensuring a structured approach to handle both simple and complex expressions. The provided parser implementations use a combination of `Or`, `XOr`, position tracking, and comments to effectively manage and identify different constructs within the Vein Language. This enhances the ability for maintaining consistency and robustness within the language features.