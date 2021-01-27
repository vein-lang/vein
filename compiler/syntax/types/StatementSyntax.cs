namespace wave.syntax
{
    using System.Collections.Generic;

    public class StatementSyntax : BaseSyntax
    {
        public StatementSyntax(string body = null) => Body = body;

        public override SyntaxType Kind => SyntaxType.Statement;

        public override void Accept(WaveSyntaxVisitor visitor) => visitor.VisitStatement(this);

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Body);

        public string Body { get; set; }
    }
}