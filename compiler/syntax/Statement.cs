namespace wave.syntax
{
    using Sprache;
    using stl;

    public partial class WaveSyntax
    {
        protected internal virtual Parser<StatementSyntax> Statement =>
            from statement in Block.Select(s => s as StatementSyntax)
                //.Or(IfStatement)
                //.Or(DoStatement)
                //.Or(ForEachStatement)
                //.Or(ForStatement)
                //.Or(WhileStatement)
                //.Or(BreakStatement)
                //.Or(ContinueStatement)
                //.Or(TryCatchFinallyStatement)
                .Or(ReturnStatement)
                .Or(FailStatement)
                .Or(DeleteStatement)
                .Or(VariableDeclaration)
                //.Or(SwitchStatement)
                //.Or(UnknownGenericStatement)
                .Commented(this)
            select statement.Value
                .WithLeadingComments(statement.LeadingComments)
                .WithTrailingComments(statement.TrailingComments);
        
        protected internal virtual Parser<ReturnStatementSyntax> ReturnStatement =>
            from expression in KeywordExpressionStatement("return")
            select new ReturnStatementSyntax
            {
                Expression = expression == null ? null : new ExpressionSyntax(expression),
            };
        /// <example>
        /// fail new Exception(); fail;
        /// </example>
        protected internal virtual Parser<FailStatementSyntax> FailStatement =>
            from expression in KeywordExpressionStatement("fail")
            select new FailStatementSyntax
            {
                Expression = expression == null ? null : new ExpressionSyntax(expression),
            };
        /// <example>
        /// delete variable;
        /// </example>
        protected internal virtual Parser<DeleteStatementSyntax> DeleteStatement =>
            from expression in KeywordExpressionStatement("delete")
            where !string.IsNullOrWhiteSpace(expression)
            select new DeleteStatementSyntax
            {
                Expression = new ExpressionSyntax(expression),
            };
        /// <example>
        /// while (foo) {}
        /// </example>
        protected internal virtual Parser<WhileStatementSyntax> WhileStatement =>
            from whileKeyword in Parse.IgnoreCase("while").Token()
            from expression in GenericExpressionInBraces()
            from loopBody in Statement
            select new WhileStatementSyntax
            {
                Expression = new ExpressionSyntax(expression),
                Statement = loopBody,
            };
        
        protected internal virtual Parser<IfStatementSyntax> IfStatement =>
            from ifKeyword in Keyword("if").Token()
            from expression in GenericExpressionInBraces()
            from thenBranch in Statement
            from elseBranch in Keyword("else").Token(this).Then(_ => Statement).Optional()
            select new IfStatementSyntax
            {
                Expression = new ExpressionSyntax(expression),
                ThenStatement = thenBranch,
                ElseStatement = elseBranch.GetOrDefault(),
            };
    }
}