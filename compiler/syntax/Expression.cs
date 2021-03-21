namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using Sprache;
    using stl;

    public partial class WaveSyntax
    {
        // examples: {}, { /* inner comments */ }, { int a = 0; return; } // trailing comments
        protected internal virtual Parser<BlockSyntax> Block =>
            from comments in CommentParser.AnyComment.Token().Many()
            from openBrace in Parse.Char('{').Token()
            from statements in Statement.Many()
            from closeBrace in Parse.Char('}').Commented(this)
            select new BlockSyntax
            {
                LeadingComments = comments.ToList(),
                Statements = statements.ToList(),
                InnerComments = closeBrace.LeadingComments.ToList(),
                TrailingComments = closeBrace.TrailingComments.ToList(),
            };

        protected internal virtual Parser<IOption<ExpressionSyntax>> KeywordExpressionStatement(string keyword) =>
            KeywordExpression(keyword).Token().Then(_ => QualifiedExpression.Token().Optional());
    }
    
    public class OperatorExpressionSyntax : ExpressionSyntax
    {
        public ExpressionType OperatorType { get; set; }

        public OperatorExpressionSyntax()  { }

        public OperatorExpressionSyntax(ExpressionType exp) => this.OperatorType = exp;
    }
    
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
    
    public class NewExpressionSyntax : OperatorExpressionSyntax, IPositionAware<NewExpressionSyntax>
    {
        public override SyntaxType Kind => SyntaxType.ClassInitializer;
        public override IEnumerable<BaseSyntax> ChildNodes => CtorArgs.Concat(new BaseSyntax[] { TargetType });
        public TypeSyntax TargetType { get; set; }
        public List<ExpressionSyntax> CtorArgs { get; set; }
        
        public new NewExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class AccessExpressionSyntax : BinaryExpressionSyntax, IPositionAware<AccessExpressionSyntax>
    {
        public ExpressionSyntax Exp1, Exp2;
        public ExpressionSyntax op;
        public new AccessExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class InvalidBinaryExpressionSyntax : BinaryExpressionSyntax, IPositionAware<BinaryExpressionSyntax>
    {
        public new InvalidBinaryExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    
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
    
    
    public class MemberAccessSyntax : ExpressionSyntax
    {
        public string MemberName { get; set; }
        public string[] MemberChain { get; set; }
        
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;
        public override SyntaxType Kind => SyntaxType.MemberAccessExpression;
        
        public override string ExpressionString
        {
            get
            {
                if (MemberChain is null || MemberChain.Length == 0)
                    return $"{MemberName}";
                return $"{string.Join(".", MemberChain)}.{MemberName}";
            }
        }
    }
    
    public static class ExpressionTypeEx
    {
        public static ExpressionType ToExpressionType(this string str)
        {
            return Enum.GetValues<ExpressionType>().Select(x => (x.GetSymbol(), x)).Where(x => x.Item1 != null).First(x => x.Item1.Equals(str)).x;
        }
        public static string GetSymbol(this ExpressionType exp)
        {
            switch (exp)
            {
                case ExpressionType.Equal:
                    return "==";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.Power:
                    return "^^";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return "+";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.AndAlso:
                    return "&&";
                case ExpressionType.Coalesce:
                    return "??";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LeftShift:
                    return "<<";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";
                case ExpressionType.Not:
                    return "!";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.OrElse:
                    return "||";
                case ExpressionType.RightShift:
                    return ">>";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
                case ExpressionType.Assign:
                    return "=";
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                    return "+=";
                case ExpressionType.AndAssign:
                    return "&=";
                case ExpressionType.DivideAssign:
                    return "/=";
                case ExpressionType.ExclusiveOrAssign:
                    return "^=";
                case ExpressionType.LeftShiftAssign:
                    return "<<=";
                case ExpressionType.ModuloAssign:
                    return "%=";
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                    return "*=";
                case ExpressionType.OrAssign:
                    return "|=";
                case ExpressionType.RightShiftAssign:
                    return ">>=";
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                    return "-=";
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PreIncrementAssign:
                    return "++";
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostDecrementAssign:
                    return "--";
                case ExpressionType.OnesComplement:
                    return "~";
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return "-";
                case ExpressionType.MemberAccess:
                    return ".";
                case ExpressionType.Conditional:
                    return "?:";
                default:
                    return null;
            }
        }
    }
}