namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;

    public class NewExpressionSyntax : OperatorExpressionSyntax, IPositionAware<NewExpressionSyntax>
    {
        public override SyntaxType Kind => SyntaxType.NewExpression;
        public override IEnumerable<BaseSyntax> ChildNodes => new[] { TargetType, CtorArgs };
        public TypeExpression TargetType { get; set; }
        public ExpressionSyntax CtorArgs { get; set; }

        public bool IsObject => CtorArgs is ObjectCreationExpression;
        public bool IsArray => CtorArgs is ArrayInitializerExpression;

        public NewExpressionSyntax(TypeExpression type, ExpressionSyntax args)
        {
            TargetType = type;
            CtorArgs = args;

            if (args is ArrayInitializerExpression)
                TargetType.Typeword.IsArray = true;
        }

        public new NewExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
