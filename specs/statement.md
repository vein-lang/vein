### ECMA Specification for Vein Language Statement Syntax

#### 1. Introduction

This section defines the rules and behaviors for parsing different types of statements in the Vein Language. Statements are fundamental constructs that represent executable code and can include variable declarations, control flow constructs, and expressions.

#### 2. Statement Definition

A statement in the Vein Language can be one of the following types:
- Declaration Statement
- Expression Statement
- Control Flow Statement (e.g., if, for, foreach, while)
- Block Statement
- Empty Statement

#### 3. Detailed Grammar

### Statement

The syntax for a statement is:
```
Statement ::= DeclarationStatement
             | EmbeddedStatement
```

##### Parser Implementation

The parser rule for `Statement` is structured as follows:

```csharp
protected internal virtual Parser<StatementSyntax> Statement =>
    from statement in
        declarationStatement.OrPreview(embedded_statement)
        .Commented(this)
    select statement.Value
        .WithLeadingComments(statement.LeadingComments)
        .WithTrailingComments(statement.TrailingComments);
```

### Declaration Statement

The syntax for a declaration statement involves declaring local variables:
```
DeclarationStatement ::= LocalVariableDeclaration ';'
```

##### Parser Implementation

The parser rule for `declarationStatement` is structured as follows:

```csharp
protected internal virtual Parser<StatementSyntax> declarationStatement =>
    from loc in local_variable_declaration.Token().Positioned()
    from s in Parse.Char(';').Token().Optional()
    select ValidateSemicolon(s, loc);
```

### Local Variable Declaration

The syntax for a local variable declaration is:
```
LocalVariableDeclaration ::= 'auto' LocalVariableDeclarator
                            | 'let' LocalVariableDeclarator
LocalVariableDeclarator ::= Identifier ('=' LocalVariableInitializer)?
```

##### Parser Implementation

The parser rule for `local_variable_declaration` is structured as follows:

```csharp
protected internal virtual Parser<LocalVariableDeclaration> local_variable_declaration =>
    from a in KeywordExpression("auto").Or(KeywordExpression("let"))
    from decl in local_variable_declarator.Positioned().Token()
    select decl;
```

### Embedded Statement

The syntax for an embedded statement can be a block, an expression, or various control flow statements:
```
EmbeddedStatement ::= Block
                     | SimpleEmbeddedStatement
```

##### Parser Implementation

The parser rule for `embedded_statement` is structured as follows:

```csharp
protected internal virtual Parser<StatementSyntax> embedded_statement =>
    Block.Or(simple_embedded_statement);
```

### Simple Embedded Statement

The syntax for a simple embedded statement is:
```
SimpleEmbeddedStatement ::= ';'
                           | QualifiedExpression ';'
                           | ControlFlowStatement
```

##### Parser Implementation

The parser rule for `simple_embedded_statement` is structured as follows:

```csharp
protected internal virtual Parser<StatementSyntax> simple_embedded_statement =>
    Parse.Char(';').Token().Return((StatementSyntax)new EmptyStatementSyntax())
    .Or(QualifiedExpression.Then(x => Parse.Char(';').Token().Return(new QualifiedExpressionStatement(x))).Positioned())
    .Or(IfStatement.Positioned())
    .Or(WhileStatement.Positioned())
    .Or(TryStatement.Positioned())
    .Or(ReturnStatement.Positioned())
    .Or(for_statement.Positioned())
    .Or(foreach_statement.Positioned())
    .Or(FailStatement.Positioned())
    .Or(DeleteStatement.Positioned());
```

### Control Flow Statements

Examples of control flow statements include `if`, `for`, `foreach`, `while`, and `return`.

#### If Statement

The syntax for an if statement is:
```
IfStatement ::= 'if' '(' QualifiedExpression ')' Statement ('else' Statement)?
```

##### Parser Implementation

The parser rule for `IfStatement` is structured as follows:

```csharp
protected internal virtual Parser<IfStatementSyntax> IfStatement =>
    from ifKeyword in Keyword("if").Token()
    from expression in WrappedExpression('(', ')', QualifiedExpression)
    from thenBranch in Statement
    from elseBranch in Keyword("else").Token(this).Then(_ => Statement).Optional()
    select new IfStatementSyntax(expression, thenBranch, elseBranch.GetOrDefault());
```

#### While Statement

The syntax for a while statement is:
```
WhileStatement ::= 'while' '(' QualifiedExpression ')' Statement
```

##### Parser Implementation

The parser rule for `WhileStatement` is structured as follows:

