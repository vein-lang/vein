namespace mana.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using stl;

    public class AnnotationSyntax : BaseSyntax, IPositionAware<AnnotationSyntax>
    {
        public AnnotationSyntax(ManaAnnotationKind kind)
            => this.AnnotationKind = kind;
        public AnnotationSyntax(ManaAnnotationKind kind, IOption<ExpressionSyntax> args)
            => (AnnotationKind, Args) = (kind, ((ObjectCreationExpression)args.GetOrDefault())?.Args?.DefaultIfEmpty().ToArray());
        public ManaAnnotationKind AnnotationKind { get; }
        public ExpressionSyntax[] Args { get; }
        public override SyntaxType Kind => SyntaxType.Annotation;
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public new AnnotationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"Annotation '{AnnotationKind}'";

    }
}
