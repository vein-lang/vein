namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;

    public class QualifiedExpressionStatement : StatementSyntax, IPositionAware<QualifiedExpressionStatement>
    {
        public readonly ExpressionSyntax Value;

        public QualifiedExpressionStatement(ExpressionSyntax exp) => Value = exp;
        
        public new QualifiedExpressionStatement SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class VariableDeclarationSyntax : StatementSyntax, IPositionAware<VariableDeclarationSyntax>
    {
        public override SyntaxType Kind => SyntaxType.VariableDeclaration;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            GetNodes(Type).Concat(new [] {Variable}).Where(n => n != null);

        public TypeExpression Type { get; set; }

        public IdentifierExpression Variable { get; set; }

        public ExpressionSyntax ExpressionValue { get; set; }
        

        public new VariableDeclarationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}