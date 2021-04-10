namespace insomnia.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using stl;

    public class AnnotationSyntax : BaseSyntax, IPositionAware<AnnotationSyntax>
    {
        public AnnotationSyntax(WaveAnnotationKind kind) 
            => this.AnnotationKind = kind;
        public AnnotationSyntax(WaveAnnotationKind kind, IOption<ExpressionSyntax[]> args) 
            => (AnnotationKind, Args) = (kind, args.GetOrEmpty().ToArray());
        public WaveAnnotationKind AnnotationKind { get; }
        public ExpressionSyntax[] Args { get; } = Array.Empty<ExpressionSyntax>();
        public override SyntaxType Kind => SyntaxType.Annotation;
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;
        
        public new AnnotationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}