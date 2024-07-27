namespace vein.compilation;

using System.Linq;
using ishtar.emit;
using reflection;
using runtime;
using Spectre.Console;
using syntax;

public partial class CompilationTask
{
    // todo remove this shit
    private void LoadAliases()
    {
        foreach (var alias in Target.LoadedModules.SelectMany(x => x.alias_table).OfType<VeinAliasType>())
        {
            Status.VeinStatus($"Load alias [grey]'{alias.type.FullName.ToString().EscapeMarkup()}'[/] -> [grey]'{alias.aliasName.ToString().EscapeMarkup()}'[/]...");

            KnowClasses.TryAdd(alias.type.FullName, alias.type);
            KnowClasses.TryAdd(alias.aliasName, alias.type);
        }
    }

    private VeinComplexType FetchType(ClassBuilder builder, TypeSyntax typename, DocumentDeclaration doc)
    {
        if (builder.IsGenericType)
        {
            var typeArg = builder.TypeArgs.FirstOrDefault(x => x.Name.Equals(typename.Identifier));
            if (typeArg is not null)
                return typeArg;
        }

        if (typename.IsGeneric)
            return module.FindType(ConvertGenerics(typename), doc.Includes);

        return module.FindType(new NameSymbol(typename.Identifier), doc.Includes);
    }

    private NameSymbol ConvertGenerics(TypeSyntax type)
        => NameSymbol.WithGenerics(type.Identifier, type.TypeParameters.Select(ConvertGenerics).ToList());

    //private VeinClass FetchType_v2(IdentifierExpression typename, DocumentDeclaration doc)
    //{
    //    if (KnowClasses.TryGetValue(typename, out var type))
    //        return type;

    //    var retType = module.TryFindType(typename.ExpressionString, doc.Includes);

    //    if (retType is null)
    //    {
    //        Log.Defer.Error($"[red bold]Cannot resolve type[/] '[purple underline]{typename}[/]'", typename, doc);
    //        return new UnresolvedVeinClass(new QualityTypeName(new(typename), doc.Namespace,
    //            this.module.Name));
    //    }

    //    return KnowClasses[typename] = retType;
    //}
}
