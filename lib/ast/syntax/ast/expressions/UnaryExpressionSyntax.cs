namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Text;

    public class UnaryExpressionSyntax : OperatorExpressionSyntax
    {
        public ExpressionSyntax Operand { get; set; }
        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Operand);
        public override SyntaxType Kind => SyntaxType.PostfixUnaryExpression;

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append("(");
            str.Append(OperatorType.GetSymbol());
            if (Operand.ExpressionString is not null)
                str.Append(Operand.ExpressionString);
            else
                str.Append(Operand.Kind);
            str.Append(")");
            return str.ToString();
        }

        public override string ExpressionString => ToString();
    }
}
