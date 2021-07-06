namespace mana.syntax
{
    using System.Collections;
    using System.Collections.Generic;
    using extensions;
    using Sprache;
    using stl;

    public class ErrorBlockSyntax : BlockSyntax { }
    public class EmptyBlockSyntax : BlockSyntax { }

    public class BlockSyntax : StatementSyntax, IEnumerable<StatementSyntax>
    {
        public BlockSyntax() { }

        public BlockSyntax(IEnumerable<StatementSyntax> statements)
            => Statements.AddRange(statements.EmptyIfNull());

        public override SyntaxType Kind => SyntaxType.Block;

        public override IEnumerable<BaseSyntax> ChildNodes => Statements;

        public List<StatementSyntax> Statements { get; set; } = new();

        public void Add(StatementSyntax statement) => Statements.Add(statement);

        public List<string> InnerComments { get; set; } = new();

        public IEnumerator GetEnumerator() => ((IEnumerable)Statements).GetEnumerator();

        IEnumerator<StatementSyntax> IEnumerable<StatementSyntax>.GetEnumerator() => Statements.GetEnumerator();

        public new BlockSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
