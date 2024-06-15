namespace vein.syntax;

using Sprache;
using stl;

public class CorruptedChainException(ExpressionSyntax e)
    : VeinParseException($"Transform is not found in '{e.ExpressionString}'", new Position(0, 0, 0), e);
