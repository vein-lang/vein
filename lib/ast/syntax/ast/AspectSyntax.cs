namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;

    public class AspectSyntax : BaseSyntax, IPositionAware<AspectSyntax>
    {
        public AspectSyntax(IdentifierExpression kind)
            => this.Name = kind;
        public AspectSyntax(IdentifierExpression kind, IOption<ObjectCreationExpression> args)
        {
            (Name, Args) = (kind,
                (args.GetOrDefault() ?? new ObjectCreationExpression(new ArgumentListExpression(Array.Empty<ExpressionSyntax>()))).Args.Arguments.OfType<ArgumentExpression>().ToArray());
        }

        public IdentifierExpression Name { get; }
        public ArgumentExpression[] Args { get; } = Array.Empty<ArgumentExpression>();
        public override SyntaxType Kind => SyntaxType.Annotation;
        public override IEnumerable<BaseSyntax> ChildNodes =>
            new BaseSyntax[] { this }.Concat(Args);


        public bool IsNative => Name.ExpressionString.Equals("native", StringComparison.InvariantCultureIgnoreCase);
        public bool IsSpecial => Name.ExpressionString.Equals("special", StringComparison.InvariantCultureIgnoreCase);
        public bool IsForwarded => Name.ExpressionString.Equals("forwarded", StringComparison.InvariantCultureIgnoreCase);
        public bool IsAspectUsage => Name.ExpressionString.Equals("aspectUsage", StringComparison.InvariantCultureIgnoreCase);

        public new AspectSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"Aspect '{Name}'";
    }
}
