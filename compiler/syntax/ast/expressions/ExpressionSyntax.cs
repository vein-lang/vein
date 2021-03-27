namespace insomnia.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class ExpressionSyntax : BaseSyntax, IPositionAware<ExpressionSyntax>
    {
        public ExpressionSyntax()
        {
        }
        public ExpressionSyntax(bool isUnused) => HasUnused = isUnused;

        public ExpressionSyntax(string expr) => ExpressionString = expr;

        public static ExpressionSyntax CreateOrDefault(IOption<ExpressionSyntax> expression) 
            => expression.IsDefined ? expression.Get() : null;

        public override SyntaxType Kind => SyntaxType.Expression;

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public virtual string ExpressionString { get; set; }
        public bool HasUnused { get; set; }

        public new ExpressionSyntax SetPos(Position startPos, int length)
        {
            this.Transform = new Transform(startPos, length);
            return this;
        }

        public ExpressionSyntax Downlevel() => this;
    }
}