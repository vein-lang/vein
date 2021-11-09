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
    private readonly HashSet<string> search_paths_collider_ = new ();

    public ModuleResolverBase AddSearchPath(params DirectoryInfo[] dirs)
    {
        foreach (var dir in dirs)
        {
            if (!search_paths_collider_.Add(dir.FullName)) continue;
            search_paths.Add(dir);
            debug($"Assembly search path '{dir}' is added.");
        }
        return this;
    }

    public virtual VeinModule ResolveDep(VeinArtifact artifact, IReadOnlyList<VeinModule> deps)
    {
        if (artifact.Path is null or { Exists: false })
        {
            debug($"Failed resolve 'unnamed' dependency for '{artifact.ProjectName}' project.");
            throw new FileNotFoundException();
        }

        if (!IshtarAssembly.TryLoadFromFile(artifact.Path, out var asm, out var exception))
        {
            debug($"Failed resolve '{artifact.Path.Name}' dependency for '{artifact.ProjectName}' project.");
            throw exception;
        }

        var mod = ModuleReader.Read(asm.Sections.First().data, deps,
            (s, v) => ResolveDep(s, v, deps));

        debug($"Dependency '{mod.Name}@{mod.Version}' is resolved. [[from artifacts]]");
        return mod;
    }

    public virtual VeinModule ResolveDep(IDependency package, IReadOnlyList<VeinModule> deps)
        => ResolveDep(package.Name, package.Version, deps);

    public virtual VeinModule ResolveDep(string name, Version version, IReadOnlyList<VeinModule> deps)
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
        debug($"Dependency '{name}@{mod.Version}' is resolved. [[from '{file}']]");
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
