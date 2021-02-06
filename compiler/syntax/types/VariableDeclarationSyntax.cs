namespace wave.syntax
{
    using System.Collections.Generic;
    using System.Linq;

    public class VariableDeclarationSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.VariableDeclaration;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            GetNodes(Type).Concat(Variables).Where(n => n != null);

        public TypeSyntax Type { get; set; }

        public List<VariableDeclaratorSyntax> Variables { get; set; } = new();
    }
}