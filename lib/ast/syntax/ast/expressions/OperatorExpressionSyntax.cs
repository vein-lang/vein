namespace insomnia.syntax
{
    using System.Linq.Expressions;
    using stl;
    public class OperatorExpressionSyntax : ExpressionSyntax
    {
        public ExpressionType OperatorType { get; set; }

        public OperatorExpressionSyntax()  { }

        public OperatorExpressionSyntax(ExpressionType exp) => this.OperatorType = exp;
    }
}