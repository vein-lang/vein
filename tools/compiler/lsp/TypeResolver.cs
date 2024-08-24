using vein.runtime;
using vein.stl;
using vein.syntax;

public class TypeResolver(WorkspaceService workspace)
{
    public VeinClass? ResolveClassByName(NameSymbol key, List<NamespaceSymbol> includes)
    {
        foreach (var module in workspace.Modules)
        {
            var t = module.FindType(key, includes, false);
            if (t is not null)
                return t;
        }
        return null;
    }


    public List<VeinClass> ResolveStaticClasses(List<NamespaceSymbol> includes) =>
        workspace.Modules
            .SelectMany(x => x.class_table
                .Where(z => z.Namespace.IsMatched(includes))
                .Where(z => z is { IsSpecial: false })
                .Where(z => z.Methods.Any(w => !w.IsPrivate && w.IsStatic)))
            .ToList();



    public List<NamespaceSymbol> GetIncludes(string documentText)
    {
        var lines = documentText.Split('\n');
        var list = new List<DirectiveSyntax>();

        foreach (string s in lines)
        {
            if (!s.StartsWith("#"))
                continue;

            list.AddRange(new VeinSyntax().DirectivesUnit.ParseVein(s).Select(x => x.syntax).OfType<DirectiveSyntax>().ToList());
        }

        return list.Where(x => x is UseSyntax).Select(x => new NamespaceSymbol(x.Value.ExpressionString)).ToList();
    }

}
