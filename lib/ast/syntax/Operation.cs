namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using stl;

    public partial class VeinSyntax
    {
        /// <example>
        /// -> typeName
        /// </example>
        internal virtual Parser<TypeSyntax> OperationReturnType =>
            from open in Parse.String("->").Token()
            from type in TypeReference.Token().Positioned()
            select type;
        /// <example>
        /// body {
        ///     %block%
        /// }
        /// </example>
        protected internal virtual Parser<MethodDeclarationSyntax> OperationParametersAndBody =>
            from parameters in OperationParameters
            from returnType in OperationReturnType
            from openBrace in Parse.Char('{').Token()
            from _ in Keyword("body")
            from methodBody in Block.Token().Positioned()
            from gc in GCDeclaration.Token().Optional()
            from sync in SyncDeclaration.Token().Optional()
            from closeBrace in Parse.Char('}').Commented(this)
            select new MethodDeclarationSyntax
            {
                Parameters = parameters,
                Body = methodBody,
                ReturnType = returnType
            };
        /// <example>
        /// gc nocontrol;
        /// gc auto;
        /// gc
        /// {
        ///     %block%
        /// }
        /// </example>
        protected internal virtual Parser<GCStatementSyntax> GCDeclaration =>
            from _ in Keyword("gc").Token()
            from gc_status in (Keyword("auto").Or(Keyword("nocontrol")))
            from closed in Parse.Char(';')
                //from exp in Block.Optional()
            select new GCStatementSyntax()
            {
                IsNoControl = gc_status == "nocontrol",
                IsAuto = gc_status == "auto",
                //Body = exp.GetOrDefault()
            };
        /// <example>
        /// sync nocontrol;
        /// sync auto;
        /// sync thread;
        /// </example>
        protected internal virtual Parser<SyncStatementSyntax> SyncDeclaration =>
            from _ in Keyword("sync").Token()
            from status in (Keyword("nocontrol").Or(Keyword("thread").Or(Keyword("auto"))))
            from closed in Parse.Char(';')
            select new SyncStatementSyntax()
            {
                IsControl = status == "thread",
                IsAuto = status == "auto"
            };
        /// <example>
        /// [int32 x, int32 y]
        /// </example>
        protected internal virtual Parser<List<ParameterSyntax>> OperationParameters =>
            from openBrace in Parse.Char('[').Token()
            from param in ParameterDeclarations.Optional()
            from closeBrace in Parse.Char(']').Token()
            select param.GetOrElse(Enumerable.Empty<ParameterSyntax>()).ToList();


        /// <example>
        /// public operation Hello[] -> void
        /// {
        ///     body {
        ///     }
        ///     gc auto;
        ///     sync auto;
        /// }
        /// </example>
        protected internal virtual Parser<MethodDeclarationSyntax> OperationDeclaration =>
            from heading in MemberDeclarationHeading
            from _ in Keyword("operation")
            from name in IdentifierExpression
            from methodBody in OperationParametersAndBody
            select new MethodDeclarationSyntax(heading)
            {
                Identifier = name,
                Parameters = methodBody.Parameters,
                Body = methodBody.Body,
                ReturnType = methodBody.ReturnType
            };
    }
}
