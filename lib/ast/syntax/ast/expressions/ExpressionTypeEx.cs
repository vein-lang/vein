namespace vein.syntax
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

        public static bool IsLogic(this ExpressionType exp) => exp switch
        {
            ExpressionType.GreaterThan => true,
            ExpressionType.GreaterThanOrEqual => true,
            ExpressionType.LessThan => true,
            ExpressionType.LessThanOrEqual => true,
            ExpressionType.Conditional => true,
            ExpressionType.Not => true,
            ExpressionType.OrElse => true,
            ExpressionType.AndAlso => true,
            ExpressionType.Coalesce => true,
            ExpressionType.Equal => true,
            ExpressionType.NotEqual => true,
            _ => false
        };

        public static string GetSymbol(this ExpressionType exp, bool isBinary = true) => exp switch
        {
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.Power => "^^",
            ExpressionType.Add => "+",
            ExpressionType.AddChecked => "+",
            ExpressionType.And => "&",
            ExpressionType.AndAlso => "&&",
            ExpressionType.Coalesce => "??",
            ExpressionType.Divide => "/",
            ExpressionType.ExclusiveOr => "^",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LeftShift => "<<",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.Modulo => "%",
            ExpressionType.Multiply => "*",
            ExpressionType.MultiplyChecked => "*",
            ExpressionType.Or => "|",
            ExpressionType.OrElse => "||",
            ExpressionType.RightShift => ">>",
            ExpressionType.Subtract => "-",
            ExpressionType.SubtractChecked => "-",
            ExpressionType.Assign => "=",
            ExpressionType.AddAssign => "+=",
            ExpressionType.AddAssignChecked => "+=",
            ExpressionType.AndAssign => "&=",
            ExpressionType.DivideAssign => "/=",
            ExpressionType.ExclusiveOrAssign => "^=",
            ExpressionType.LeftShiftAssign => "<<=",
            ExpressionType.ModuloAssign => "%=",
            ExpressionType.MultiplyAssign => "*=",
            ExpressionType.MultiplyAssignChecked => "*=",
            ExpressionType.OrAssign => "|=",
            ExpressionType.RightShiftAssign => ">>=",
            ExpressionType.SubtractAssign => "-=",
            ExpressionType.SubtractAssignChecked => "-=",
            ExpressionType.MemberAccess => ".",
            ExpressionType.Conditional => "?:",
            ExpressionType.Not => "!",
            ExpressionType.PostIncrementAssign when !isBinary => "++",
            ExpressionType.PreIncrementAssign when !isBinary => "++",
            ExpressionType.PreDecrementAssign when !isBinary => "--",
            ExpressionType.PostDecrementAssign when !isBinary => "--",
            ExpressionType.OnesComplement when !isBinary => "~",
            ExpressionType.Negate when !isBinary => "-",
            ExpressionType.NegateChecked when !isBinary => "-",
            _ => null
        };
    }
}
