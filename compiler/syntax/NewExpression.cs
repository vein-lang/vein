namespace wave.syntax
{
    using System;
    using Sprache;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.InteropServices;
    using stl;

    public static class Combinators
    {
        public static Parser<IEnumerable<T>> ChainForward<T, Z>(this Parser<T> elm, Parser<Z> dlm) where T : IPositionAware<T> =>
            elm.Positioned().Once().Then(x => dlm.Token().Then(_ => elm.Token().Positioned()).Many().Select(z => x.Concat(z)));
    }

    public partial class WaveSyntax
    {
        protected internal virtual Parser<ExpressionSyntax> expression =>
            assignment.Or(non_assignment_expression);

        protected internal virtual Parser<ExpressionSyntax> assignment =>
            from exp in unary_expression
            from op in assignment_operator
            from exp2 in failable_expression
            select new BinaryExpressionSyntax(exp, exp2, op);

        protected internal virtual Parser<string> assignment_operator =>
            Parse.String("<<=")
                .Or(Parse.String("??="))
                .Or(Parse.String("^="))
                .Or(Parse.String("|="))
                .Or(Parse.String("&="))
                .Or(Parse.String("%="))
                .Or(Parse.String("/="))
                .Or(Parse.String("*="))
                .Or(Parse.String("-="))
                .Or(Parse.String("+="))
                .Or(Parse.String("=")).Token().Text();
        protected internal virtual Parser<ExpressionSyntax> non_assignment_expression =>
            lambda_expression.Or(conditional_expression);

        
        protected internal virtual Parser<ExpressionSyntax> lambda_expression =>
            from dec in lfunction_signature
            from op in Parse.String("|>").Token()
            from body in lfunction_body
            select new AnonymousFunctionExpressionSyntax(dec, body);

        #region anon_func

        
        protected internal virtual Parser<ExpressionSyntax> lfunction_signature =>
            WrappedExpression('(', ')')
                .Or(WrappedExpression('(', ')', explicit_anonymous_function_parameter_list).Select(x => new AnonFunctionSignatureExpression(x)))
                .Or(WrappedExpression('(', ')', implicit_anonymous_function_parameter_list).Select(x => new AnonFunctionSignatureExpression(x)))
                .Or(IdentifierExpression);


        protected internal virtual Parser<ExpressionSyntax> lfunction_body =>
            failable_expression.Or(block.Select(x => x.GetOrElse(new BlockSyntax())));
        
        protected internal virtual Parser<ParameterSyntax[]> explicit_anonymous_function_parameter_list =>
            explicit_anonymous_function_parameter.Token().Positioned().ChainForward(Parse.Char(',').Token())
                .Select(x => x.ToArray());
        protected internal virtual Parser<ParameterSyntax[]> implicit_anonymous_function_parameter_list =>
            implicit_anonymous_function_parameter.Token().Positioned().ChainForward(Parse.Char(',').Token())
                .Select(x => x.ToArray());
        protected internal virtual Parser<ParameterSyntax> explicit_anonymous_function_parameter =>
            from i in Identifier
            from d in Parse.Char(':')
            from t in TypeExpression
            select new ParameterSyntax(t.Typeword, i);

        protected internal virtual Parser<ParameterSyntax> implicit_anonymous_function_parameter =>
            from i in Identifier
            select new ParameterSyntax((TypeSyntax)null, i);

        #endregion




        protected internal virtual Parser<IOption<BlockSyntax>> block =>
            WrappedExpression('{', '}', statement_list.Token().Select(x => new BlockSyntax(x)).Optional());

        protected internal virtual Parser<IEnumerable<StatementSyntax>> statement_list =>
            Statement.AtLeastOnce();

        protected internal virtual Parser<ExpressionSyntax> conditional_expression =>
            from operand in null_coalescing_expression
            from d in Parse.Char('?')
                .Token()
                .Then(_ => failable_expression
                    .Then(x => Parse
                        .Char(':')
                        .Token()
                        .Then(_ => failable_expression
                            .Select(z => (x, z)))))
                .Token()
                .Optional()
            select new BinaryExpressionSyntax(operand, new CoalescingExpressionSyntax(d));



        protected internal virtual Parser<ExpressionSyntax> failable_expression =>
            expression.Or(fail_expression);
        
        protected internal virtual Parser<ExpressionSyntax> fail_expression =>
            Keyword("fail")
                .Token()
                .Then(_ => expression.Token().Positioned())
                .Select(x => new FailOperationExpression(x));

        protected internal virtual Parser<BinaryExpressionSyntax> null_coalescing_expression =>
            from c in conditional_or_expression
            from a in Parse.String("??").Token().Then(_ => null_coalescing_expression.Or(fail_expression)).Optional()
            select new BinaryExpressionSyntax(c, a.GetOrDefault());


        protected internal virtual Parser<BinaryExpressionSyntax> conditional_or_expression =>
            BinaryExpression(conditional_and_expression, "||").Positioned();

        protected internal virtual Parser<BinaryExpressionSyntax> conditional_and_expression =>
            BinaryExpression(inclusive_or_expression, "&&").Positioned();

        protected internal virtual Parser<BinaryExpressionSyntax> inclusive_or_expression =>
            BinaryExpression(exclusive_or_expression, "|").Positioned();

        protected internal virtual Parser<BinaryExpressionSyntax> exclusive_or_expression =>
            BinaryExpression(and_expression, "^").Positioned();

        protected internal virtual Parser<BinaryExpressionSyntax> and_expression =>
            BinaryExpression(equality_expression, "&").Positioned();


        protected internal virtual Parser<BinaryExpressionSyntax> equality_expression =>
            BinaryExpression(relational_expression, "==", "!=").Positioned();

        protected internal virtual Parser<BinaryExpressionSyntax> relational_expression =>
            BinaryExpression(shift_expression.Or(AsTypePattern).Or(IsTypePattern), ">=", "<=", ">", "<").Positioned();

        protected internal virtual Parser<BinaryExpressionSyntax> shift_expression =>
            BinaryExpression(additive_expression, "<<", ">>").Positioned();

        protected internal virtual Parser<BinaryExpressionSyntax> additive_expression =>
            BinaryExpression(multiplicative_expression, "+", "-").Positioned();

        protected internal virtual Parser<BinaryExpressionSyntax> multiplicative_expression =>
            BinaryExpression(switch_expression, "*", "/", "%").Positioned();


        protected internal virtual Parser<BinaryExpressionSyntax> switch_expression =>
            from key in Keyword("switch").Token()
            from ob in Parse.Char('{').Token()
            from cb in Parse.Char('}').Token()
            select new InvalidBinaryExpressionSyntax();

        protected internal virtual Parser<ExpressionSyntax> AsTypePattern =>
            from key in Keyword("as").Token()
            from ty in TypeExpression.Token()
            select new AsTypePatternExpression(ty);

        protected internal virtual Parser<ExpressionSyntax> IsTypePattern =>
            from key in Keyword("is").Token()
            from ty in TypeExpression.Token()
            select new IsTypePatternExpression(ty);


        private Parser<BinaryExpressionSyntax> BinaryExpression<T>(Parser<T> t, string op) where T : ExpressionSyntax, IPositionAware<ExpressionSyntax> => 
            from c in t.Token()
            from _op in Parse.String(op).Text().Token()
            from a in Parse.String(op).Token().Then(_ => t.Token()).Many()
            select new BinaryExpressionSyntax(c, new MultipleBinaryChainExpressionSyntax(a), _op);
        
        private Parser<BinaryExpressionSyntax> BinaryExpression<T>(Parser<T> t, params string[] ops) where T : ExpressionSyntax, IPositionAware<ExpressionSyntax> => 
            from c in t.Token()
            from _op in Parse.Regex(ops.Join("|"), $"operators '{ops.Join(',')}'")
            from a in t.Token().Many()
            select new BinaryExpressionSyntax(c, new MultipleBinaryChainExpressionSyntax(a), _op);

        protected internal virtual Parser<IEnumerable<ExpressionSyntax>> expression_list =>
            from exp in expression
            from exps in Parse.Ref(() => expression).DelimitedBy(Parse.Char(',')).Token()
            select exps;

        protected internal virtual Parser<IdentifierExpression> IdentifierExpression =>
            RawIdentifier.Token().Named("Identifier").Select(x => new IdentifierExpression(x));
        internal virtual Parser<KeywordExpression> KeywordExpression(string text) =>
            Parse.IgnoreCase(text).Then(_ => Parse.LetterOrDigit.Or(Parse.Char('_')).Not()).Return(new KeywordExpression(text));

        protected internal virtual Parser<TypeExpression> SystemTypeExpression =>
            Keyword("byte").Or(
                    Keyword("sbyte")).Or(
                    Keyword("int16")).Or(
                    Keyword("uint16")).Or(
                    Keyword("int32")).Or(
                    Keyword("uint32")).Or(
                    Keyword("int64")).Or(
                    Keyword("uint64")).Or(
                    Keyword("bool")).Or(
                    Keyword("string")).Or(
                    Keyword("char")).Or(
                    Keyword("void"))
                .Token().Select(n => new TypeExpression(new TypeSyntax(n.Capitalize())))
                .Named("SystemType").Positioned();

        protected internal virtual Parser<ExpressionSyntax> argument =>
            from id in IdentifierExpression
                .Then(x => Parse.Char(':').Token().Return(x))
                .Positioned()
                .Token().Optional()
            from type in KeywordExpression("auto")
                .Or(TypeExpression.Select(x => x.Downlevel()))
                .Positioned().Token().Optional()
            from exp in expression
            select new ArgumentExpression(id, type, exp);

        protected internal virtual Parser<IndexerArgument> indexer_argument =>
            from id in IdentifierExpression
                .Then(x => Parse.Char(':').Token().Return(x))
                .Positioned()
                .Token().Optional()
            from exp in expression
            select new IndexerArgument(id, exp);

        protected internal virtual Parser<ExpressionSyntax[]> argument_list =>
            from arg in argument.Positioned()
            from args in argument.Positioned().Many().Optional().Token()
            select new[] {arg}.Concat(args.GetOrEmpty()).ToArray();


        protected internal virtual Parser<ExpressionSyntax[]> object_creation_expression =>
            from arg_list in WrappedExpression('(', ')', argument_list.Optional())
            select arg_list.GetOrEmpty().ToArray();

        protected internal virtual Parser<TypeSyntax> BaseType =>
            SystemType.Or(
                from @void in Keyword("void").Token()
                from a in Parse.Char('*').Token()
                select new TypeSyntax(@void) {IsPointer = true});
        protected internal virtual Parser<ExpressionSettingSyntax> rank_specifier =>
            from ranks in WrappedExpression('[', ']', Parse.Char(',').Token().Many())
            select new RankSpecifierValue(ranks.Count());
        protected internal virtual Parser<ExpressionSettingSyntax> nullable_specifier =>
            from h in Parse.Char('?').Optional()
            select new NullableExpressionValue(h.IsDefined);
        protected internal virtual Parser<ExpressionSettingSyntax> pointer_specifier =>
            from h in Parse.Char('*').Optional()
            select new PointerExpressionValue(h.IsDefined);

        protected internal virtual Parser<TypeExpression> TypeExpression =>
            from type in BaseType
            from meta in nullable_specifier
                .Or(rank_specifier)
                .Or(pointer_specifier)
                .Token().Positioned().Many()
            select new TypeExpression(type).WithMetadata(meta.EmptyIfNull().ToArray());


        protected internal virtual Parser<ExpressionSyntax> bracket_expression =>
            from nl in nullable_specifier.Select(x => (NullableExpressionValue) x).Token().Optional()
            from idx in WrappedExpression('[', ']', indexer_argument.ChainForward(Parse.Char(',')))
            select new BracketExpression(nl, idx.ToArray());

        protected internal virtual Parser<ExpressionSyntax> primary_expression_start =>
            LiteralExpression.Select(x => x.Downlevel())
                .Or(IdentifierExpression)
                .Or(WrappedExpression('(', ')', expression))
                .Or(SystemTypeExpression)
                .Or(KeywordExpression("this"))
                .Or(
                    from b in KeywordExpression("base")
                    from dw in  
                        Parse.Char('.').Then(_ => IdentifierExpression).Positioned().Select(x => x.Downlevel()).Or(
                            WrappedExpression('[', ']', expression_list).Select(x => new IndexerExpression(x).Downlevel()))
                    select dw)
                //.Or(
                //    from kw in KeywordExpression("new")
                //    from newexp in (

                //                )
                //        )
                .Positioned();

        protected internal virtual Parser<ExpressionSyntax> member_access =>
            from s1 in Parse.Char('?').Token().Optional()
            from s2 in Parse.Char('.').Token()
            from id in IdentifierExpression
            //from args in type_argument_list.Optional()
            select id;
        protected internal virtual Parser<ExpressionSyntax> method_invocation =>
            from o in Parse.Char('(')
            from exp in argument_list.Optional()
            from c in Parse.Char(')')
            select new MethodInvocationExpression(exp);

        protected internal virtual Parser<ExpressionSyntax> primary_expression =>
            from pe in primary_expression_start.Token()
            from s1 in Parse.Char('!').Token().Optional()
            from bk in bracket_expression.Many()
            from s2 in Parse.Char('!').Token().Optional()
            from dd in (
                from cc in (
                    from zz in member_access.Or(method_invocation)
                        .Or(Parse.String("++").Return(new OperatorExpressionSyntax(ExpressionType.Increment)))
                        .Or(Parse.String("--").Return(new OperatorExpressionSyntax(ExpressionType.Decrement)))
                        .Or(Parse.String("->").Token().Then(_ => IdentifierExpression)).Token().Positioned()
                    from s3 in Parse.Char('!').Token().Optional()
                    select zz
                ).Token()
                from bk1 in bracket_expression.Many()
                from s4 in Parse.Char('!').Token().Optional()
                select new Unnamed01ExpressionSyntax(cc, bk1)
            ).Token().Many()
            select new Unnamed02ExpressionSyntax(pe, bk, dd);

        private Parser<ExpressionSyntax> UnaryOperator(string op) =>
            from o in Parse.String(op).Token()
            from u in unary_expression
            select new UnaryExpressionSyntax() {Operand = u, OperatorType = op.ToExpressionType()};
        


        protected internal virtual Parser<ExpressionSyntax> unary_expression =>
            from f1 in primary_expression
                .Or(UnaryOperator("++"))
                .Or(UnaryOperator("--"))
                .Or(UnaryOperator("+"))
                .Or(UnaryOperator("-"))
                .Or(UnaryOperator("!"))
                .Or(UnaryOperator("~"))
                .Or(UnaryOperator("&"))
                .Or(UnaryOperator("*"))
            select f1;

    }

    public class AnonFunctionSignatureExpression : ExpressionSyntax, IPositionAware<AnonFunctionSignatureExpression>
    {
        public AnonFunctionSignatureExpression() => Params = Array.Empty<ParameterSyntax>();
        public AnonFunctionSignatureExpression(ParameterSyntax[] p) => Params = p;

        public ParameterSyntax[] Params { get; set; }

        public new AnonFunctionSignatureExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public class AsTypePatternExpression : ExpressionSyntax, IPositionAware<AsTypePatternExpression>
    {
        public TypeExpression Type { get; set; }

        public AsTypePatternExpression(TypeExpression t) => this.Type = t;

        public new AsTypePatternExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public class IsTypePatternExpression : ExpressionSyntax, IPositionAware<IsTypePatternExpression>
    {
        public TypeExpression Type { get; set; }

        public IsTypePatternExpression(TypeExpression t) => this.Type = t;

        public new IsTypePatternExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

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

    public class AnonymousFunctionExpressionSyntax : ExpressionSyntax, IPositionAware<AnonymousFunctionExpressionSyntax>
    {
        public ExpressionSyntax Declaration { get; set; }
        public ExpressionSyntax Body { get; set; }

        public AnonymousFunctionExpressionSyntax(ExpressionSyntax dec, ExpressionSyntax body)
        {
            this.Declaration = dec;
            this.Body = body;
        }

        public new AnonymousFunctionExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class FailOperationExpression : UnaryExpressionSyntax, IPositionAware<FailOperationExpression>
    {
        public FailOperationExpression(ExpressionSyntax expression) => this.Operand = expression;

        public override SyntaxType Kind => SyntaxType.FailStatement;
        
        public new FailOperationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class CoalescingExpressionSyntax : ExpressionSyntax, IPositionAware<CoalescingExpressionSyntax>
    {
        public ExpressionSyntax First { get; set; }
        public ExpressionSyntax Second { get; set; }

        public CoalescingExpressionSyntax(ExpressionSyntax f1, ExpressionSyntax f2)
        {
            this.First = f1;
            this.Second = f2;
        }

        public CoalescingExpressionSyntax(IOption<(ExpressionSyntax x, ExpressionSyntax z)> pair) 
            => (First, Second) = pair.GetOrDefault();

        public new CoalescingExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class Unnamed01ExpressionSyntax : ExpressionSyntax, IPositionAware<Unnamed01ExpressionSyntax>
    {
        public ExpressionSyntax cc;
        public IEnumerable<ExpressionSyntax> bk1;

        public Unnamed01ExpressionSyntax(ExpressionSyntax cc, IEnumerable<ExpressionSyntax> bk1)
        {
            this.cc = cc;
            this.bk1 = bk1;
        }

        public new Unnamed01ExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class Unnamed02ExpressionSyntax : ExpressionSyntax, IPositionAware<Unnamed02ExpressionSyntax>
    {
        public Unnamed02ExpressionSyntax(ExpressionSyntax pe, IEnumerable<ExpressionSyntax> bk, IEnumerable<Unnamed01ExpressionSyntax> dd)
        {
            Pe = pe;
            Bk = bk;
            Dd = dd;
        }

        public ExpressionSyntax Pe { get; }
        public IEnumerable<ExpressionSyntax> Bk { get; }
        public IEnumerable<Unnamed01ExpressionSyntax> Dd { get; }

        public new Unnamed02ExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class MethodInvocationExpression : ExpressionSyntax, IPositionAware<MethodInvocationExpression>
    {
        public ExpressionSyntax[] Arguments { get; set; }

        public MethodInvocationExpression(IOption<ExpressionSyntax[]> args) 
            => this.Arguments = args.GetOrEmpty().ToArray();

        public new MethodInvocationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class FullMethodInvocationExpression : ExpressionSyntax, IPositionAware<FullMethodInvocationExpression>
    {
        public ExpressionSyntax[] Arguments { get; set; }

        public FullMethodInvocationExpression(IOption<ExpressionSyntax[]> args) 
            => this.Arguments = args.GetOrEmpty().ToArray();

        public new FullMethodInvocationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class BracketExpression : ExpressionSyntax, IPositionAware<BracketExpression>
    {
        public NullableExpressionValue Nullable { get; set; }
        public IndexerArgument[] Arguments { get; set; }

        public BracketExpression(IOption<NullableExpressionValue> nullable, IndexerArgument[] args)
        {
            this.Nullable = nullable.GetOrElse(new NullableExpressionValue(false));
            this.Arguments = args;
        }
        public new BracketExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public class IndexerArgument : ExpressionSyntax, IPositionAware<IndexerArgument>
    {
        public IdentifierExpression Identifier { get; set; }
        public ExpressionSyntax Value { get; set; }

        public IndexerArgument(IOption<IdentifierExpression> id, ExpressionSyntax value)
        {
            this.Identifier = id.GetOrDefault();
            this.Value = value;
        }

        public new IndexerArgument SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public abstract class ExpressionSettingSyntax : ExpressionSyntax, IPositionAware<ExpressionSettingSyntax>
    {
        public new ExpressionSettingSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public class PointerExpressionValue : ExpressionSettingSyntax, IPositionAware<PointerExpressionValue>
    {
        public PointerExpressionValue(bool value) => this.HasPointer = value;
        public bool HasPointer { get; set; }
        public new PointerExpressionValue SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public class NullableExpressionValue : ExpressionSettingSyntax, IPositionAware<NullableExpressionValue>
    {
        public NullableExpressionValue(bool value) => this.HasNullable = value;
        public bool HasNullable { get; set; }
        public new NullableExpressionValue SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public class RankSpecifierValue : ExpressionSettingSyntax, IPositionAware<RankSpecifierValue>
    {
        public int Rank { get; set; }

        public RankSpecifierValue(int len)
        {
            this.Rank = len;
            this.ExpressionString = $"[{new string(',', len)}]";
        }
        public new RankSpecifierValue SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public class ArgumentExpression : ExpressionSyntax, IPositionAware<ArgumentExpression>
    {
        public IdentifierExpression Identifier { get; set; }
        public ExpressionSyntax Type { get; set; }
        public ExpressionSyntax Value { get; set; }

        public ArgumentExpression(IOption<IdentifierExpression> id, IOption<ExpressionSyntax> t, ExpressionSyntax v)
        {
            this.Identifier = id.GetOrDefault();
            this.Type = t.GetOrDefault();
            this.Value = v;
        }
        public new ArgumentExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public class TypeExpression : ExpressionSyntax, IPositionAware<TypeExpression>
    {
        public TypeSyntax Typeword { get; set; }

        public TypeExpression(TypeSyntax typeword) : base(typeword.Identifier) => this.Typeword = typeword;

        public new TypeExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public TypeExpression WithMetadata(ExpressionSettingSyntax[] settings)
        {
            return this;
        }
    }
    public class KeywordExpression : ExpressionSyntax, IPositionAware<KeywordExpression>
    {
        public string Keyword { get; set; }


        public KeywordExpression(string keyword) : base(keyword) => this.Keyword = keyword;

        public new KeywordExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class IndexerExpression : ExpressionSyntax, IPositionAware<IndexerExpression>
    {
        private readonly ExpressionSyntax[] _exps;

        public IndexerExpression(IEnumerable<ExpressionSyntax> exps) => _exps = exps.ToArray();

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(_exps.OfExactType<BaseSyntax>().ToArray());

        public new IndexerExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        
    }

    public class IdentifierExpression : ExpressionSyntax, IPositionAware<IdentifierExpression>
    {
        public IdentifierExpression(string name) : base(name)
        {
        }
        public new IdentifierExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}