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
        public AspectSyntax(IdentifierExpression kind, IOption<ExpressionSyntax> args)
        {
            (Name, Args) = (kind,
                ((ObjectCreationExpression)args.GetOrDefault())?.Args?.EmptyIfNull().ToArray());
            Args ??= Array.Empty<ExpressionSyntax>(); // the fuck
        }

        public IdentifierExpression Name { get; }
        public ExpressionSyntax[] Args { get; } = Array.Empty<ExpressionSyntax>();
        public override SyntaxType Kind => SyntaxType.Annotation;
        public override IEnumerable<BaseSyntax> ChildNodes =>
            new BaseSyntax[] { this }.Concat(Args);


        public bool IsNative => Name.ExpressionString.Equals("native", StringComparison.InvariantCultureIgnoreCase);
        public bool IsSpecial => Name.ExpressionString.Equals("special", StringComparison.InvariantCultureIgnoreCase);
        public bool IsForwarded => Name.ExpressionString.Equals("forwarded", StringComparison.InvariantCultureIgnoreCase);
        public bool IsAlias => Name.ExpressionString.Equals("alias", StringComparison.InvariantCultureIgnoreCase);

        public new AspectSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"Aspect '{Name}'";
    }
}
