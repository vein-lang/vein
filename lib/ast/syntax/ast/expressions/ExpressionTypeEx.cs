namespace mana.syntax
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public static class ExpressionTypeEx
    {
        public static ExpressionType ToExpressionType(this string str, bool isBinary) =>
            Enum.GetValues<ExpressionType>()
                .Select(x => (GetSymbol(x, isBinary), x))
                .Where(x => x.Item1 != null)
                .First(x => x.Item1.Equals(str)).x;

        public static bool IsLogic(this ExpressionType exp)
        {
            switch (exp)
            {
                case ExpressionType.Power:
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Divide:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.LeftShift:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Assign:
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.AndAssign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.OrAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.OnesComplement:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.MemberAccess:
                    return false;
                case ExpressionType.And:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Conditional:
                case ExpressionType.Or:
                case ExpressionType.Not:
                case ExpressionType.OrElse:
                case ExpressionType.AndAlso:
                case ExpressionType.Coalesce:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    return true;
                default:
                    throw new Exception();
            }
        }
        public static string GetSymbol(this ExpressionType exp, bool isBinary = true)
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
                case ExpressionType.MemberAccess:
                    return ".";
                case ExpressionType.Conditional:
                    return "?:";
                case ExpressionType.Not:
                    return "!";
                case ExpressionType.PostIncrementAssign when !isBinary:
                case ExpressionType.PreIncrementAssign when !isBinary:
                    return "++";
                case ExpressionType.PreDecrementAssign when !isBinary:
                case ExpressionType.PostDecrementAssign when !isBinary:
                    return "--";
                case ExpressionType.OnesComplement when !isBinary:
                    return "~";
                case ExpressionType.Negate when !isBinary:
                case ExpressionType.NegateChecked when !isBinary:
                    return "-";
                default:
                    return null;
            }
        }
    }
}
