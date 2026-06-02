namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class UnsafeBlockSyntax : StatementSyntax, IPositionAware<UnsafeBlockSyntax>
{
    public override SyntaxType Kind => SyntaxType.UnsafeBlock;

    public BlockSyntax Body { get; set; }

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Body);

    public new UnsafeBlockSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
