namespace wave.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;

    public class IndexerExpression : ExpressionSyntax, IPositionAware<IndexerExpression>
    {
        private readonly ExpressionSyntax[] _exps;

        public IndexerExpression(IEnumerable<ExpressionSyntax> exps) => _exps = exps.ToArray();

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(_exps.OfExactType<BaseSyntax>().ToArray());

        public new IndexerExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}