namespace wave.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;

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
}