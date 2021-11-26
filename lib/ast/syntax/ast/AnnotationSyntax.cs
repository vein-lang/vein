namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;
    using stl;

    public class AnnotationSyntax : BaseSyntax, IPositionAware<AnnotationSyntax>
    {
        public AnnotationSyntax(IdentifierExpression kind)
            => this.AnnotationKind = kind;
        public AnnotationSyntax(IdentifierExpression kind, IOption<ExpressionSyntax> args)
        {
            (AnnotationKind, Args) = (kind,
                ((ObjectCreationExpression)args.GetOrDefault())?.Args?.EmptyIfNull().ToArray());
            Args ??= Array.Empty<ExpressionSyntax>(); // the fuck
        }

        public IdentifierExpression AnnotationKind { get; }
        public ExpressionSyntax[] Args { get; } = Array.Empty<ExpressionSyntax>();
        public override SyntaxType Kind => SyntaxType.Annotation;
        public override IEnumerable<BaseSyntax> ChildNodes =>
            new BaseSyntax[] { this }.Concat(Args);

        public new AnnotationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"Annotation '{AnnotationKind}'";

    }
}
