namespace vein.ast;

using Superpower;
using Superpower.Model;

public static class TextParserExtensions
{
    public static TextParser<T> WithLocation<T>(this TextParser<T> parser)
        where T : ITransform
    {
        Result<T> Location(TextSpan i)
        {
            var inner = parser(i);
            if (!inner.HasValue) return inner;
            inner.Value.Transform = inner.Location;
            inner.Value.TransformUntil = inner.Location.Until(inner.Remainder);
            return inner;
        }

        return Location;
    }
}
