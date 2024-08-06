namespace vein.ast.v2.syntax;

using Superpower.Model;

public interface ITransform
{
    TextSpan Transform { get; set; }
    TextSpan TransformUntil { get; set; }
}
