namespace wave.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;

    public class VariableDeclarationSyntax : StatementSyntax, IPositionAware<VariableDeclarationSyntax>
    {
        public override SyntaxType Kind => SyntaxType.VariableDeclaration;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            GetNodes(Type).Concat(Variables).Where(n => n != null);

        public TypeSyntax Type { get; set; }

        public List<VariableDeclaratorSyntax> Variables { get; set; } = new();
        

        public new VariableDeclarationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}