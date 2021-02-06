namespace wave.syntax
{
    using System.Collections.Generic;

    public class VariableDeclaratorSyntax : BaseSyntax
    {
        public override SyntaxType Kind => SyntaxType.VariableDeclarator;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public string Identifier { get; set; }

        public ExpressionSyntax Expression { get; set; }
    }
}