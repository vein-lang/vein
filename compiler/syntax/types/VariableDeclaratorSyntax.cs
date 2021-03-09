namespace wave.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class VariableDeclaratorSyntax : BaseSyntax, IPositionAware<VariableDeclaratorSyntax>
    {
        public override SyntaxType Kind => SyntaxType.VariableDeclarator;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public string Identifier { get; set; }

        public ExpressionSyntax Expression { get; set; }
        
        public new VariableDeclaratorSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}