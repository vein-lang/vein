namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using vein.extensions;

    public class MemberAccessExpression : ExpressionSyntax, IPositionAware<MemberAccessExpression>
    {
        public MemberAccessExpression(ExpressionSyntax start, IEnumerable<ExpressionSyntax> indexerAccess, IEnumerable<ExpressionSyntax> chain)
        {
            Start = start;
            IndexerAccess = indexerAccess;
            Chain = chain;
        }

        public ExpressionSyntax Start { get; }
        public IEnumerable<ExpressionSyntax> IndexerAccess { get; }
        public IEnumerable<ExpressionSyntax> Chain { get; }

        public new MemberAccessExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
        public IEnumerable<ExpressionSyntax> GetChain()
        {
            var prepacked = new[] {Start}.Concat(IndexerAccess).Concat(Chain).ToArray();
            var result = new List<ExpressionSyntax>();
            foreach (var syntax in prepacked)
            {
                if (syntax is ChainAccessExpression chain)
                    result.AddRange(new[] { chain.Start }.Concat(chain.Other));
                else
                    result.Add(syntax);
            }
            return result;
        }

        public override string ExpressionString
            => $"{GetChain().Select(x => x.ExpressionString).Join('.')}";
    }
}
