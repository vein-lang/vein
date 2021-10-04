namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;

    public class MultipleBinaryChainExpressionSyntax : ExpressionSyntax,
        IPositionAware<MultipleBinaryChainExpressionSyntax>
    {
        public ExpressionSyntax[] Expressions { get; set; }

        public MultipleBinaryChainExpressionSyntax(IEnumerable<ExpressionSyntax> exps)
            => Expressions = exps.EmptyIfNull().ToArray();

        public new MultipleBinaryChainExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