```csharp
protected internal virtual Parser<WhileStatementSyntax> WhileStatement =>
    from whileKeyword in Parse.IgnoreCase("while").Token()
    from expression in WrappedExpression('(', ')', QualifiedExpression)
    from loopBody in Statement
    select new WhileStatementSyntax(expression, loopBody);
```

#### For Statement

The syntax for a for statement is:
```
ForStatement ::= 'for' '(' LocalVariableDeclaration? ';' QualifiedExpression? ';' QualifiedExpression? ')' Statement
```

##### Parser Implementation

The parser rule for `for_statement` is structured as follows:

```csharp
protected internal virtual Parser<StatementSyntax> for_statement =>
    from k in KeywordExpression("for").Positioned().Token()
    from ob in Parse.Char('(').Token()
    from loopVariable in local_variable_declaration.Positioned().Token().Optional()
    from d1 in Parse.Char(';').Token()
    from exp in QualifiedExpression.Positioned().Token().Optional()
    from d2 in Parse.Char(';').Token()
    from loopCounter in QualifiedExpression.Positioned().Token().Optional()
    from cb in Parse.Char(')').Token()
    from statement in embedded_statement.Token().Positioned()
    select new ForStatementSyntax(loopVariable, exp, loopCounter, statement);
```

#### Foreach Statement

The syntax for a foreach statement is:
```
ForeachStatement ::= 'foreach' '(' LocalVariableDeclaration 'in' QualifiedExpression ')' Statement
```

##### Parser Implementation

The parser rule for `foreach_statement` is structured as follows:

```csharp
protected internal virtual Parser<StatementSyntax> foreach_statement =>
    from k in KeywordExpression("foreach").Positioned().Token()
    from ob in Parse.Char('(').Token()
    from declaration in local_variable_declaration.Positioned().Token()
    from keyword in KeywordExpression("in").Positioned().Token()
    from exp in QualifiedExpression.Positioned().Token()
    from cb in Parse.Char(')').Token()
    from statement in embedded_statement.Token().Positioned()
    select new ForeachStatementSyntax(declaration, exp, statement);
```

#### Return Statement

The syntax for a return statement is:
```
ReturnStatement ::= 'return' QualifiedExpression? ';'
```

##### Parser Implementation

The parser rule for `ReturnStatement` is structured as follows:

```csharp
protected internal virtual Parser<ReturnStatementSyntax> ReturnStatement =>
    from expression in KeywordExpressionStatement("return")
    select new ReturnStatementSyntax(expression.GetOrDefault());
```

#### Fail Statement

The syntax for a fail statement is:
```
FailStatement ::= 'fail' QualifiedExpression? ';'
```

##### Parser Implementation

The parser rule for `FailStatement` is structured as follows:

```csharp
protected internal virtual Parser<FailStatementSyntax> FailStatement =>
    from expression in KeywordExpressionStatement("fail")
    select new FailStatementSyntax(expression.GetOrDefault());
```

#### Delete Statement

The syntax for a delete statement is:
```
DeleteStatement ::= 'delete' QualifiedExpression? ';'
```

##### Parser Implementation

The parser rule for `DeleteStatement` is structured as follows:

```csharp
protected internal virtual Parser<DeleteStatementSyntax> DeleteStatement =>
    from expression in KeywordExpressionStatement("delete")
    select new DeleteStatementSyntax(expression.GetOrDefault());
```

### Validation for Semicolon

Ensures that semicolon is present where required:

```csharp
protected internal virtual T ValidateSemicolon<T>(IOption<char> semi, T other) where T : BaseSyntax
{
    if (semi.IsDefined)
        return other;
    var poz = other.Transform.pos;
    throw new VeinParseException("Semicolon required", poz, other);
}
```

### Keyword Expression Statement

To handle statements that begin with a keyword and might end with an optional expression followed by a semicolon:

```csharp
protected internal virtual Parser<IOption<ExpressionSyntax>> KeywordExpressionStatement(string keyword) =>
    KeywordExpression(keyword)
        .Token()
        .Then(_ => QualifiedExpression.Token().XOptional())
        .Then(x => Parse.Char(';').Token().Return(x));
```

### General Parsing Helper Methods

Include helper methods like `WrappedExpression` for wrapping expressions within certain delimiters and chaining/parsing various syntactical constructs.

#### Conclusion

This specification outlines the rules and behaviors for parsing different statements in the Vein Language. It ensures a structured and detailed approach to handle the diverse range of statements, including declarations, expressions, and control flow constructs, enhancing the consistency and functionality of the language parsing process.