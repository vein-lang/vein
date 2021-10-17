namespace vein.syntax
{
    using System;
    using Sprache;

    public class ArgumentExpression : ExpressionSyntax, IPositionAware<ArgumentExpression>
    {
        [Obsolete("TODO")]
        public IdentifierExpression Identifier { get; set; }
        [Obsolete("TODO")]
        public ExpressionSyntax Type { get; set; }
        public ExpressionSyntax Value { get; set; }

        public ArgumentExpression(ExpressionSyntax v)
            => this.Value = v;

        public new ArgumentExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"{Value}";
    }
}
