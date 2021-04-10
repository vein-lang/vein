namespace wave.syntax
{
    using System;
    using Sprache;

    public class AnonFunctionSignatureExpression : ExpressionSyntax, IPositionAware<AnonFunctionSignatureExpression>
    {
        public AnonFunctionSignatureExpression() => Params = Array.Empty<ParameterSyntax>();
        public AnonFunctionSignatureExpression(ParameterSyntax[] p) => Params = p;

        public ParameterSyntax[] Params { get; set; }

        public new AnonFunctionSignatureExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}