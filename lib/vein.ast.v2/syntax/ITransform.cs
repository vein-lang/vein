namespace vein.ast;

using Superpower.Model;

public interface ITransform
{
    TextSpan Transform { get; set; }
    TextSpan TransformUntil { get; set; }
}
