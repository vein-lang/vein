namespace vein.syntax;

using Sprache;
using stl;

public class CorruptedChainException : VeinParseException
{
    public CorruptedChainException(ExpressionSyntax e) :
        base($"Transform is not found in '{e.ExpressionString}'", new Position(0, 0, 0))
    {
    }
}
