namespace mana.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class ParameterSyntax : BaseSyntax, IPositionAware<ParameterSyntax>
    {
        public ParameterSyntax(IdentifierExpression type, IdentifierExpression identifier)
            : this(new TypeSyntax(type), identifier)
        {
        }

        public ParameterSyntax(TypeSyntax type, IdentifierExpression identifier)
        {
            Type = type;
            Identifier = identifier;
        }

        public override SyntaxType Kind => SyntaxType.Parameter;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Type);

        public List<ModificatorSyntax> Modifiers { get; set; } = new();

        public TypeSyntax Type { get; set; }

        public IdentifierExpression Identifier { get; set; }

        public bool IsNeedDetectType => Type is null;

        public new ParameterSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}