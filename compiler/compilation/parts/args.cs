namespace vein.compilation;

using System.Collections.Generic;
using System.Linq;
using ishtar;
using runtime;
using syntax;

public partial class CompilationTask
{
    private VeinArgumentRef[] GenerateArgument(MethodDeclarationSyntax method, DocumentDeclaration doc)
    {
        var args = new List<VeinArgumentRef>();
        var reserved = method.Parameters.FirstOrDefault(x => $"{x.Identifier}".Equals(VeinArgumentRef.THIS_ARGUMENT));

        if (reserved is not null)
        {
            Log.Defer.Error("Cannot use reserved argument name.", reserved.Identifier, doc);
            throw new SkipStatementException();
        }

        if (!method.Modifiers.Any(x => x.ModificatorKind == ModificatorKind.Static))
            args.Add(new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, FetchType(method.OwnerClass.Identifier, doc)));

        if (method.Parameters.Count == 0)
            return args.ToArray();

        return args.Concat(method.Parameters.Select(parameter => new VeinArgumentRef
        {
            Type = parameter.Type.IsSelf ?
                FetchType(method.OwnerClass.Identifier, doc) :
                FetchType(parameter.Type, doc),
            Name = parameter.Identifier.ExpressionString

        })).ToArray();
    }
}
