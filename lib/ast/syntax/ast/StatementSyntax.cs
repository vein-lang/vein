namespace insomnia.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class StatementSyntax : ExpressionSyntax, IPositionAware<StatementSyntax>
    {
        public StatementSyntax() {}

        public override SyntaxType Kind => SyntaxType.Statement;

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;
        
        public new StatementSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}