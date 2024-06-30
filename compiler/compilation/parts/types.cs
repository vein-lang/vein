namespace vein.compilation;

using System.Linq;
using reflection;
using runtime;
using syntax;

public partial class CompilationTask
{
    private void LoadAliases()
    {
        foreach (var alias in Target.LoadedModules.SelectMany(x => x.alias_table).OfType<VeinAliasType>())
        {
            var class_id = new IdentifierExpression(alias.type.Name);
            var alias_id = new IdentifierExpression(alias.aliasName.Name);

            Status.VeinStatus($"Load alias [grey]'{class_id}'[/] -> [grey]'{alias_id}'[/]...");

            KnowClasses.TryAdd(class_id, alias.type);
            KnowClasses.TryAdd(alias_id, alias.type);
        }
    }
    private VeinClass FetchType(IdentifierExpression typename, DocumentDeclaration doc)
    {
        if (KnowClasses.TryGetValue(typename, out var type))
            return type;

        var retType = module.TryFindType(typename.ExpressionString, doc.Includes);

        if (retType is null)
        {
            Log.Defer.Error($"[red bold]Cannot resolve type[/] '[purple underline]{typename}[/]'", typename, doc);
            return new UnresolvedVeinClass($"{this.module.Name}%{doc.Name}/{typename}");
        }

        return KnowClasses[typename] = retType;
    }
    private VeinClass FetchType(TypeSyntax typename, DocumentDeclaration doc)
        => FetchType(typename.Identifier, doc);
}
