namespace wave.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class StatementSyntax : ExpressionSyntax, IPositionAware<StatementSyntax>
    {
        public StatementSyntax() => Body = null;
        public StatementSyntax(string body) => Body = body;

        public override SyntaxType Kind => SyntaxType.Statement;

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Body);

        public string Body { get; set; }
        
        public new StatementSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}