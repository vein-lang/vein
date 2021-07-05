namespace mana.lsp
{
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using syntax;

    [DataContract]
    public class CompletionItemData
    {
        [DataMember(Name = "namespace")]
        private readonly string? @namespace;

        [DataMember(Name = "name")]
        private readonly string? name;

        /// <summary>
        /// The text document that the original completion request was made from.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier? TextDocument { get; }

        /// <summary>
        /// The qualified name of the completion item.
        /// </summary>
        public IdentifierExpression? QualifiedName =>
            this.@namespace == null || this.name == null
                ? null
                : new IdentifierExpression(/*this.@namespace, */this.name);

        /// <summary>
        /// The source file the completion item is declared in.
        /// </summary>
        [DataMember(Name = "sourceFile")]
        public string? SourceFile { get; }

        public CompletionItemData(
            TextDocumentIdentifier? textDocument = null, IdentifierExpression? qualifiedName = null, string? sourceFile = null)
        {
            this.TextDocument = textDocument;
            //this.@namespace = qualifiedName?.Namespace;
            this.name = qualifiedName?.ExpressionString;
            this.SourceFile = sourceFile;
        }
    }
}
