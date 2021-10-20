namespace ishtar;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using emit;
using vein;
using vein.fs;
using vein.runtime;

public abstract class ModuleResolverBase : IAssemblyResolver
{
    public const string MODULE_FILE_EXTENSION = "wll";
    protected readonly HashSet<DirectoryInfo> search_paths = new ();

    public ModuleResolverBase AddSearchPath(DirectoryInfo dir)
    {
        debug($"Assembly search path '{dir}' is added.");
        search_paths.Add(dir);
        return this;
    }

    public virtual VeinModule ResolveDep(string name, Version version, List<VeinModule> deps)
    {
        var file = FindInPaths(name);
        if (file is null)
        {
            debug($"Dependency '{name}' is not resolved.");
            throw new FileNotFoundException(name);
        }

        var asm = IshtarAssembly.LoadFromFile(file);

        var mod = ModuleReader.Read(asm.Sections.First().data, deps,
            (s, v) => ResolveDep(s, v, deps));
        debug($"Dependency '{name}@{mod.Version}' is resolved.");
        return mod;
    }

    private FileInfo FindInPaths(string name)
    {
        try
        {
            var files = search_paths
                .SelectMany(x => x.EnumerateFiles($"*.{MODULE_FILE_EXTENSION}"))
                .Where(x =>
                    x.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            return files.Single(x => x.Name.Equals($"{name}.{MODULE_FILE_EXTENSION}", StringComparison.InvariantCultureIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    protected abstract void debug(string s);
}