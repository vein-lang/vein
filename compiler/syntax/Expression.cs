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
        // creates dummy generic parser for any expressions with matching braces
        protected internal virtual Parser<string> GenericExpressionInBraces(char open = '(', char close = ')') =>
            from openBrace in Parse.Char(open).Token()
            from expression in GenericExpressionCore().Optional()
            from closeBrace in Parse.Char(close).Token()
            select expression.GetOrElse(string.Empty).Trim();
        // creates dummy generic parser for expressions with matching braces allowing commas and semicolons by default
        protected internal virtual Parser<string> GenericExpressionCore(string forbidden = null, bool allowCurlyBraces = true)
        {
            var subExpressionParser = GenericNewExpression.Select(x => $" {x}")
                .Or(Parse.CharExcept("'/(){}[]" + forbidden).Except(GenericNewExpression).Many().Text().Token())
                .Or(Parse.Char('/').Then(_ => Parse.Chars('/', '*').Not()).Once().Return("/"))
                .Or(CommentParser.AnyComment.Return(string.Empty))
                .Or(StringLiteral)
                //.Or(InvocationExpression.Select(x => x.))
                .Or(GenericExpressionInBraces('(', ')').Select(x => $"({x})"))
                .Or(GenericExpressionInBraces('[', ']').Select(x => $"[{x}]"));

            // optionally include support for curly braces
            if (allowCurlyBraces)
            {
                subExpressionParser = subExpressionParser
                    .Or(GenericExpressionInBraces('{', '}').Select(x => $"{{{x}}}"));
            }

            return
                from subExpressions in subExpressionParser.Many()
                let expr = string.Join(string.Empty, subExpressions)
                where !string.IsNullOrWhiteSpace(expr)
                select expr;
        }
        
        // dummy generic parser for expressions with matching braces
        protected internal virtual Parser<string> GenericExpression =>
            GenericExpressionCore(forbidden: ",;").Select(x => x.Trim());
        
        protected internal virtual Parser<IOption<ExpressionSyntax>> KeywordExpressionStatement(string keyword) =>
            from key in Keyword(keyword).Token()
            from expr in QualifiedExpression.XOptional()
            from semicolon in Parse.Char(';')
            select expr;
        
        // examples: new Map<string, string>
        protected internal virtual Parser<string> GenericNewExpression =>
            from prev in WaveParserExtensions.PrevChar(c => !char.IsLetterOrDigit(c), "non-alphanumeric")
            from @new in Parse.IgnoreCase("new").Then(_ => Parse.LetterOrDigit.Not()).Token()
            from type in TypeReference.Token()
            select $"new {type.AsString()}";

        /// <summary> == </summary>
        protected internal virtual Parser<ExpressionType> Equal 
            => Operator("==", ExpressionType.Equal);
        /// <summary> != </summary>
        protected internal virtual Parser<ExpressionType> NotEqual 
            => Operator("!=", ExpressionType.NotEqual);
        /// <summary> || </summary>
        protected internal virtual Parser<ExpressionType> OrElse 
            => Operator("||", ExpressionType.OrElse);
        /// <summary> && </summary>
        protected internal virtual Parser<ExpressionType> AndAlso 
            => Operator("&&", ExpressionType.AndAlso);
        /// <summary> &lt; </summary>
        protected internal virtual Parser<ExpressionType> LessThan 
            => Operator("<", ExpressionType.LessThan);
        /// <summary> &gt; </summary>
        protected internal virtual Parser<ExpressionType> GreaterThan 
            => Operator(">", ExpressionType.GreaterThan);
        /// <summary> &gt;= </summary>
        protected internal virtual Parser<ExpressionType> LessThanOrEqual 
            => Operator("<=", ExpressionType.LessThanOrEqual);
        /// <summary> &gt;= </summary>
        protected internal virtual Parser<ExpressionType> GreaterThanOrEqual
            => Operator(">=", ExpressionType.GreaterThanOrEqual);
        /// <summary> + </summary>
        protected internal virtual Parser<ExpressionType> Add 
            => Operator("+", ExpressionType.AddChecked);
        /// <summary> - </summary>
        protected internal virtual Parser<ExpressionType> Subtract 
            => Operator("-", ExpressionType.SubtractChecked);
        /// <summary> * </summary>
        protected internal virtual Parser<ExpressionType> Multiply 
            => Operator("*", ExpressionType.MultiplyChecked);
        /// <summary> / </summary>
        protected internal virtual Parser<ExpressionType> Divide 
            => Operator("/", ExpressionType.Divide);
        /// <summary> % </summary>
        protected internal virtual Parser<ExpressionType> Modulo 
            => Operator("%", ExpressionType.Modulo);
        /// <summary> ^^ </summary>
        protected internal virtual Parser<ExpressionType> Power 
            => Operator("^^", ExpressionType.Power);
        
        /// <summary> ^ </summary>
        protected internal virtual Parser<ExpressionType> Xor 
            => Operator("^", ExpressionType.ExclusiveOr);
        /// <summary> &amp; </summary>
        protected internal virtual Parser<ExpressionType> And 
            => Operator("&", ExpressionType.And);
        /// <summary> | </summary>
        protected internal virtual Parser<ExpressionType> Or 
            => Operator("|", ExpressionType.Or);
        /// <summary> &lt;&lt; </summary>
        protected internal virtual Parser<ExpressionType> LeftShift 
            => Operator("<<", ExpressionType.LeftShift);
        /// <summary> &gt;&gt; </summary>
        protected internal virtual Parser<ExpressionType> RightShift 
            => Operator(">>", ExpressionType.RightShift);
        /// <summary> ~ </summary>
        protected internal virtual Parser<ExpressionSyntax> OnesComplement =>
            Operand('~', ExpressionType.OnesComplement);
        /// <summary> - </summary>
        protected internal virtual Parser<ExpressionSyntax> Negate =>
            Operand('-', ExpressionType.Negate);
        /// <summary> ! </summary>
        protected internal virtual Parser<ExpressionSyntax> Not =>
            Operand('!', ExpressionType.Not);
        
        
        
            
        
        protected internal virtual Parser<ExpressionType> Operator(string op, ExpressionType opType) 
            => Parse.String(op).Token().Log("Operator").Return(opType);
        protected internal virtual Parser<ExpressionSyntax> Operand(char symbol, ExpressionType type) =>
        (   
            from sign in Parse.Char(symbol)
            from factor in RawExpression()
            select new UnaryExpressionSyntax()
            {
                Operand = factor,
                OperatorType = type
            }
        ).XOr(RawExpression()).Log("Operand").Token();
        
        


        protected internal virtual Parser<ExpressionSyntax> New =>
        (
            from sign in Keyword("new")
            from type in TypeReference
            from ctorArgs in Parse.Ref(() => QualifiedArgumentExpression)
            select new NewExpressionSyntax
            {
                TargetType = type,
                CtorArgs = ctorArgs.GetOrEmpty().ToList(),
                OperatorType = ExpressionType.New
            }
        ).Token();
        
        protected internal virtual Parser<IOption<IEnumerable<ExpressionSyntax>>> QualifiedArgumentExpression =>
            from open in Parse.Char('(')
            from expression in Parse.Ref(() => QualifiedExpression).DelimitedBy(Parse.Char(',').Token()).Optional()
            from close in Parse.Char(')')
            select expression;
        
        protected internal virtual Parser<ExpressionSyntax> RawExpression(char open = '(', char close = ')') =>
            (
                from _open in Parse.Char(open)
                from expr in Parse.Ref(() => QualifiedExpression)
                from _close in Parse.Char(close)
                select expr
            ).Named("expression")
            .Or(LiteralExpression)
            .Or(InvocationExpression)
            .Or(MemberAccessExpression);



        protected internal virtual Parser<ExpressionSyntax> Level0
            => Parse.ChainRightOperator(Power.XOr(Xor), Negate.Or(OnesComplement).Or(Not), MakeBinary)
                .Log("Expression::Level0").Positioned();
        
        protected internal virtual Parser<ExpressionSyntax> Level1 
            => Parse.ChainOperator(Multiply.Or(Divide).Or(Modulo), Level0, MakeBinary)
                .Log("Expression::Level1").Positioned();

        protected internal virtual Parser<ExpressionSyntax> Level2 
            => Parse.ChainOperator(Add.Or(Subtract), Level1, MakeBinary)
                .Log("Expression::Level2").Positioned();

        protected internal virtual Parser<ExpressionSyntax> Level3
            => Parse.ChainOperator(LeftShift.Or(RightShift), Level2, MakeBinary)
                .Log("Expression::Level3").Positioned();
        
        protected internal virtual Parser<ExpressionSyntax> Level4 
            => Parse.ChainOperator(Xor.Or(And).Or(Or), Level3, MakeBinary)
                .Log("Expression::Level4").Positioned();
        
        protected internal virtual Parser<ExpressionSyntax> Level5 
            => Parse.ChainOperator(AndAlso.Or(OrElse), Level4, MakeBinary)
                .Log("Expression::Level5").Positioned();

        protected internal virtual Parser<ExpressionSyntax> Level6
            => Parse.ChainOperator(LessThanOrEqual.Or(GreaterThanOrEqual).Or(LessThan).Or(GreaterThan), 
                Level5, MakeBinary)
                .Log("Expression::Level6").Positioned();
        
        protected internal virtual Parser<ExpressionSyntax> QualifiedExpression 
            => Parse.ChainOperator(NotEqual.XOr(Equal), Level6, MakeBinary)
                .Log("QualifiedExpression").Positioned();
        
        
        private static ExpressionSyntax MakeBinary(ExpressionType type, ExpressionSyntax left, ExpressionSyntax right) =>
        new BinaryExpressionSyntax
        {
            OperatorType = type,
            Left = left,
            Right = right
        };
    }
    
    public class OperatorExpressionSyntax : ExpressionSyntax
    {
        public ExpressionType OperatorType { get; set; }
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
    
    public class NewExpressionSyntax : OperatorExpressionSyntax
    {
        public override SyntaxType Kind => SyntaxType.ClassInitializer;
        public override IEnumerable<BaseSyntax> ChildNodes => CtorArgs.Concat(new BaseSyntax[] { TargetType });
        public TypeSyntax TargetType { get; set; }
        public List<ExpressionSyntax> CtorArgs { get; set; }
        
    }
    
    public class BinaryExpressionSyntax : OperatorExpressionSyntax
    {
        public ExpressionSyntax Left { get; set; }
        public ExpressionSyntax Right { get; set; }
        
        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Left, Right);
        
        public override SyntaxType Kind => SyntaxType.BinaryExpression;
        
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(exp), exp, null);
            }
        }
    }
}