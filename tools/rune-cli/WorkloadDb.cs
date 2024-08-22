namespace vein;

using collections;

public class NotFoundToolException(string tool, PackageKey key) : Exception($"Tool '{tool}' not found in '{key.key}', maybe not installed package?");

public class WorkloadDb
{
    private static readonly SymlinkCollector Symlink = new(SecurityStorage.RootFolder);
    public async Task RegistryTool(PackageKey key, WorkloadPackageTool tool, DirectoryInfo baseFolder)
    {
        var db = await OpenAsync();
        db.tools.TryAdd(key,  new Dictionary<string, string>());


        var file = new FileInfo(Symlink.ToExec(tool.ExecPath));
        var toolExec = baseFolder.Combine(file);
        var name = string.IsNullOrEmpty(tool.OverrideName)
            ? Path.GetFileNameWithoutExtension(file.Name)
            : tool.OverrideName;
        db.tools[key][name] = toolExec.FullName;
        await SaveAsync(db);
    }

    public async Task RegistrySdkAsync(string sdkTarget, string alias, DirectoryInfo baseFolder)
    {
        var db = await OpenAsync();
        db.sdks.TryAdd(sdkTarget, new UniqueList<string>());

        db.sdks[sdkTarget].Add(baseFolder.Combine(new FileInfo(alias)).FullName);
        await SaveAsync(db);
    }

    public async Task<FileInfo?> TakeTool(PackageKey key, string name, bool throwIfNotFound = true)
    {
        var db = await OpenAsync();

        if (db.tools.TryGetValue(key, out var tools))
        {
            if (tools.TryGetValue(name, out var result))
                return new FileInfo(result);
        }
        if (throwIfNotFound)
            throw new NotFoundToolException(name, key);
        return null;
    }

    private async Task<WorkloadDatabase> OpenAsync()
    {
        var dbFile = SecurityStorage.RootFolder.File("workloads.json");
        if (!dbFile.Exists) return new WorkloadDatabase();
        
        var txt = await dbFile.ReadToEndAsync();
        return JsonConvert.DeserializeObject<WorkloadDatabase>(txt)!;
    }

    private async Task SaveAsync(WorkloadDatabase db)
    {
        var dbFile = SecurityStorage.RootFolder.File("workloads.json");

        await dbFile.WriteAllTextAsync(JsonConvert.SerializeObject(db));
    }
}

public record WorkloadDatabase
{
    [JsonConverter(typeof(DictionaryWithPackageKeyConverter<Dictionary<string, string>>))]
    public Dictionary<PackageKey, Dictionary<string, string>> tools = new();
    public Dictionary<string, List<string>> sdks = new();
}
