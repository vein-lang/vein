namespace vein.syntax
{
    using Sprache;
    using stl;

    public partial class VeinSyntax
    {
        protected internal virtual Parser<StatementSyntax> Statement =>
            from statement in
                declarationStatement.OrPreview(embedded_statement)
                .Commented(this)
            select statement.Value
                .WithLeadingComments(statement.LeadingComments)
                .WithTrailingComments(statement.TrailingComments);

        protected internal virtual Parser<ExpressionSyntax> local_variable_initializer =>
            QualifiedExpression; // TODO array init

        protected internal virtual Parser<LocalVariableDeclaration> local_variable_declarator =>
            from id in IdentifierExpression
            from body in (
                from assign in Parse.Char('=').Token()
                from init in local_variable_initializer
                select init
            ).Optional()
            select new LocalVariableDeclaration(id, body);

        protected internal virtual Parser<LocalVariableDeclaration> local_variable_declaration =>
            from a in KeywordExpression("auto").Or(KeywordExpression("let"))
            from decl in local_variable_declarator.Positioned().Token()
            select decl;

        protected internal virtual Parser<StatementSyntax> declarationStatement =>
            from loc in local_variable_declaration.Token().Positioned()
            from s in Parse.Char(';').Token().Optional()
            select ValidateSemicolon(s, loc);

        protected internal virtual T ValidateSemicolon<T>(IOption<char> semi, T other) where T : BaseSyntax
        {
            if (semi.IsDefined)
                return other;
            var poz = other.Transform.pos;
            throw new VeinParseException("Semicolon required", poz, other);
        }

        protected internal virtual Parser<StatementSyntax> foreach_statement =>
            from k in KeywordExpression("foreach").Positioned().Token()
            from ob in Parse.Char('(').Token()
            from declaration in local_variable_declaration.Positioned().Token()
            from keyword in KeywordExpression("in").Positioned().Token()
            from exp in QualifiedExpression.Positioned().Token()
            from cb in Parse.Char(')').Token()
            from statement in embedded_statement.Token().Positioned()
            select new ForeachStatementSyntax(declaration, exp, statement);

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

        protected internal virtual Parser<StatementSyntax> embedded_statement =>
            Block.Or(simple_embedded_statement);

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


        /// <example>
        /// return exp;
        /// </example>
        protected internal virtual Parser<ReturnStatementSyntax> ReturnStatement =>
            from expression in KeywordExpressionStatement("return")
            select new ReturnStatementSyntax(expression.GetOrDefault());

        /// <example>
        /// fail new Exception(); fail;
        /// </example>
        protected internal virtual Parser<FailStatementSyntax> FailStatement =>
            from expression in KeywordExpressionStatement("fail")
            select new FailStatementSyntax(expression.GetOrDefault());

        /// <example>
        /// delete variable;
        /// </example>
        protected internal virtual Parser<DeleteStatementSyntax> DeleteStatement =>
            from expression in KeywordExpressionStatement("delete")
            select new DeleteStatementSyntax(expression.GetOrDefault());

        /// <example>
        /// while (foo) {}
        /// </example>
        protected internal virtual Parser<WhileStatementSyntax> WhileStatement =>
            from whileKeyword in Parse.IgnoreCase("while").Token()
            from expression in WrappedExpression('(', ')', QualifiedExpression)
            from loopBody in Statement
            select new WhileStatementSyntax(expression, loopBody);

        /// <example>
        /// if(exp) {}
        /// if(exp) {} else {}
        /// if(exp) {} else if(exp) {}
        /// </example>
        protected internal virtual Parser<IfStatementSyntax> IfStatement =>
            from ifKeyword in Keyword("if").Token()
            from expression in WrappedExpression('(', ')', QualifiedExpression)
            from thenBranch in Statement
            from elseBranch in Keyword("else").Token(this).Then(_ => Statement).Optional()
            select new IfStatementSyntax(expression, thenBranch, elseBranch.GetOrDefault());
    }
}
