namespace insomnia.syntax
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;
    using Sprache;

    public class BinaryExpressionSyntax : OperatorExpressionSyntax, IPositionAware<BinaryExpressionSyntax>
    {
        public ExpressionSyntax Left { get; set; }
        public ExpressionSyntax Right { get; set; }
        
        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Left, Right);
        
        public override SyntaxType Kind => SyntaxType.BinaryExpression;

        public new BinaryExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public BinaryExpressionSyntax()
        {
            
        }

        public BinaryExpressionSyntax(ExpressionSyntax first, ExpressionSyntax last)
        {
            this.Left = first;
            this.Right = last;
        }

        public BinaryExpressionSyntax(ExpressionSyntax first, ExpressionSyntax last, ExpressionType op)
        {
            this.OperatorType = op;
            this.Left = first;
            this.Right = last;
        }
        public BinaryExpressionSyntax(ExpressionSyntax first, ExpressionSyntax last, string op)
        {
            this.OperatorType = op.ToExpressionType();
            this.Left = first;
            this.Right = last;
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append("(");
            if(Left is not null)
            {
                if (Left.ExpressionString is not null)
                    str.Append(Left.ExpressionString);
                else
                    str.Append(Left.Kind);
                
                str.Append($" {OperatorType.GetSymbol()} ");
            }
            else
                str.Append($"{OperatorType.GetSymbol()}");
            
            if (Right.ExpressionString is not null)
                str.Append(Right.ExpressionString);
            else
                str.Append(Right.Kind);
            str.Append(")");
            return str.ToString();
        }
        
        public override string ExpressionString => ToString();
    }
}