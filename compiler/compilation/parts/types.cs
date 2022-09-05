namespace vein.compilation;

using System.Linq;
using reflection;
using runtime;
using syntax;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    private void LoadAliases()
    {
        foreach (var clazz in Target.LoadedModules.SelectMany(x => x.class_table)
                     .Where(x => x.Aspects.Any(x => x.IsAlias())))
        {
            var aliases = clazz.Aspects.Where(x => x.IsAlias()).Select(x => x.AsAlias());

            if (aliases.Count() > 1)
            {
                Log.Defer.Error($"[red bold]Detected multiple alises[/] '[purple underline]{aliases.Select(x => x.Name)}[/]'");
                continue;
            }

            var alias = aliases.Single();

            var class_id = new IdentifierExpression(clazz.Name);
            var alias_id = new IdentifierExpression(alias.Name);


            Status.VeinStatus($"Load alias [grey]'{class_id}'[/] -> [grey]'{alias_id}'[/]...");

            if (!KnowClasses.ContainsKey(class_id))
                KnowClasses[class_id] = clazz;
            if (!KnowClasses.ContainsKey(alias_id))
                KnowClasses[alias_id] = clazz;
        }
    }
    private VeinClass FetchType(IdentifierExpression typename, DocumentDeclaration doc)
    {
        if (KnowClasses.ContainsKey(typename))
            return KnowClasses[typename];

        var retType = module.TryFindType(typename.ExpressionString, doc.Includes);

        if (retType is null)
        {
            Log.Defer.Error($"[red bold]Cannot resolve type[/] '[purple underline]{typename}[/]'", typename, doc);
            return new UnresolvedVeinClass($"{this.module.Name}%global::{doc.Name}/{typename}");
        }

        return KnowClasses[typename] = retType;
    }
    private VeinClass FetchType(TypeSyntax typename, DocumentDeclaration doc)
        => FetchType(typename.Identifier, doc);
}
