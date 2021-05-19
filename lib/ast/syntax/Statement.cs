namespace mana.syntax
{
    using Sprache;
    using stl;

    public partial class ManaSyntax
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
            from a in KeywordExpression("auto")
            from decl in local_variable_declarator.Token()
            select decl;

        protected internal virtual Parser<StatementSyntax> declarationStatement =>
            local_variable_declaration.Then(x => Parse.Char(';').Token().Return(x));


        protected internal virtual Parser<StatementSyntax> embedded_statement =>
            Block.Or(simple_embedded_statement);

        protected internal virtual Parser<StatementSyntax> simple_embedded_statement => 
            Parse.Char(';').Token().Return((StatementSyntax)new EmptyStatementSyntax())
            .Or(QualifiedExpression.Then(x => Parse.Char(';').Token().Return(new QualifiedExpressionStatement(x))))
            .Or(IfStatement)
            .Or(WhileStatement)
            .Or(ReturnStatement)
            .Or(FailStatement)
            .Or(DeleteStatement);



        protected internal virtual Parser<ReturnStatementSyntax> ReturnStatement =>
            from expression in KeywordExpressionStatement("return")
            select new ReturnStatementSyntax
            {
                Expression = expression.GetOrDefault()
            };
        /// <example>
        /// fail new Exception(); fail;
        /// </example>
        protected internal virtual Parser<FailStatementSyntax> FailStatement =>
            from expression in KeywordExpressionStatement("fail")
            select new FailStatementSyntax
            {
                Expression = expression.GetOrDefault()
            };
        /// <example>
        /// delete variable;
        /// </example>
        protected internal virtual Parser<DeleteStatementSyntax> DeleteStatement =>
            from expression in KeywordExpressionStatement("delete")
            select new DeleteStatementSyntax
            {
                Expression = expression.GetOrDefault()
            };
        /// <example>
        /// while (foo) {}
        /// </example>
        protected internal virtual Parser<WhileStatementSyntax> WhileStatement =>
            from whileKeyword in Parse.IgnoreCase("while").Token()
            from expression in WrappedExpression('(', ')', QualifiedExpression)
            from loopBody in Statement
            select new WhileStatementSyntax
            {
                Expression = expression,
                Statement = loopBody,
            };
        
        protected internal virtual Parser<IfStatementSyntax> IfStatement =>
            from ifKeyword in Keyword("if").Token()
            from expression in WrappedExpression('(', ')', QualifiedExpression)
            from thenBranch in Statement
            from elseBranch in Keyword("else").Token(this).Then(_ => Statement).Optional()
            select new IfStatementSyntax
            {
                Expression = expression,
                ThenStatement = thenBranch,
                ElseStatement = elseBranch.GetOrDefault(),
            };
    }
}