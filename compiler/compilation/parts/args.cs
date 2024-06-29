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

        if (!method.IsMethodType) // check method has linked to class, otherwise it is an anonymous method type
        {
            if (method.Modifiers.All(x => x.ModificatorKind != ModificatorKind.Static))
                args.Add(new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, FetchType(method.OwnerClass.Identifier, doc)));
        }
        

        if (method.Parameters.Count == 0)
            return args.ToArray();
        
        return args.Concat(Convert(method.Parameters, method)).ToArray();
    }

    private IEnumerable<VeinArgumentRef> Convert(List<ParameterSyntax> args, MethodDeclarationSyntax method)
    {
        VeinClass selector(TypeExpression exp) => FetchType(exp.Typeword, method.OwnerDocument);

        foreach (var parameter in args)
        {
            var name = parameter.Identifier.ExpressionString;
            var generic = method.GenericTypes.FirstOrDefault(x => x.Typeword.Equals(parameter.Type));
            var constraints =
                method.TypeParameterConstraints.FirstOrDefault(x => x.GenericIndex.Typeword.Equals(parameter.Type));

            if (!method.IsMethodType)
            {
                var classGeneric = method.OwnerClass!.GenericTypes?.FirstOrDefault(x => x.Typeword.Equals(parameter.Type));
                var classGenericConstrains = method.OwnerClass!.TypeParameterConstraints?.FirstOrDefault(x => x.GenericIndex.Typeword.Equals(parameter.Type));

                if (generic is not null && classGeneric is not null)
                {
                    Log.Defer.Error($"Detected conflict of declaration generic types, generic type '[red bold]{parameter.Type.Identifier}[/]' " +
                                    $"is declared in the '[red bold]{method.Identifier}[/]' method and in the '[red bold]{method.OwnerClass!.Identifier}[/]' class", generic, method.OwnerDocument);
                    throw new SkipStatementException();
                }
                if (generic is not null && constraints is not null)
                    yield return new VeinArgumentRef(name,
                        generic.Typeword.ToTypeArg([constraints.ToConstraint(selector)]));
                else if (generic is not null)
                    yield return new VeinArgumentRef(name, generic.Typeword.ToTypeArg([]));
                else if (classGeneric is not null && classGenericConstrains is not null)
                    yield return new VeinArgumentRef(name,
                        classGeneric.Typeword.ToTypeArg([classGenericConstrains.ToConstraint(selector)]));
                else if (classGeneric is not null)
                    yield return new VeinArgumentRef(name, classGeneric.Typeword.ToTypeArg([]));
                else if (parameter.Type.IsSelf)
                    yield return new VeinArgumentRef(name, FetchType(method.OwnerClass.Identifier, method.OwnerDocument));
                else
                    yield return new VeinArgumentRef(name, FetchType(parameter.Type, method.OwnerDocument));
            }
            else
            {
                if (generic is not null && constraints is not null)
                    yield return new VeinArgumentRef(name,
                        generic.Typeword.ToTypeArg([constraints.ToConstraint(selector)]));
                else if (generic is not null)
                    yield return new VeinArgumentRef(name, generic.Typeword.ToTypeArg([]));
                else if (parameter.Type.IsSelf)
                {
                    Log.Defer.Error($"self type is not supported in method type '[red bold]{parameter.Type.Identifier}[/]", parameter, method.OwnerDocument);
                    throw new SkipStatementException();
                }
                else
                    yield return new VeinArgumentRef(name, FetchType(parameter.Type, method.OwnerDocument));
            }
        }
    }
}
