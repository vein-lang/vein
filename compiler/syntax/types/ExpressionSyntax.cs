namespace wave.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class ExpressionSyntax : BaseSyntax
    {
        public ExpressionSyntax()
        {
        }

        public ExpressionSyntax(string expr) => ExpressionString = expr;

        public static ExpressionSyntax CreateOrDefault(IOption<string> expression) => 
            expression.IsDefined ? new ExpressionSyntax(expression.Get()) : null;

        public override SyntaxType Kind => SyntaxType.Expression;

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public virtual string ExpressionString { get; set; }
    }
}