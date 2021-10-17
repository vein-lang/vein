namespace vein.syntax
{
    using System.Linq.Expressions;
    using Sprache;

    public class AccessExpressionSyntax : BinaryExpressionSyntax, IPositionAware<AccessExpressionSyntax>
    {
        public AccessExpressionSyntax(ExpressionSyntax left, ExpressionSyntax right)
        {
            this.Left = left;
            this.Right = right;
            this.OperatorType = ExpressionType.MemberAccess;
        }
        public new AccessExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"{Left}.{Right}";
    }

    public class IndexerAccessExpressionSyntax : AccessExpressionSyntax, IPositionAware<IndexerAccessExpressionSyntax>
    {
        public IndexerAccessExpressionSyntax(ExpressionSyntax left, ExpressionSyntax right) : base(left, right)
            => base.OperatorType = ExpressionType.ArrayIndex;

        public new IndexerAccessExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"{Left}[{Right}]";
    }
}
