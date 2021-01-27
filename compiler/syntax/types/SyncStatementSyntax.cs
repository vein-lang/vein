namespace wave.syntax
{
    using System;
    using System.Collections.Generic;

    public class SyncStatementSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.GCDeclaration;
        
        public override void Accept(WaveSyntaxVisitor visitor) => throw new NotImplementedException();

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public bool IsAuto { get;set; }
        public bool IsControl { get;set; }
    }
}