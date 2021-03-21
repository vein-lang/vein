namespace wave.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class AnnotationSyntax : BaseSyntax, IPositionAware<AnnotationSyntax>
    {
        public AnnotationSyntax(WaveAnnotationKind kind) 
            => this.AnnotationKind = kind;
        public WaveAnnotationKind AnnotationKind { get; }
        public override SyntaxType Kind => SyntaxType.Annotation;
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;
        
        public new AnnotationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}