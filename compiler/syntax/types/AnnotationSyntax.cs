namespace wave.syntax
{
    using System.Collections.Generic;

    public class AnnotationSyntax : BaseSyntax
    {
        public AnnotationSyntax(WaveAnnotationKind kind) 
            => this.AnnotationKind = kind;
        public WaveAnnotationKind AnnotationKind { get; }
        public override SyntaxType Kind => SyntaxType.Annotation;
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;
    }
}