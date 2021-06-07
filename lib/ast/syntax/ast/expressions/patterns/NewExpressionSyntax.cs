namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;

    public class NewExpressionSyntax : OperatorExpressionSyntax, IPositionAware<NewExpressionSyntax>
    {
        public override SyntaxType Kind => SyntaxType.NewExpression;
        public override IEnumerable<BaseSyntax> ChildNodes => CtorArgs.Concat(new BaseSyntax[] { TargetType });
        public TypeExpression TargetType { get; set; }
        public List<ExpressionSyntax> CtorArgs { get; set; }

        public NewExpressionSyntax(TypeExpression type, ExpressionSyntax[] args)
        {
            TargetType = type;
            CtorArgs = args.EmptyIfNull().ToList();
        }

        public new NewExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}