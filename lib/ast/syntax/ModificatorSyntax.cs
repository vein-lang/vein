namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using Sprache;

    public enum ModificatorKind
    {
        Public,
        Protected,
        Private,
        Static,
        Const,
        Extern,
        Internal,
        Override,
        Global
    }

    public class ModificatorSyntax : BaseSyntax, IPositionAware<ModificatorSyntax>
    {
        public override SyntaxType Kind => SyntaxType.Modificator;
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public ModificatorKind ModificatorKind { get; }


        public ModificatorSyntax(string mod) 
            => this.ModificatorKind = Enum.Parse<ModificatorKind>(mod, true);

        public new ModificatorSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}