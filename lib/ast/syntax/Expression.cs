namespace mana.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using extensions;
    using Sprache;
    using stl;

    public partial class ManaSyntax
    {
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
            KeywordExpression(keyword)
                .Token()
                .Then(_ => QualifiedExpression.Token().XOptional())
                .Then(x => Parse.Char(';').Token().Return(x));

        protected internal virtual Parser<ExpressionSyntax> QualifiedExpression =>
            assignment.Or(non_assignment_expression);
        protected internal virtual Parser<ExpressionSyntax> assignment =>
            (
                from exp in unary_expression
                from op in assignment_operator
                from exp2 in QualifiedExpression
                select new BinaryExpressionSyntax(exp, exp2, op)
            )
            .Or(
                from exp in unary_expression
                from op in Parse.String("??=").Text().Token()
                from exp2 in failable_expression
                select new BinaryExpressionSyntax(exp, exp2, op)
            );

        protected internal virtual Parser<string> assignment_operator =>
            Parse.String("<<=")
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
            conditional_expression.Or(lambda_expression);


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
            from i in IdentifierExpression
            from d in Parse.Char(':')
            from t in TypeExpression.Token().Positioned()
            select new ParameterSyntax(t.Typeword, i);

        protected internal virtual Parser<ParameterSyntax> implicit_anonymous_function_parameter =>
            from i in IdentifierExpression
            select new ParameterSyntax((TypeSyntax)null, i);

        #endregion

        protected internal virtual Parser<ExpressionSyntax> range_expression =>
            (
                from s1 in unary_expression
                from op in Parse.String("..").Token()
                from s2 in unary_expression
                select new RangeExpressionSyntax(s1, s2)
            ).Or(unary_expression);

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
            select FlatIfEmptyOrNull(operand, new CoalescingExpressionSyntax(d));



        protected internal virtual Parser<ExpressionSyntax> failable_expression =>
            QualifiedExpression.Or(fail_expression);

        protected internal virtual Parser<ExpressionSyntax> fail_expression =>
            Keyword("fail")
                .Token()
                .Then(_ => QualifiedExpression.Token().Positioned())
                .Select(x => new FailOperationExpression(x));

        protected internal virtual Parser<ExpressionSyntax> null_coalescing_expression =>
            from c in inclusive_or_expression
            from a in Parse.String("??").Token().Then(_ => null_coalescing_expression.Or(fail_expression)).Optional()
            select FlatIfEmptyOrNull(c, a, "??");

        protected internal virtual Parser<ExpressionSyntax> inclusive_or_expression =>
            BinaryExpression(exclusive_or_expression, "|").Positioned();

        protected internal virtual Parser<ExpressionSyntax> exclusive_or_expression =>
            BinaryExpression(and_expression, "^").Positioned();

        protected internal virtual Parser<ExpressionSyntax> and_expression =>
            BinaryExpression(equality_expression, "&").Positioned();


        protected internal virtual Parser<ExpressionSyntax> equality_expression =>
            BinaryExpression(relational_expression, "!=", "==").Positioned();

        protected internal virtual Parser<ExpressionSyntax> relational_expression =>
            BinaryExpression(shift_expression.Or(AsTypePattern).Or(IsTypePattern), ">=", "<=", ">", "<").Positioned();

        protected internal virtual Parser<ExpressionSyntax> shift_expression =>
            BinaryExpression(additive_expression, "<<", ">>").Positioned();

        protected internal virtual Parser<ExpressionSyntax> additive_expression =>
            BinaryExpression(multiplicative_expression, "+", "-").Positioned();

        protected internal virtual Parser<ExpressionSyntax> multiplicative_expression =>
            BinaryExpression(conditional_or_expression, "*", "/", "%").Positioned();

        protected internal virtual Parser<ExpressionSyntax> conditional_or_expression =>
            BinaryExpression(conditional_and_expression, "||").Positioned();

        protected internal virtual Parser<ExpressionSyntax> conditional_and_expression =>
            BinaryExpression(power_expression, "&&").Positioned();

        protected internal virtual Parser<ExpressionSyntax> power_expression =>
            BinaryExpression(range_expression, "^^").Positioned();

        // TODO
        protected internal virtual Parser<BinaryExpressionSyntax> switch_expression =>
            from key in Keyword("switch").Token()
            from ob in Parse.Char('{').Token()
            from cb in Parse.Char('}').Token()
            select new InvalidBinaryExpressionSyntax();

        protected internal virtual Parser<ExpressionSyntax> AsTypePattern =>
            from key in Keyword("as").Token()
            from ty in TypeExpression.Token().Positioned()
            select new AsTypePatternExpression(ty);

        protected internal virtual Parser<ExpressionSyntax> IsTypePattern =>
            from key in Keyword("is").Token()
            from ty in TypeExpression.Token().Positioned()
            select new IsTypePatternExpression(ty);


        private Parser<ExpressionSyntax> BinaryExpression<T>(Parser<T> t, string op) where T : ExpressionSyntax, IPositionAware<ExpressionSyntax> =>
            from c in t.Token()
            from data in
                (from _op in Parse.String(op).Text().Token()
                 from a in t.Token()
                 select (_op, a)).Many()
            select FlatIfEmptyOrNull(c, data.EmptyIfNull().ToArray());

        private Parser<ExpressionSyntax> BinaryExpression<T>(Parser<T> t, params string[] ops) where T : ExpressionSyntax, IPositionAware<ExpressionSyntax> =>
            from c in t.Token()
            from data in
                (from _op in Parse.Regex(ops.Select(x => $"\\{x}").Join("|"), $"operators '{ops.Join(',')}'")
                 from a in t.Token()
                 select (_op, a)).Many()
            select FlatIfEmptyOrNull(c, data.EmptyIfNull().ToArray());

        protected internal virtual Parser<IEnumerable<ExpressionSyntax>> expression_list =>
            from exp in QualifiedExpression
            from exps in Parse.Ref(() => QualifiedExpression).DelimitedBy(Parse.Char(',')).Token()
            select exps;

        protected internal virtual Parser<IdentifierExpression> IdentifierExpression =>
            RawIdentifier.Token().Named("Identifier").Select(x => new IdentifierExpression(x)).Positioned();
        internal virtual Parser<IdentifierExpression> KeywordExpression(string text) =>
            Parse.IgnoreCase(text).Then(_ => Parse.LetterOrDigit.Or(Parse.Char('_')).Not()).Return(new IdentifierExpression(text)).Positioned();

        protected internal virtual Parser<TypeExpression> SystemTypeExpression =>
            KeywordExpression("byte").Or(
                    KeywordExpression("sbyte")).Or(
                    KeywordExpression("int16")).Or(
                    KeywordExpression("uint16")).Or(
                    KeywordExpression("int32")).Or(
                    KeywordExpression("uint32")).Or(
                    KeywordExpression("int64")).Or(
                    KeywordExpression("uint64")).Or(
                    KeywordExpression("bool")).Or(
                    KeywordExpression("string")).Or(
                    KeywordExpression("char")).Or(
                    KeywordExpression("void"))
                .Token().Select(n => new TypeExpression(new TypeSyntax(n)))
                .Named("SystemType").Positioned();

        protected internal virtual Parser<ExpressionSyntax> argument =>
            //from id in IdentifierExpression
            //    .Then(x => Parse.Char(':').Token().Return(x))
            //    .Positioned()
            //    .Token()
            //    .Optional()
            //from type in KeywordExpression("auto")
            //    .Or(TypeExpression.Select(x => x.Downlevel()))
            //    .Positioned().Token().Optional()
            from exp in QualifiedExpression
            select new ArgumentExpression(null, null, exp);

        protected internal virtual Parser<IndexerArgument> indexer_argument =>
            from id in IdentifierExpression
                .Then(x => Parse.Char(':').Token().Return(x))
                .Positioned()
                .Token().Optional()
            from exp in QualifiedExpression
            select new IndexerArgument(id, exp);

        protected internal virtual Parser<ExpressionSyntax[]> argument_list =>
            from args in argument.Positioned().ChainForward(Parse.Char(',').Token())
            select args.ToArray();


        protected internal virtual Parser<ExpressionSyntax> object_creation_expression =>
            from arg_list in WrappedExpression('(', ')', argument_list.Optional())
            select new ObjectCreationExpression(arg_list.GetOrEmpty());

        protected internal virtual Parser<TypeSyntax> BaseType =>
            SystemType.Or(
                from @void in KeywordExpression("void").Token()
                from a in Parse.Char('*').Token()
                select new TypeSyntax(@void) { IsPointer = true });
        protected internal virtual Parser<ExpressionSettingSyntax> rank_specifier =>
            from ranks in WrappedExpression('[', ']', Parse.Char(',').Token().Many())
            select new RankSpecifierValue(ranks.Count());
        protected internal virtual Parser<ExpressionSettingSyntax> nullable_specifier =>
            from h in Parse.Char('?').Optional()
            select new NullableExpressionValue(h.IsDefined);
        protected internal virtual Parser<ExpressionSettingSyntax> pointer_specifier =>
            from h in Parse.Char('*').Optional()
            select new PointerSpecifierValue(h.IsDefined);

        protected internal virtual Parser<TypeExpression> TypeExpression =>
            from type in BaseType.Or(namespace_or_type_name.Token()).Positioned()
            from meta in nullable_specifier
                .Or(rank_specifier)
                .Or(pointer_specifier)
                .Token().Positioned().Many()
            select new TypeExpression(type).WithMetadata(meta.EmptyIfNull().ToArray());


        protected internal virtual Parser<TypeSyntax> namespace_or_type_name =>
            from id in qualified_alias_member.Or(IdentifierExpression)
            from chain in Parse.Char('/').Token().Then(_ => IdentifierExpression.Token()).Many()
            select new QualifiedAliasSyntax(chain.EmptyIfNull().ToArray(), id);

        protected internal virtual Parser<IdentifierExpression> qualified_alias_member =>
            Identifier.Then(x => Parse.String("::").Token().Return($"{x}::"))
                .Then(x => Identifier.Select(z => $"{x}{z}"))
                .Select(x => new IdentifierExpression(x)).Positioned();


        protected internal virtual Parser<ExpressionSyntax> bracket_expression =>
            from nl in nullable_specifier.Select(x => (NullableExpressionValue)x).Token().Optional()
            from idx in WrappedExpression('[', ']', indexer_argument.ChainForward(Parse.Char(',')))
            select new BracketExpression(nl, idx.ToArray());

        protected internal virtual Parser<ExpressionSyntax> primary_expression_start =>
            LiteralExpression.Select(x => x.Downlevel())
                .Or(LiteralAccessExpression.Select(x => x.Downlevel()).Log("LiteralAccessExpression"))
                .Or(Parse.Ref(() => IdentifierExpression))
                .Or(WrappedExpression('(', ')', Parse.Ref(() => QualifiedExpression)))
                .Or(SystemTypeExpression)
                //.Or(namespace_or_type_name)
                .Or(KeywordExpression("this"))
                .Or(
                    from b in KeywordExpression("base")
                    from dw in
                        Parse.Char('.').Then(_ => IdentifierExpression).Positioned().Select(x => x.Downlevel()).Or(
                            WrappedExpression('[', ']', expression_list).Select(x => new IndexerExpression(x).Downlevel()))
                    select dw)
                .Or(new_expression)

                .Token()
                .Positioned();


        protected internal virtual Parser<ExpressionSyntax> LiteralAccessExpression =>
            from chain in NumberChainBlock
            from suffix in IntegerTypeSuffix.Optional()
            from dot in Parse.Char('.').Token()
            from key in IdentifierExpression
            select new LiteralAccessExpression(FromDefault(chain, suffix.GetOrDefault()), key);



        protected internal virtual Parser<ExpressionSyntax> new_expression =>
            KeywordExpression("new").Token().Then(_ =>
                from type in TypeExpression.Token().Positioned()
                from creation_expression in object_creation_expression.Positioned().Or(array_initializer.Positioned())
                select new NewExpressionSyntax(type, creation_expression)
                ).Positioned().Token();

        protected internal virtual Parser<ParametersArrayExpression> parameters_array =>
            from opb in Parse.Char('{').Token()
            from args in QualifiedExpression.DelimitedBy(Parse.Char(',').Token())
            from clb in Parse.Char('}').Token()
            select new ParametersArrayExpression(args);

        protected internal virtual Parser<ExpressionSyntax> array_initializer =>
            from opb in Parse.Char('[')
            from sizes in variable_initializer.DelimitedBy(Parse.Char(',').Token())
            from clb in Parse.Char(']')
            from args in parameters_array.Positioned().Token().Optional()
            select new ArrayInitializerExpression(sizes, args);


        protected internal virtual Parser<ExpressionSyntax> variable_initializer =>
            QualifiedExpression.Or(array_initializer);

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
            from pe in primary_expression_start.Token().Log("from pe in primary_expression_start")
            from s1 in Parse.Char('!').Token().Optional()
            from bk in bracket_expression.Many()
            from s2 in Parse.Char('!').Token().Optional()
            from dd in (
                from cc in (
                    from zz in method_invocation
                        .Or(member_access)
                        .Or(Parse.String("++").Return(new OperatorExpressionSyntax(ExpressionType.Increment)))
                        .Or(Parse.String("--").Return(new OperatorExpressionSyntax(ExpressionType.Decrement)))
                        .Or(Parse.String("->").Token().Then(_ => IdentifierExpression)).Token().Positioned()
                    from s3 in Parse.Char('!').Token().Optional()
                    select zz
                ).Token()
                from bk1 in bracket_expression.Many()
                from s4 in Parse.Char('!').Token().Optional()
                select FlatIfEmptyOrNull(new ChainAccessExpression(cc, bk1))
            ).Token().Many()
            select FlatIfEmptyOrNull(new MemberAccessExpression(pe, bk, dd));

        private Parser<ExpressionSyntax> UnaryOperator(string op) =>
            from o in Parse.String(op).Token()
            from u in Parse.Ref(() => unary_expression)
            select new UnaryExpressionSyntax { Operand = u, OperatorType = op.ToExpressionType(false) };



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

    public class QualifiedAliasSyntax : TypeSyntax
    {
        public QualifiedAliasSyntax(IdentifierExpression[] namespaces, IdentifierExpression id)
        {
            this.Identifier = id;
            this.Namespaces.AddRange(namespaces);
        }
    }

    public class LiteralAccessExpression : ExpressionSyntax, IPositionAware<LiteralAccessExpression>
    {
        public NumericLiteralExpressionSyntax NumericLiteral { get; set; }
        public IdentifierExpression KeyID { get; set; }

        public LiteralAccessExpression(NumericLiteralExpressionSyntax number, IdentifierExpression id)
        {
            this.NumericLiteral = number;
            this.KeyID = id;
        }
        public new LiteralAccessExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
