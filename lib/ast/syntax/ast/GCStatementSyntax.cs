namespace mana.syntax
{
    using System.Collections.Generic;

    public class GCStatementSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.GCDeclaration;

        public override IEnumerable<BaseSyntax> ChildNodes
        {
            get
            {
                if (IsAuto || IsNoControl)
                    return NoChildren;
                return GetNodes(Body);
            }
        }

        public bool IsAuto { get;set; }
        public bool IsNoControl { get;set; }
        public new BlockSyntax Body { get; set; }
    }
}