namespace insomnia.syntax
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public static class ExpressionTypeEx
    {
        public static ExpressionType ToExpressionType(this string str)
        {
            return Enum.GetValues<ExpressionType>().Select(x => (GetSymbol(x), x)).Where(x => x.Item1 != null).First(x => x.Item1.Equals(str)).x;
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